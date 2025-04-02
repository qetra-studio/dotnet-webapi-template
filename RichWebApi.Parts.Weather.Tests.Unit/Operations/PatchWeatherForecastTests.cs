using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichWebApi.Entities;
using RichWebApi.Hubs;
using RichWebApi.Models;
using RichWebApi.Operations;
using RichWebApi.Parts;
using RichWebApi.Persistence;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.FluentAssertions;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using RichWebApi.Tests.Persistence;
using RichWebApi.Tests.Resources;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Operations;

public class PatchWeatherForecastTests : UnitTest
{
	private readonly IResourceScope _testResources;
	private readonly IServiceProvider _serviceProvider;

	public PatchWeatherForecastTests(ITestOutputHelper testOutputHelper, ResourceRepositoryFixture resourceRepository,
									 UnitDependencyContainerFixture container) : base(testOutputHelper)
	{
		_testResources = resourceRepository.CreateTestScope(this);
		var parts = new AppPartsCollection()
			.AddWeather();
		_serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithTestScopeInMemoryDatabase(parts)
			.WithMockedSignalRHubContext<WeatherHub, IWeatherHubClient>(
				configureHubClients: (_, mock, client) => mock
					.Group(Arg.Is<string>(s => s == WeatherHubConstants.GroupName))
					.Returns(client))
			.ConfigureServices(s => s.AddAppParts(parts))
			.BuildServiceProvider();
	}

	[Fact]
	public async Task UpdatesOne()
	{
		var sharedInput = _testResources.GetJsonInputResource<PatchWeatherForecast>();
		using var caseResources = _testResources.CreateMethodScope();
		var mediator = _serviceProvider.GetRequiredService<IMediator>();

		await mediator.Send(sharedInput);
		var expected = await mediator.Send(new GetWeatherForecasts(0, 1));
		caseResources.CompareWithJsonExpectation(TestOutputHelper, expected, configure: c => c.ExcludingAuditableDtoProperties());
	}

	[Fact]
	public async Task NotifiesAboutUpdates()
	{
		var sharedInput = _testResources.GetJsonInputResource<PatchWeatherForecast>();

		var clientMock = _serviceProvider.GetRequiredService<IWeatherHubClient>();
		var groupManagerMock = _serviceProvider.GetRequiredService<IHubClients<IWeatherHubClient>>();

		await _serviceProvider
			.GetRequiredService<IMediator>()
			.Send(sharedInput);

		await clientMock.Received(1).OnWeatherUpdate(Arg.Any<WeatherForecastDto>());
		groupManagerMock.Received(1).Group(Arg.Is<string>(s => s == WeatherHubConstants.GroupName));
	}

	public override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		var sharedEntity = _testResources.GetJsonInputResource<WeatherForecast>("entity");
		await _serviceProvider
			.GetRequiredService<IRichWebApiDatabase>()
			.PersistEntityAsync(sharedEntity);
	}
}