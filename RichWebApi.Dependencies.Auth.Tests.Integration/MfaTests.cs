using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
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
		var (auth, _) = await _serviceProvider.RegisterAndLoginUserAsync(factory
			=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")), o => o.SkipMfa = true);
		var client = _serviceProvider.GetRequiredService<IMfaClient>();
		client.SetAccessToken(auth);
		var result = await client.SetupAsync();
		result.ShouldHaveStatusCode(HttpStatusCode.OK);
		var setup = result.Result;
		var totp = new Totp(Base32Encoding.ToBytes(setup.Key));
		var action = () => client.VerifyAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});

		var assert = await action.Should().NotThrowAsync();
		var response = assert.Subject;
		
		response.ShouldHaveStatusCode(HttpStatusCode.OK);
	}

	[Fact]
	public async Task CredsLoginWithMfaEnabledCause302()
	{
		var (auth, login) = await _serviceProvider.RegisterAndLoginUserAsync(factory
			=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")), o => o.SkipMfa = true);
		var mfaClient = _serviceProvider.GetRequiredService<IMfaClient>();
		mfaClient.SetAccessToken(auth);
		var result = await mfaClient.SetupAsync();
		result.ShouldHaveStatusCode(HttpStatusCode.OK);
		var setup = result.Result;
		var totp = new Totp(Base32Encoding.ToBytes(setup.Key));
		await mfaClient.VerifyAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});

		var authClient = _serviceProvider.GetRequiredService<IAuthClient>();

		var action = () => authClient.LoginWithCredentialsAsync(login);
		var assert = await action.Should().ThrowAsync<ApiException<AuthActionRequiredDto>>();
		var response = assert.Which;
		response.ShouldHaveStatusCode(HttpStatusCode.Found);
		response.Result.ErrorCode.Should().Be("two_factor_required");
	}
	
	[Fact]
	public Task LoginsWithMfa() => _serviceProvider.RegisterAndLoginUserAsync(factory
		=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")));

	[Fact]
	public async Task NotMfaTokenLoginCause403()
	{
		var (auth, _) = await _serviceProvider.RegisterAndLoginUserAsync(factory
			=> new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")), o => o.SkipMfa = true);
		var mfaClient = _serviceProvider.GetRequiredService<IMfaClient>();
		mfaClient.SetAccessToken(auth);
		var result = await mfaClient.SetupAsync();
		result.ShouldHaveStatusCode(HttpStatusCode.OK);
		var setup = result.Result;
		var totp = new Totp(Base32Encoding.ToBytes(setup.Key));
		await mfaClient.VerifyAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});
		var authClient = _serviceProvider.GetRequiredService<IAuthClient>();
		authClient.SetAccessToken(auth);
		var action = () => authClient.LoginWithMfaAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});
		var assert = await action.Should().ThrowAsync<ApiException>();
		var response = assert.Which;
		response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
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