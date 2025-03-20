using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection WithXunitLogging(this IServiceCollection services,
	                                                          ITestOutputHelper testOutputHelper)
	{
		var serilogLogger = new LoggerConfiguration()
			.MinimumLevel.Verbose()
			.Enrich.FromLogContext()
			.WriteTo.TestOutput(testOutputHelper, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] "
			                                                      + "[{RequestId}] "
			                                                      + "[{SourceContext:l}] "
			                                                      + "[{Level:u3}] "
			                                                      + "{Message:lj}{NewLine}"
			                                                      + "{Properties:j}{NewLine}"
			                                                      + "{Exception}")
			.CreateLogger();
		return services.AddLogging(x =>
		{
			x.ClearProviders();
			x.SetMinimumLevel(LogLevel.Information);
			x.AddProvider(new SerilogLoggerProvider(serilogLogger));
		});
	}
}