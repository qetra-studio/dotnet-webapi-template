using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RichWebApi.Extensions;

public static class LoggerExtensions
{
#if DEBUG
	private const LogLevel PerformanceLoggingLevel = LogLevel.Information;
#else
	private const LogLevel PerformanceLoggingLevel = LogLevel.Debug;
#endif

	public static void Time(this ILogger? logger,
							Action operation,
							[StructuredMessageTemplate] string description,
							params object[] args
	)
	{
		if (logger is null or NullLogger || !logger.IsEnabled(PerformanceLoggingLevel))
		{
			operation();
			return;
		}

		AssertOperationCanBeTimed(operation, description);
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		using (logger.BeginScope(description, args))
		{
			logger.LogPerformanceStart();
			var sw = Stopwatch.StartNew();

			try
			{
				operation();
			}
			catch (Exception e)
			{
				sw.Stop();
				logger.LogPerformanceEnd(sw);
				logger.LogError(e, "Action [{Description}] ended with an error", description);
				throw;
			}
			sw.Stop();
			logger.LogPerformanceEnd(sw);
		}
	}

	public static T Time<T>(this ILogger? logger,
							Func<T> operation,
							[StructuredMessageTemplate] string description,
							params object[] args
	)
	{
		if (logger is null or NullLogger || !logger.IsEnabled(PerformanceLoggingLevel))
		{
			return operation();
		}

		AssertOperationCanBeTimed(operation, description);
		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		using (logger.BeginScope(description, args))
		{
			logger.LogPerformanceStart();
			var sw = Stopwatch.StartNew();
			T? result;
			try
			{
				result = operation();
			}
			catch (Exception e)
			{
				sw.Stop();
				logger.LogPerformanceEnd(sw);
				logger.LogError(e, "Action [{Description}] ended with an error", description);
				throw;
			}
			sw.Stop();
			logger.LogPerformanceEnd(sw);
			return result;
		}
	}

	public static async Task TimeAsync(this ILogger? logger,
									   Func<Task> operation,
									   [StructuredMessageTemplate] string description,
									   params object[] args)
	{
		if (logger is null or NullLogger || !logger.IsEnabled(PerformanceLoggingLevel))
		{
			await operation();
			return;
		}

		AssertOperationCanBeTimed(operation, description);

		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		using (logger.BeginScope(description, args))
		{
			logger.LogPerformanceStart();
			var sw = Stopwatch.StartNew();

			try
			{
				await operation();
			}
			catch (Exception e)
			{
				sw.Stop();
				logger.LogPerformanceEnd(sw);
				logger.LogError(e, "Action [{Description}] ended with an error", description);
				throw;
			}
			sw.Stop();
			logger.LogPerformanceEnd(sw);
		}
	}

	public static async Task<T> TimeAsync<T>(this ILogger? logger,
											 Func<Task<T>> operation,
											 [StructuredMessageTemplate] string description,
											 params object[] args)
	{
		if (logger is null or NullLogger || !logger.IsEnabled(PerformanceLoggingLevel))
		{
			return await operation();
		}

		AssertOperationCanBeTimed(operation, description);

		// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
		using (logger.BeginScope(description, args))
		{
			logger.LogPerformanceStart();
			var sw = Stopwatch.StartNew();
			T? result;
			try
			{
				result = await operation();
			}
			catch (Exception e)
			{
				sw.Stop();
				logger.LogPerformanceEnd(sw);
				logger.LogError(e, "Operation [{Description}] ended with an error", description);
				throw;
			}

			sw.Stop();
			logger.LogPerformanceEnd(sw);
			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LogPerformanceStart(this ILogger logger)
		=> logger.Log(LogLevel.Debug, "PERF: Start");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void LogPerformanceEnd(this ILogger logger, Stopwatch sw)
		=> logger.Log(PerformanceLoggingLevel, "PERF: Finish - operation elapsed in {ElapsedMs} ms",
			sw.Elapsed.TotalMilliseconds.ToString("F"));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AssertOperationCanBeTimed(Delegate operation, string description)
	{
		if (operation is null)
		{
			throw new ArgumentNullException(nameof(operation), "Operation should be real");
		}

		if (string.IsNullOrEmpty(description))
		{
			throw new ArgumentNullException(nameof(description), "Operation should have description");
		}
	}
}