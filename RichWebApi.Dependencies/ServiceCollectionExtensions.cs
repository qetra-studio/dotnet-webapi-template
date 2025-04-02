using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RichWebApi.Dependencies;
using RichWebApi.Parts;

namespace RichWebApi;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDependencyServices(this IServiceCollection services, IAppDependenciesCollection dependencies, IAppPartsCollection parts)
	{
		services.TryAddEnumerable(dependencies.Select(x => new ServiceDescriptor(typeof(IAppDependency), x)));
		foreach (var d in dependencies)
		{
			d.ConfigureServices(services, parts);
		}

		services.CollectCoreServicesFromAssemblies(dependencies.Select(x => x.GetType().Assembly).ToArray());

		return services;
	}
}