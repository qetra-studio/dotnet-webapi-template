
namespace RichWebApi.Persistence.Internal;

internal interface IEntityValidatorsProvider
{
	IEnumerable<EntityAsyncValidator> GetAsyncValidators(IServiceProvider serviceProvider, Type entityType);
	
	public IReadOnlyDictionary<Type, (Func<IServiceProvider, object[]> ValidatorsProvider, AsyncValidationExecutor
		ValidationExecutor
		)> AsyncValidators { get; }
	bool AllEntitiesHaveValidators { get; }
}