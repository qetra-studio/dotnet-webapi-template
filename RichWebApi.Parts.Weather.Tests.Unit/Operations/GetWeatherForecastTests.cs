using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Entities;
using RichWebApi.Operations;
using RichWebApi.Parts;
using RichWebApi.Persistence;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.FluentAssertions;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.Persistence;
using RichWebApi.Tests.Resources;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Operations;

public class GetWeatherForecastTests : UnitTest
{
	private readonly IResourceScope _testResources;
	private readonly IServiceProvider _serviceProvider;

	public GetWeatherForecastTests(ITestOutputHelper testOutputHelper,
								   ResourceRepositoryFixture resourceRepository,
								   UnitDependencyContainerFixture container) : base(testOutputHelper)
	{
		_testResources = resourceRepository.CreateTestScope(this);
		var parts = new AppPartsCollection()
			.AddWeather();
		_serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithTestScopeInMemoryDatabase(parts)
			.ConfigureServices(s => s.AddAppParts(parts))
			.BuildServiceProvider();
	}

	[Fact]
	public async Task LoadsFirstPage()
	{
		using var caseResources = _testResources.CreateMethodScope();
		var entities = caseResources.GetJsonInputResource<WeatherForecast[]>("entities");
		await _serviceProvider
			.GetRequiredService<IRichWebApiDatabase>()
			.PersistEntitiesAsync(entities);

		var input = caseResources.GetJsonInputResource<GetWeatherForecasts>();
		var mediator = _serviceProvider.GetRequiredService<IMediator>();

		var result = await mediator.Send(input);
		caseResources.CompareWithJsonExpectation(TestOutputHelper, result, configure: c => c.ExcludingAuditableDtoProperties());
	}

	[Fact]
	public async Task LoadsOne()
	{
		using var caseResources = _testResources.CreateMethodScope();
		var entities = caseResources.GetJsonInputResource<WeatherForecast[]>("entities");
		await _serviceProvider
			.GetRequiredService<IRichWebApiDatabase>()
			.PersistEntitiesAsync(entities);

		var input = caseResources.GetJsonInputResource<GetWeatherForecasts>();
		var mediator = _serviceProvider.GetRequiredService<IMediator>();

		var result = await mediator.Send(input);
		caseResources.CompareWithJsonExpectation(TestOutputHelper, result, configure: c => c.ExcludingAuditableDtoProperties());
	}
}