using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RichWebApi.Entities;
using RichWebApi.Entities.Configuration;
using RichWebApi.Persistence.Interceptors;

namespace RichWebApi;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddSaveChangesInterceptor<T>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
		where T : class, ISaveChangesInterceptor, IOrderedInterceptor
	{
		services.TryAddEnumerable(new ServiceDescriptor(typeof(IOrderedInterceptor), typeof(T), lifetime));
		return services;
	}

	internal static IServiceCollection CollectDatabaseEntities(this IServiceCollection services,
															   IEnumerable<Assembly> assemblies)
	{
		var entityConfigurationType = typeof(INonGenericEntityConfiguration);

		foreach (var assembly in assemblies)
		{
			var types = assembly.GetExportedTypes();
			var configurations = types
				.Where(x => x is { IsAbstract: false, IsClass: true } && x.IsAssignableTo(entityConfigurationType))
				.Select(c => new ServiceDescriptor(entityConfigurationType, c, ServiceLifetime.Scoped));
			services.TryAddEnumerable(configurations);

			var validators = types
				.Where(t => t.IsAssignableTo(typeof(IEntity)))
				.Select(t => new ServiceDescriptor(typeof(IValidator<>).MakeGenericType(t),
					typeof(BasicEntityValidator<>).MakeGenericType(t), ServiceLifetime.Scoped));
			services.TryAddEnumerable(validators);
		}

		return services;
	}
}