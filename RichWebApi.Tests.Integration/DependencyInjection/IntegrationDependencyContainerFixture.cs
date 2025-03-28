using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.Config;
using RichWebApi.Tests.Exceptions;

namespace RichWebApi.Tests.DependencyInjection;

[UsedImplicitly]
public class IntegrationDependencyContainerFixture : DependencyContainerFixture
{
	private static readonly IReadOnlyList<ClientDescriptor> apiClientsToRegister;
	private readonly Dictionary<Type, List<Action<IServiceProvider, HttpClient>>> _clientConfigurators;

	static IntegrationDependencyContainerFixture()
		=> apiClientsToRegister = ScanForClients(typeof(IntegrationTest).Assembly);

	public IntegrationDependencyContainerFixture()
		=> _clientConfigurators = apiClientsToRegister
			.ToDictionary(x => x.ServiceType, _ => new List<Action<IServiceProvider, HttpClient>> { ConfigureBaseUrl });

	public IntegrationDependencyContainerFixture ConfigureClient<TClient>(
		Action<IServiceProvider, HttpClient> configure) where TClient : IRichWebApiClient
	{
		if (!_clientConfigurators.TryGetValue(typeof(TClient), out var configurators))
		{
			throw new TestConfigurationException(
				$"API client of type '{typeof(TClient).Name}' has not been registered");
		}

		configurators.Add(configure);
		return this;
	}

	protected override IServiceCollection ConfigureSharedServices(IServiceCollection services)
	{
		base.ConfigureSharedServices(services);
		var configuration = new ConfigurationBuilder()
			.AddUserSecrets<IntegrationTest>()
			.Build();
		services.TryAddSingleton<IConfiguration>(configuration);
		services.AddOptions<IntegrationTestsConfig>()
			.BindConfiguration("Config");
		foreach (var descriptor in apiClientsToRegister)
		{
			if (!_clientConfigurators.TryGetValue(descriptor.ServiceType, out var configurators))
			{
				throw new TestConfigurationException(
					$"Configurators for API client '{descriptor.ServiceType.Name}' has not been registered");
			}

			descriptor.AddClientToServices(services, (sp, client) =>
			{
				foreach (var configurator in configurators)
				{
					configurator.Invoke(sp, client);
				}
			});
		}

		return services;
	}


	private static IReadOnlyList<ClientDescriptor> ScanForClients(Assembly assembly)
	{
		var baseClassType = typeof(RichWebApiClient);
		var interfaceType = typeof(IRichWebApiClient);
		var baseClassInheritors =
			assembly.GetTypes()
				.Where(x => x is { IsClass: true, IsAbstract: false } && x.IsAssignableTo(baseClassType))
				.ToArray();
		var addHttpClientMethod = typeof(HttpClientFactoryServiceCollectionExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Single(x =>
			{
				if (x is not
					{
						Name: nameof(HttpClientFactoryServiceCollectionExtensions.AddHttpClient),
						IsGenericMethodDefinition: true
					})
				{
					return false;
				}

				if (x.GetGenericArguments().Length != 2)
				{
					return false;
				}

				var parameters = x.GetParameters();
				if (parameters.Length != 2)
				{
					return false;
				}

				return parameters[0].ParameterType == typeof(IServiceCollection)
					   && parameters[1].ParameterType == typeof(Action<IServiceProvider, HttpClient>);
			});
		return baseClassInheritors
			.Select(x =>
			{
				var serviceType = x.GetInterfaces().Single(i => i != interfaceType && i.IsAssignableTo(interfaceType));
				var addSpecificHttpClient = addHttpClientMethod.MakeGenericMethod(serviceType, x);
				return new ClientDescriptor(serviceType,
					x,
					(services, configure) => addSpecificHttpClient.Invoke(null, [services, configure]));
			})
			.ToList()
			.AsReadOnly();
	}

	private static void ConfigureBaseUrl(IServiceProvider sp, HttpClient client)
	{
		var options = sp.GetRequiredService<IOptions<IntegrationTestsConfig>>();
		client.BaseAddress = new Uri(options.Value.BaseUrl);
	}

	private record ClientDescriptor(Type ServiceType, Type ImplementationType,
									Action<IServiceCollection, Action<IServiceProvider, HttpClient>>
										AddClientToServices);
}