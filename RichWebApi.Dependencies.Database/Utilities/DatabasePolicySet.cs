using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace RichWebApi.Utilities;

internal sealed class DatabasePolicySet(ILogger<DatabasePolicySet> logger) : IDatabasePolicySet
{
	public IAsyncPolicy DatabaseReadPolicy => Policy.WrapAsync(
			CommonWaitAndRetryOn<SqlException, InvalidOperationException, TimeoutRejectedException>(3)
				.WithPolicyKey("Database read retry"),
			CommonTimeoutPerTry(TimeSpan.FromSeconds(500)).WithPolicyKey("Database read timeout"))
		.WithPolicyKey(nameof(DatabaseReadPolicy));

	public IAsyncPolicy DatabaseWritePolicy => Policy.WrapAsync(
			CommonWaitAndRetryOn<SqlException, InvalidOperationException, TimeoutRejectedException>(5)
				.WithPolicyKey("Database write retry"),
			CommonTimeoutPerTry(TimeSpan.FromSeconds(60)).WithPolicyKey("Database write timeout"))
		.WithPolicyKey(nameof(DatabaseWritePolicy));

	private AsyncTimeoutPolicy CommonTimeoutPerTry(TimeSpan timeSpan)
		=> Policy.TimeoutAsync(timeSpan, TimeoutStrategy.Pessimistic, LogTimeoutAsync);

	private AsyncRetryPolicy CommonWaitAndRetryOn<T1, T2, T3>(int times)
		where T1 : Exception
		where T2 : Exception
		where T3 : Exception
		=> Policy.Handle<T1>().OrInner<T1>()
			.Or<T2>().OrInner<T2>()
			.Or<T3>().OrInner<T3>()
			.WaitAndRetryAsync(times, x => TimeSpan.FromSeconds(Math.Pow(x, 2) / 2), OnRetry);


	private Task LogTimeoutAsync(Context context, TimeSpan waitedFor, Task _)
	{
		logger.LogWarning(
			"{PolicyKey}:{PolicyWrapKey} - timed out after waiting {WaitedFor} for {Operation}",
			context.PolicyKey,
			context.PolicyWrapKey,
			waitedFor,
			context.OperationKey);
		return Task.CompletedTask;
	}

	private void OnRetry(Exception because, TimeSpan goingToWait, Context context) => logger.LogWarning(because,
		"{PolicyKey}:{PolicyWrapKey} - failed to do {Operation}. Unless retry count is exceeded, going to wait {Wait} and retry",
		context.PolicyKey,
		context.PolicyWrapKey,
		context.OperationKey,
		goingToWait
	);
}