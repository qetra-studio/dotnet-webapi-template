using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Enums;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Fakers;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.Operations;
using RichWebApi.Tests.Services;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class LoginTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public LoginTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithAuthServices()
			.BuildServiceProvider();

	[Theory]
	[InlineData("", "")]
	[InlineData("123213valid.Username", "")]
	[InlineData("1", "Asd123!.")]
	public async Task LoginBadCredentialsCause400(string login, string password)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var action = () => client.LoginWithCredentialsAsync(new LoginDto
		{
			Login = login,
			Password = password,
		});
		var assert = await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData("123213valid.Username", "Asd123.!OrOtherNonExistentUserPassword")]
	public async Task LoginInvalidCredentialsCause401(string login, string password)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var action = () => client.LoginWithCredentialsAsync(new LoginDto
		{
			Login = login,
			Password = password,
		});
		var assert = await action.Should().ThrowAsync<ApiException<AuthErrorResponseDto>>();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task LoginPhoneNumberCause400()
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var user = _serviceProvider
			.GetRequiredService<IFakerFactory>()
			.RegisterDto()
			.Generate("random,phonenumber");
		await client.RegisterAsync(user);
		var action = () => client.LoginWithCredentialsAsync(new LoginDto
		{
			Login = user.PhoneNumber!,
			Password = user.Password,
		});
		var assert = await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
	}

	[Theory]
	[InlineData(LoginValueKind.UserName)]
	[InlineData(LoginValueKind.Email)]
	public Task LoginsWithRandomUser(LoginValueKind loginValueKind)
		=> _serviceProvider.RegisterAndLoginUserAsync(
			factory => new ValueTask<RegisterDto>(factory.RegisterDto().Generate("random")),
			options => options.LoginValueKind = loginValueKind);
}