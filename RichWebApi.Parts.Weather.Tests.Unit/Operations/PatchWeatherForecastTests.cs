﻿using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RichWebApi.Entities;
using RichWebApi.Hubs;
using RichWebApi.Models;
using RichWebApi.Persistence;
using RichWebApi.Tests;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Moq;
using RichWebApi.Tests.Resources;
using Xunit.Abstractions;

namespace RichWebApi.Operations;

public class PatchWeatherForecastTests : UnitTest, IAsyncLifetime
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
					.Setup(x => x.Group(It.Is<string>(s => s == WeatherHubConstants.GroupName)))
					.Returns(client)
					.Verifiable("should access the weather group"))
			.ConfigureServices(s => s.AddAppParts(parts))
			.BuildServiceProvider();
	}

	[Fact]
	public async Task UpdatesOne()
	{
		var sharedInput = _testResources.GetJsonInputResource<PatchWeatherForecast>();
		using var resources = _testResources.CreateMethodScope();
		var mediator = _serviceProvider.GetRequiredService<IMediator>();

		await mediator.Send(sharedInput);
		var expected = await mediator.Send(new GetWeatherForecast(0, 1));
		resources.CompareWithJsonExpectation(TestOutputHelper, expected);
	}

	[Fact]
	public async Task NotifiesAboutUpdates()
	{
		var sharedInput = _testResources.GetJsonInputResource<PatchWeatherForecast>();

		var clientMock = _serviceProvider.GetRequiredService<Mock<IWeatherHubClient>>();
		var groupManagerMock = _serviceProvider.GetRequiredService<Mock<IHubClients<IWeatherHubClient>>>();

		await _serviceProvider
			.GetRequiredService<IMediator>()
			.Send(sharedInput);

		clientMock.Verify(x => x.OnWeatherUpdate(It.IsAny<WeatherForecastDto>()), Times.Once());
		groupManagerMock.Verify(x => x.Group(It.Is<string>(s => s == WeatherHubConstants.GroupName)), Times.Once());
	}

	public Task InitializeAsync()
	{
		var sharedEntity = _testResources.GetJsonInputResource<WeatherForecast>("entity");
		return _serviceProvider
			.GetRequiredService<IRichWebApiDatabase>()
			.PersistEntityAsync(sharedEntity)
			.AsTask();
	}

	public Task DisposeAsync()
		=> Task.CompletedTask;
}