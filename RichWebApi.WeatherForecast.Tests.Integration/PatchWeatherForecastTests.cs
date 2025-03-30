using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Logging;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class PatchWeatherForecastTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public PatchWeatherForecastTests(ITestOutputHelper testOutputHelper,
									 IntegrationDependencyContainerFixture container) : base(testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.BuildServiceProvider();

	[Fact]
	public async Task PatchesTomorrowForecast()
	{
		var tomorrow = DateTimeOffset.UtcNow.AddDays(1);
		var client = _serviceProvider.GetRequiredService<IWeatherForecastClient>();
		var initForecast = await client.GetWeatherForecastAsync(tomorrow);

		var modifiedForecast = initForecast.Result.JsonCopy();

		modifiedForecast.Summary = "Integration test";
		modifiedForecast.TemperatureC = 54;

		var response = await client.PatchWeatherForecastAsync(modifiedForecast);
		response.StatusCode.Should().Be(200);

		var modifiedForecastResponse = await client.GetWeatherForecastAsync(tomorrow);
		modifiedForecastResponse.StatusCode.Should().Be(200);
		modifiedForecastResponse.Result
			.Should()
			.BeEquivalentTo(new
			{
				modifiedForecast.Date,
				modifiedForecast.TemperatureC,
				modifiedForecast.Summary
			});

		await client.PatchWeatherForecastAsync(initForecast.Result);
	}
}