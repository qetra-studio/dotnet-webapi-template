using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RichWebApi.Config;
using RichWebApi.Extensions;
using RichWebApi.Persistence.Internal;

namespace RichWebApi.Persistence.Interceptors;

internal class ValidationSaveChangesInterceptor(
	IServiceProvider serviceProvider,
	ILogger<ValidationSaveChangesInterceptor> logger,
	IOptionsMonitor<DatabaseEntitiesConfig> configMonitor)
	: SaveChangesInterceptor, IOrderedInterceptor, IDisposable
{
	private readonly IServiceScope _scope = serviceProvider.CreateScope();

	public uint Order => 1;

	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
																				CancellationToken cancellationToken = default)
	{
		await base.SavingChangesAsync(eventData, result, cancellationToken);
		var context = eventData.Context;
		if (context is null)
		{
			logger.LogWarning("Database context is null");
			return result;
		}

		var validationOption = configMonitor.CurrentValue.Validation;
		if (validationOption == EntitiesValidationOption.None)
		{
			return result;
		}

		await ValidateTrackedEntitiesAsync(context.ChangeTracker, cancellationToken);

		return result;
	}

	public void Dispose() => _scope.Dispose();

	public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
		=> throw new NotSupportedException();

	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
		=> throw new NotSupportedException();

	public override void SaveChangesCanceled(DbContextEventData eventData)
		=> throw new NotSupportedException();

	public override void SaveChangesFailed(DbContextErrorEventData eventData)
		=> throw new NotSupportedException();

	public override InterceptionResult ThrowingConcurrencyException(ConcurrencyExceptionEventData eventData,
																	InterceptionResult result)
		=> throw new NotSupportedException();

	private async Task ValidateTrackedEntitiesAsync(ChangeTracker changeTracker, CancellationToken cancellationToken)
	{
		var sp = _scope.ServiceProvider;
		var failures = await logger.TimeAsync(async () =>
		{
			var validatorsProvider = sp.GetRequiredService<IEntityValidatorsProvider>();
			var entriesByType = changeTracker.Entries()
				.GroupBy(x => x.Metadata.ClrType)
				.ToDictionary(x => x.Key, x => x.Select(e => e.Entity));
			var failures = new Dictionary<Type, IList<ValidationResult>>(entriesByType.Count);

			foreach (var group in entriesByType)
			{
				var asyncValidators = validatorsProvider.GetAsyncValidators(sp, group.Key);

				var tasks = group.Value.SelectMany(x => asyncValidators.Select(v => v(x, cancellationToken)));
				var groupResult = await Task.WhenAll(tasks);
				var groupFailures = groupResult.Where(x => !x.IsValid).ToArray();

				if (groupFailures.Length != 0)
				{
					failures[group.Key] = groupFailures;
				}
			}

			return failures;
		}, "Validate tracked db entities");

		if (failures.Count != 0)
		{
			throw new AggregateException("Entity entries validation failed",
				failures.SelectMany(x => x.Value.Select(v => new ValidationException(x.Key.Name, v.Errors, true))));
		}
	}
}