using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RichWebApi.Parts;

namespace RichWebApi;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddAppParts(this IServiceCollection services, IAppPartsCollection parts)
	{
		var partsArray = parts.Select(x => new { Part = x, PartAssembly = x.GetType().Assembly }).ToArray();
		services.TryAddEnumerable(partsArray.Select(x => new ServiceDescriptor(typeof(IAppPart), x.Part)));
		var mvcCoreBuilder = services.AddMvcCore();
		var partsAssemblies = partsArray
			.Select(x => x.PartAssembly)
			.ToArray();
		services.CollectCoreServicesFromAssemblies(partsAssemblies);

		foreach (var p in partsArray)
		{
			mvcCoreBuilder.AddApplicationPart(p.PartAssembly);
			p.Part.ConfigureServices(services);
		}

		return services;
	}
}