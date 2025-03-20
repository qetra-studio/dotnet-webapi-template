using System.Runtime.CompilerServices;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RichWebApi.Config;
using RichWebApi.Entities;
using RichWebApi.Entities.Configuration;
using RichWebApi.Persistence;
using RichWebApi.Persistence.Interceptors;
using RichWebApi.Persistence.Internal;
using RichWebApi.Startup;
using RichWebApi.Utilities;
using RichWebApi.Utilities.Paging;
using RichWebApi.Validation;

[assembly: InternalsVisibleTo("RichWebApi.Dependencies.Database.Tests.Unit")]

namespace RichWebApi;

public sealed class DatabaseDependencyOptions
{
	public bool SkipDatabaseClientSetup { get; set; }
}

internal class DatabaseDependency(IHostEnvironment environment, DatabaseDependencyOptions options) : IAppDependency
{
	private const string ConfigurationSection = "Database";

	public void ConfigureServices(IServiceCollection services, IAppPartsCollection parts)
	{
		services.AddOptionsWithValidator<DatabaseConnectionConfig, DatabaseConnectionConfig.ProdEnvValidator>(
			$"Dependencies:{ConfigurationSection}:Connection");

		services.AddOptionsWithValidator<DatabaseEntitiesConfig, DatabaseEntitiesConfig.Validator>(
			$"Dependencies:{ConfigurationSection}:Entities");

		var migrationsAssemblyName = $"{typeof(DatabaseDependency).Assembly.GetName().Name}.Migrations";
		if (!options.SkipDatabaseClientSetup)
		{
			services.AddDbContext<RichWebApiDbContext>((sp, dbContextOptionsBuilder) =>
			{
				dbContextOptionsBuilder.AddInterceptors(sp.GetServices<IOrderedInterceptor>().OrderBy(x => x.Order));
				var dbConfig = sp.GetRequiredService<IOptionsMonitor<DatabaseConnectionConfig>>()
					.CurrentValue;

				if (environment.IsDevelopment())
				{
					dbContextOptionsBuilder.EnableSensitiveDataLogging();
				}

				dbContextOptionsBuilder
					.UseSqlServer(GetConnectionString(sp),
						builder => builder
							.EnableRetryOnFailure(dbConfig.Retries)
							.CommandTimeout(dbConfig.Timeout)
							.MigrationsAssembly(migrationsAssemblyName));
			}, ServiceLifetime.Transient);
		}

		services.AddStartupAction<DatabaseMigrationAction>();
		services.AddSaveChangesInterceptor<ValidationSaveChangesInterceptor>();
		services.AddSaveChangesInterceptor<AuditSaveChangesInterceptor>();
		services.TryAddScoped<IRichWebApiDatabase, RichWebApiDatabase>();
		services.TryAddScoped<IDatabasePolicySet, DatabasePolicySet>();
		services.TryAddScoped<IDatabaseConfigurator, DatabaseConfigurator>();
		services.TryAddScoped<IValidator<IPagedRequest>, IPagedRequest.Validator>();
		services.TryAddScoped<IValidator<IAuditableEntity>, IAuditableEntity.Validator>();
		services.TryAddScoped<IValidator<ISoftDeletableEntity>, ISoftDeletableEntity.Validator>();
		AddInternalServices(services);
		services.AddHealthChecks()
			.AddSqlServer(GetConnectionString);

		var dependenciesToScan = parts
			.Select(x => x.GetType().Assembly)
			.ToList();
		dependenciesToScan.Add(typeof(DatabaseDependency).Assembly);
		services.CollectDatabaseEntities(dependenciesToScan);
	}

	private static void AddInternalServices(IServiceCollection services)
		=> services.TryAddSingleton<IEntityValidatorsProvider, EntityValidatorsProvider>();

	private static string GetConnectionString(IServiceProvider sp)
	{
		var dbConfig = sp.GetRequiredService<IOptionsMonitor<DatabaseConnectionConfig>>()
			.CurrentValue;

		return dbConfig.ToConnectionString();
	}


	public void ConfigureApplication(IApplicationBuilder builder)
	{
	}
}