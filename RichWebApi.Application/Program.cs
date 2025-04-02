using System.Text.Json;
using System.Text.Json.Serialization;
using Autofac.Extensions.DependencyInjection;
using Destructurama;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RichWebApi.Config;
using RichWebApi.Dependencies;
using RichWebApi.Filters;
using RichWebApi.HealthChecks;
using RichWebApi.Maintenance;
using RichWebApi.Middleware;
using RichWebApi.Parts;
using RichWebApi.Startup;
using RichWebApi.Swagger;
using RichWebApi.Validation;
using Riok.Mapperly.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]
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
			Console.WriteLine(e);
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
		var logger = app.Services.GetRequiredService<ILogger<Program>>();
		var config = app.Services.GetRequiredService<IOptions<StartupConfig>>();
		if (!config.Value.IgnoreStartupActions)
		{
			await using var scope = app.Services.CreateAsyncScope();
			var sp = scope.ServiceProvider;

			var maintenance = sp.GetRequiredService<ApplicationMaintenance>();
			await maintenance.ExecuteInScopeAsync(() => sp
					.GetRequiredService<IStartupActionCoordinator>()
					.PerformStartupActionsAsync(lifetime.ApplicationStopping),
				new MaintenanceReason("Startup"));
		}
		else
		{
			logger.LogDebug("Skip run of startup actions ignored due to configuration");
		}

		await appRunner;
	}

	private static IConfigurationBuilder ConfigureConfiguration(IConfigurationBuilder configuration)
		=> configuration
			.AddEnvironmentVariables()
			.AddUserSecrets<Program>(true, true);

	private static IHostBuilder ConfigureHost(IHostBuilder host)
		=> host
			.UseServiceProviderFactory(new AutofacServiceProviderFactory())
			.UseSerilog((context, sp, loggerConfiguration) =>
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
					.Destructure.UsingAttributes()
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
		=> collection
			.AddAuth()
			.AddDatabase(env)
			.AddSignalR(c => c.AddWeather());

	public static IAppPartsCollection EnrichWithApplicationParts(IAppPartsCollection collection)
		=> collection.AddWeather();

	private static IServiceCollection ConfigureServices(IServiceCollection services,
														IAppPartsCollection parts,
														IAppDependenciesCollection dependencies)
	{
		services.AddCore();
		services.AddOptionsWithValidator<StartupConfig, StartupConfig.Validator>("Startup");
		services.AddDependencyServices(dependencies, parts);
		services.AddMvcCore(x => { x.Filters.Add<ExceptionFilter>(); }).AddApplicationPart(typeof(Program).Assembly);
		services.CollectCoreServicesFromAssembly(typeof(Program).Assembly);
		services.AddAppParts(parts);
		services.AddControllers()
			.AddJsonOptions(options =>
			{
				var namingPolicy = JsonNamingPolicy.CamelCase;
				options.JsonSerializerOptions.PropertyNamingPolicy = namingPolicy;
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy));
			});

		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(s =>
		{
			s.SupportNonNullableReferenceTypes();
			s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				Name = "Authorization",
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				In = ParameterLocation.Header,
				Description = "Please enter your JWT token"
			});

			s.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "Bearer"
						}
					},
					[]
				}
			});
			s.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "RichWebApi",
				Version = "v1"
			});
			s.AddSignalRSwaggerGen();
			s.OperationFilter<Response400OperationFilter>();
		});

		services.AddFluentValidationRulesToSwagger(opt => opt.SetFluentValidationCompatibility());
		services.AddHealthChecks();

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

		app.UseDependencies(dependencies);

		return app;
	}
}