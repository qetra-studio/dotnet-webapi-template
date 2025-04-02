using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichWebApi.Dependencies;
using RichWebApi.Parts;
using Riok.Mapperly.Abstractions;

[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]

namespace RichWebApi;

internal class SignalRDependency : IAppDependency, ISignalRConfigurator
{
	private readonly IList<Action<IEndpointRouteBuilder>> _hubEndpointsConfigurators =
		new List<Action<IEndpointRouteBuilder>>();

	public void ConfigureServices(IServiceCollection services)
	{
		if (_hubEndpointsConfigurators.Count == 0)
		{
			return;
		}

		services.AddSignalR(x =>
			{
				x.EnableDetailedErrors = true;
				x.ClientTimeoutInterval = TimeSpan.FromMinutes(20);
			})
			.AddJsonProtocol(x =>
				x.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
	}

	public void ConfigureServices(IServiceCollection services, IAppPartsCollection parts)
	{
		if (_hubEndpointsConfigurators.Count == 0)
		{
			return;
		}

		services
			.AddSwaggerGen(options
				=> options.AddSignalRSwaggerGen(signalROptions
					=> signalROptions.ScanAssemblies(parts
						.Select(x => x.GetType().Assembly))))
			.AddSignalR(x =>
			{
				x.EnableDetailedErrors = true;
				x.ClientTimeoutInterval = TimeSpan.FromMinutes(20);
			})
			.AddJsonProtocol(x =>
				x.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
	}

	public void ConfigureApplication(IApplicationBuilder builder)
	{
		if (_hubEndpointsConfigurators.Count == 0)
		{
			return;
		}

		builder.UseEndpoints(b =>
			{
				foreach (var hubConfigurator in _hubEndpointsConfigurators)
				{
					hubConfigurator.Invoke(b);
				}
			}
		);
	}

	public ISignalRConfigurator WithHub<T>(string pattern,
										   Action<HttpConnectionDispatcherOptions>? configureOptions = null,
										   Action<HubEndpointConventionBuilder>? configureConventions = null)
		where T : Hub
	{
		_hubEndpointsConfigurators.Add(b =>
		{
			var logger = b.ServiceProvider.GetRequiredService<ILogger<SignalRDependency>>();
			var conventionBuilder = b.MapHub<T>(pattern, configureOptions);
			configureConventions?.Invoke(conventionBuilder);
			logger.LogDebug("Mapped hub '{HubName}' with route '{HubRoute}'", typeof(T).Name, pattern);
		});
		return this;
	}

	public ISignalRConfigurator WithHub<T>(
		Func<IServiceProvider, (string Pattern, Action<HttpConnectionDispatcherOptions>? ConfigureOptions,
			Action<HubEndpointConventionBuilder>? ConfigureConventions)> configure) where T : Hub
	{
		_hubEndpointsConfigurators.Add(b =>
		{
			var logger = b.ServiceProvider.GetRequiredService<ILogger<SignalRDependency>>();
			var (pattern, configureOptions, configureConventions) = configure.Invoke(b.ServiceProvider);
			var conventionBuilder = b.MapHub<T>(pattern, configureOptions);
			configureConventions?.Invoke(conventionBuilder);
			logger.LogDebug("Mapped hub '{HubName}' with route '{HubRoute}'", typeof(T).Name, pattern);
		});
		return this;
	}
}