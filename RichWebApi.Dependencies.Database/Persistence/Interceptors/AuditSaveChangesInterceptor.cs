using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using RichWebApi.Entities;
using RichWebApi.Extensions;
using RichWebApi.Services;

namespace RichWebApi.Persistence.Interceptors;

internal class AuditSaveChangesInterceptor(
	ILogger<AuditSaveChangesInterceptor> logger,
	ISystemClock clock,
	IIdentityProvider identityProvider)
	: SaveChangesInterceptor, IOrderedInterceptor
{
	public uint Order => 0;


	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)
	{
		await base.SavingChangesAsync(eventData, result, cancellationToken);

		var ctx = eventData.Context;
		if (ctx is null)
		{
			logger.LogWarning("Database context is null");
			return result;
		}

		logger.Time(() => AuditEntities(ctx.ChangeTracker),
			"Audit tracked database context '{DatabaseContextName}' entities",
			ctx.GetType().Name);
		return result;
	}

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

	private void AuditEntities(ChangeTracker changeTracker)
	{
		var entries = changeTracker.Entries();

		foreach (var entry in entries)
		{
			var now = clock.UtcNow.DateTime;

			if (entry.Entity is IDateAuditableEntity da)
			{
				switch (entry.State)
				{
					case EntityState.Modified:
						da.ModifiedAt = now;
						break;
					case EntityState.Added:
						da.CreatedAt = now;
						da.ModifiedAt = now;
						break;
					case EntityState.Detached:
					case EntityState.Unchanged:
					case EntityState.Deleted:
					default:
						break;
				}
			}

			if (entry.Entity is IIdentityAuditableEntity ia)
			{
				switch (entry.State)
				{
					case EntityState.Modified:
						ia.ModifiedById = identityProvider.UserId;
						break;
					case EntityState.Added:
						ia.CreatedById = identityProvider.UserId;
						ia.ModifiedById = identityProvider.UserId;
						break;
					case EntityState.Detached:
					case EntityState.Unchanged:
					case EntityState.Deleted:
					default:
						break;
				}
			}

			var state = entry.State;

			if (entry.Entity is IDateSoftDeletableEntity dsd)
			{
				switch (entry.State)
				{
					case EntityState.Deleted:
						state = EntityState.Modified;
						dsd.DeletedAt = now;
						break;
					case EntityState.Modified:
					case EntityState.Added:
					case EntityState.Detached:
					case EntityState.Unchanged:
					default:
						break;
				}
			}

			if (entry.Entity is IIdentitySoftDeletableEntity isd)
			{
				switch (entry.State)
				{
					case EntityState.Deleted:
						state = EntityState.Modified;
						isd.DeletedById = identityProvider.UserId;
						break;
					case EntityState.Modified:
					case EntityState.Added:
					case EntityState.Detached:
					case EntityState.Unchanged:
					default:
						break;
				}
			}

			entry.State = state;
		}
	}
}