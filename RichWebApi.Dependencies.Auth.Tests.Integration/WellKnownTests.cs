using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Logging;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class WellKnownTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public WellKnownTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.BuildServiceProvider();

	[Fact]
	public async Task CanAccessJwks()
	{
		var client = _serviceProvider.GetRequiredService<IWellKnownClient>();
		var action = () => client.GetJwksAsync();
		var assertion = await action.Should().NotThrowAsync();
		assertion.Which.ShouldHaveStatusCode(HttpStatusCode.OK);
		var result = assertion.Which.Result;

		result.Keys.Should().NotBeEmpty();
	}
}