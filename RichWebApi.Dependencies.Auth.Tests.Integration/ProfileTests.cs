using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Fakers;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.Operations;
using RichWebApi.Tests.Services;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class ProfileTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public ProfileTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithAuthServices()
			.BuildServiceProvider();

	[Fact]
	public async Task UserCanAccessHimself()
	{
		var (auth, _) = await _serviceProvider.RegisterAndLoginUserAsync(factory
			=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")));
		var client = _serviceProvider.GetRequiredService<IProfileClient>();
		client.SetAccessToken(auth);
		var action = () => client.GetCurrentUserProfileAsync();
		var assert = await action.Should().NotThrowAsync();
		var response = assert.Subject;
		response.ShouldHaveStatusCode(HttpStatusCode.OK);
	}
}