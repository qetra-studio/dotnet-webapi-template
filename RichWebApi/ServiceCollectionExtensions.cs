using System.Reflection;
using System.Runtime.CompilerServices;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using RichWebApi.Authorization;
using RichWebApi.Config;
using RichWebApi.Maintenance;
using RichWebApi.MediatR;
using RichWebApi.Services;
using RichWebApi.Startup;

[assembly: InternalsVisibleTo("RichWebApi.Tests.Unit")]

namespace RichWebApi;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCore(this IServiceCollection services)
	{
		services.TryAddTransient<IStartupActionCoordinator, StartupActionCoordinator>();
		services.TryAddSingleton<ApplicationMaintenance>();
		services.TryAddSingleton<ISystemClock, SystemClock>();

		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

		services.CollectCoreServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
		return services;
	}

	public static IServiceCollection AddStartupAction<T>(this IServiceCollection services) where T : IAsyncStartupAction
	{
		services.TryAddEnumerable(new ServiceDescriptor(typeof(IAsyncStartupAction), typeof(T),
			ServiceLifetime.Scoped));
		return services;
	}

	public static IServiceCollection AddCronService<T>(this IServiceCollection services)
		where T : CronScheduleService
	{
		services.TryAddSingleton<T>();
		services.AddHostedService<T>(sp => sp.GetRequiredService<T>());
		return services;
	}

	public static IServiceCollection CollectCoreServicesFromAssemblies(this IServiceCollection services,
																	   Assembly[] assemblies)
	{
		var appConfig = typeof(IAppConfig);
		var builder = services.AddAuthorizationBuilder();
		foreach (var assembly in assemblies)
		{
			builder.ConfigureForAssembly(assembly);
		}
		return services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true,
				filter: result =>
				{
					var validatorInterface = result.InterfaceType;
					if (validatorInterface is not { IsInterface: true, IsConstructedGenericType: true })
					{
						return true;
					}

					if (validatorInterface.GetGenericTypeDefinition() != typeof(IValidator<>))
					{
						return true;
					}

					var parameter = validatorInterface.GetGenericArguments()[0];
					return !parameter.IsAssignableTo(appConfig);
				}) // app config validators have their own lifetime
			.AddMediatR(x => x.RegisterServicesFromAssemblies(assemblies))
			.AddAutoMapper(assemblies);
	}

	public static IServiceCollection CollectCoreServicesFromAssembly(this IServiceCollection services,
																	 Assembly assembly)
		=> services.CollectCoreServicesFromAssemblies([assembly]);
}