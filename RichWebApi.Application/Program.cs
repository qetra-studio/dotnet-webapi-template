using AutoMapper.EquivalencyExpression;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using RichWebApi.HealthChecks;
using RichWebApi.Maintenance;
using RichWebApi.Middleware;
using RichWebApi.Parts.Auth;
using RichWebApi.Startup;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace RichWebApi;

public class Program
{
	public static async Task Main(string[] args)
	{
		try
		{
			var builder = WebApplication.CreateBuilder(args);

			ConfigureConfiguration(builder.Configuration);
			ConfigureHost(builder.Host);

			var dependencies = EnrichWithDependencies(new AppDependenciesCollection(), builder.Environment);
			var parts = EnrichWithApplicationParts(new AppPartsCollection());

			ConfigureServices(builder.Services, parts, dependencies);

			var app = ConfigureWebApp(builder.Build(), dependencies);
			var logger = app.Services.GetRequiredService<ILogger<Program>>();
			logger.LogDebug("Configured app parts {@AppParts} along with dependencies {@Dependencies}",
				parts.Select(x => x.GetType().FullName),
				dependencies.Select(x => x.GetType().FullName));
			await RunAsync(app);
		}
		catch (Exception e)
		{
			Log.Error(e, "Application exited with error");
		}
		finally
		{
			await Log.CloseAndFlushAsync();
		}
	}

	private static async Task RunAsync(WebApplication app)
	{
		var lifetime = app.Lifetime;

		var appRunner = app.RunAsync();
		lifetime.ApplicationStarted.WaitHandle.WaitOne();

		await using (var scope = app.Services.CreateAsyncScope())
		{
			var sp = scope.ServiceProvider;
			var maintenance = sp.GetRequiredService<ApplicationMaintenance>();
			await maintenance.ExecuteInScopeAsync(() => sp
					.GetRequiredService<IStartupActionCoordinator>()
					.PerformStartupActionsAsync(lifetime.ApplicationStopping),
				new MaintenanceReason("Startup"));
		}

		await appRunner;
	}

	private static IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder configuration)
		=> configuration
			.AddEnvironmentVariables()
			.AddUserSecrets<Program>(true, true);

	private static IHostBuilder ConfigureHost(IHostBuilder host)
		=> host.UseSerilog((context, sp, loggerConfiguration) =>
		{
			// When something wrong with logging - uncomment the line below
			// Serilog.Debugging.SelfLog.Enable(Console.Error);

			const string logOutputTemplate = "[{Timestamp:HH:mm:ss.fff}] "
											 + "[{RequestId}] "
											 + "[{SourceContext:l}] "
											 + "[{Level:u3}] "
											 + "{Message:lj}{NewLine}"
											 + "{Properties:j}{NewLine}"
											 + "{Exception}";

			loggerConfiguration
				.ReadFrom.Configuration(context.Configuration)
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
				.Enrich.WithThreadId();

			if (context.HostingEnvironment.IsDevelopment())
			{
				loggerConfiguration.WriteTo.Console(
						outputTemplate: logOutputTemplate,
						theme: AnsiConsoleTheme.Literate,
						restrictedToMinimumLevel: LogEventLevel.Debug)
					.WriteTo.Seq("http://localhost:5341");
			}
		});

	private static IAppDependenciesCollection EnrichWithDependencies(IAppDependenciesCollection collection,
																	 IWebHostEnvironment env)
		=> collection.AddDatabase(env)
			.AddSignalR(c => c.AddWeather())
			.AddAuth();

	public static IAppPartsCollection EnrichWithApplicationParts(IAppPartsCollection collection)
		=> collection.AddWeather();

	private static IServiceCollection ConfigureServices(IServiceCollection services,
														IAppPartsCollection parts,
														IAppDependenciesCollection dependencies)
	{
		services.AddControllers();
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(s =>
		{
			s.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "RichWebApi",
				Version = "v1"
			});
			s.AddSignalRSwaggerGen();
		});

		services.AddFluentValidationRulesToSwagger(opt => opt.SetFluentValidationCompatibility());
		services.AddHealthChecks();

		services.AddAutoMapper(x => x.AddCollectionMappers(), typeof(Program).Assembly);


		services.AddCore();
		services.AddAppParts(parts);
		services.AddDependencyServices(dependencies, parts);

		services.AddStartupAction<AutoMapperValidationAction>();
		return services;
	}

	private static WebApplication ConfigureWebApp(WebApplication app, IAppDependenciesCollection dependencies)
	{
		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHealthChecks(new PathString("/api/health"), new HealthCheckOptions
		{
			ResponseWriter = HealthChecksResponseWriter.WriteAsync
		});
		app.UseMiddleware<MaintenanceMiddleware>();

		app.UseHttpsRedirection();
		app.MapControllers();
		app.UseRouting();

		app.UseAuthorization();
		app.UseDependencies(dependencies);

		return app;
	}
}