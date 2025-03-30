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

public sealed class MfaTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public MfaTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithAuthServices()
			.BuildServiceProvider();

	[Fact]
	public async Task RegistersMfa()
	{
		var auth = await _serviceProvider.RegisterAndLoginUserAsync(factory
			=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")));
		var client = _serviceProvider.GetRequiredService<IMfaClient>();
		client.SetAccessToken(auth);
		var result = await client.SetupAsync();
		result.ShouldHaveStatusCode(HttpStatusCode.OK);
		// todo totp verification
	}

	[Fact]
	public async Task AnonymousSetupCause401()
	{
		var client = _serviceProvider.GetRequiredService<IMfaClient>();
		var action = () => client.SetupAsync();
		var assert = await action.Should().ThrowAsync<ApiException>();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
	}
}