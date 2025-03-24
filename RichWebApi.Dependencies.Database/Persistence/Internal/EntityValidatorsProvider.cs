using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichWebApi.Config;
using RichWebApi.Entities;
using RichWebApi.Entities.Configuration;
using RichWebApi.Exceptions;
using RichWebApi.Extensions;
using static System.Linq.Expressions.Expression;

namespace RichWebApi.Persistence.Internal;

internal class EntityValidatorsProvider : IEntityValidatorsProvider
{
	private readonly ILogger<EntityValidatorsProvider> _logger;

	public IReadOnlyDictionary<Type, (Func<IServiceProvider, object[]> ValidatorsProvider, AsyncValidationExecutor
		ValidationExecutor)> AsyncValidators { get; }

	public bool AllEntitiesHaveValidators { get; }

	public EntityValidatorsProvider(ILogger<EntityValidatorsProvider> logger,
	                                IServiceProvider serviceProvider,
	                                IOptionsMonitor<DatabaseEntitiesConfig> configMonitor,
	                                IEnumerable<IAppPart> partsToScan)
	{
		_logger = logger;
		var typeMarkersToScan = partsToScan
			.Select(x => x.GetType())
			.ToList();
		typeMarkersToScan.Add(typeof(EntityValidatorsProvider));
		var entityType = typeof(IEntity);
		var entityTypes = typeMarkersToScan
			.SelectMany(x => x.Assembly.ExportedTypes
				.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
				            && t.IsAssignableTo(entityType)))
			.ToArray();
		var validators = logger.Time(() =>
		{
			using var scope = serviceProvider.CreateScope();
			var sp = scope.ServiceProvider;
			return entityTypes.Select(x => (Type: x, MaybeValidators: CreateEntityValidators(sp, x)))
				.Where(x => x.MaybeValidators.HasValue)
				.ToDictionary(x => x.Type, x => x.MaybeValidators!.Value);
		}, "Build validator callers for entities, count: {Count}", entityTypes.Length);
		AllEntitiesHaveValidators = entityTypes.Length == validators.Count;
		AsyncValidators = validators.AsReadOnly();

		if (configMonitor.CurrentValue.Validation == EntitiesValidationOption.Required && !AllEntitiesHaveValidators)
		{
			var missingValidatorTypes = entityTypes
				.Where(x => !validators.ContainsKey(x))
				.ToArray();
			throw new MissingEntitiesValidatorsException(missingValidatorTypes);
		}
	}

	public IEnumerable<EntityAsyncValidator> GetAsyncValidators(IServiceProvider serviceProvider, Type entityType)
	{
		if (!AsyncValidators.TryGetValue(entityType, out var entityValidators))
		{
			throw new MissingEntitiesValidatorsException([entityType]);
		}

		var (validatorProvider, validate) = entityValidators;
		var validators = validatorProvider(serviceProvider);
		foreach (var v in validators)
		{
			yield return (entity, token) => validate(v, entity, token);	
		}
		
	}

	private (Func<IServiceProvider, object[]>, AsyncValidationExecutor)? CreateEntityValidators(
		IServiceProvider serviceProvider, Type entityType)
	{
		var entityName = entityType.Name;


		var validatorType = typeof(IValidator<>).MakeGenericType(entityType);

		object[] ProvideValidators(IServiceProvider sp) => sp.GetServices(validatorType)
			.Where(x => x is not null)
			.ToArray()!;

		var implementations = ProvideValidators(serviceProvider).Where(x =>
		{
			var type = x.GetType();
			return !type.IsConstructedGenericType || type.GetGenericTypeDefinition() != typeof(BasicEntityValidator<>);
		}).ToArray();

		if (implementations.Length == 0)
		{
			_logger.LogDebug("Missing validator implementation for entity '{EntityName}'", entityName);
			return null;
		}

		var validateFunc = _logger.Time(() =>
		{
			var validatorParameterExpr = Parameter(typeof(object), "validator");
			var method = validatorType.GetMethod(nameof(IValidator<object>.ValidateAsync),
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

			if (method is null)
			{
				throw new InvalidOperationException(
					$"Type '{validatorType.Name}' doesn't contain method '{nameof(IValidator<object>.ValidateAsync)}' which should be used in validation delegate.");
			}

			var entityParameterExpr = Parameter(typeof(object), "entity");

			var ctParameterExpr = Parameter(typeof(CancellationToken), "cancellationToken");
			return Lambda<AsyncValidationExecutor>(
				Call(Convert(validatorParameterExpr, validatorType), method, Convert(entityParameterExpr, entityType),
					ctParameterExpr), validatorParameterExpr,
				entityParameterExpr, ctParameterExpr
			).Compile();
		}, "Build validator caller for entity '{EntityName}'", entityName);

		return (ProvideValidators, validateFunc);
	}
}