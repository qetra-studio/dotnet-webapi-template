using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Fakers;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.Services;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class AuthTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public AuthTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.ConfigureServices(x => x.AddSingleton<JwtVerifier>())
			.BuildServiceProvider();

	[Theory]
	[InlineData("", "", "", "")]
	[InlineData("validUsername", "", "asd@mail.com", "")]
	[InlineData("validUsername", "aaaaaaaaaaaaaaaaaaa", "asd@mail.com", "")]
	[InlineData("", "ASd123.!", "asd@mail.com", "")]
	[InlineData("validUsername", "ASd123.!", "", "")]
	[InlineData("valid.Username", "ASd123.!", "", "")]
	[InlineData("valid.Username123123", "ASd123.!", "", "")]
	[InlineData("123213valid.Username", "ASd123.!", "", "+2754")]
	public async Task RegisterBadCredentialsCause400(string username, string password, string email,
													 string? phoneNumber)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var action = () => client.RegisterAsync(new RegisterDto
		{
			UserName = username,
			Email = email,
			Password = password,
			PhoneNumber = phoneNumber
		});
		await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
	}

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
		await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
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
		await action.Should().ThrowAsync<ApiException<AuthErrorResponseDto>>();
	}

	[Theory]
	[InlineData("+2754", "ASd123.!")]
	public async Task LoginPhoneNumberCause400(string phoneNumber, string password)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var action = () => client.LoginWithCredentialsAsync(new LoginDto
		{
			Login = phoneNumber,
			Password = password,
		});
		await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
	}

	[Fact]
	public async Task CreatesRandomUser()
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var user = _serviceProvider
			.GetRequiredService<IFakerFactory>()
			.RegisterDto()
			.Generate("random");
		var action = () => client.RegisterAsync(user);
		await action.Should().NotThrowAsync();
	}

	[Theory]
	[InlineData(nameof(RegisterDto.UserName))]
	[InlineData(nameof(RegisterDto.Email))]
	public async Task LoginsWithRandomUser(string loginValueKind)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var user = _serviceProvider
			.GetRequiredService<IFakerFactory>()
			.RegisterDto()
			.Generate("random");
		await client.RegisterAsync(user);
		var loginDto = new LoginDto
		{
			Password = user.Password,
			Login = loginValueKind switch
			{
				nameof(RegisterDto.UserName) => user.UserName,
				nameof(RegisterDto.Email) => user.Email,
				_ => throw new ArgumentOutOfRangeException(nameof(loginValueKind), loginValueKind, null)
			}
		};
		var login = () => client.LoginWithCredentialsAsync(loginDto);

		var loginAssert = await login.Should().NotThrowAsync();
		var response = loginAssert.Which.Result;

		var verifier = _serviceProvider.GetRequiredService<JwtVerifier>();

		var signatureResult = await verifier.VerifySignatureAsync(response.AccessToken, options =>
		{
			options.ValidateIssuer = true;
			options.ValidateAudience = true;
			options.ValidateLifetime = true;
			options.ValidIssuer = "identity.richwebapi.com";
			options.ValidAudience = "api.richwebapi.com";
		});
		signatureResult.Should().BeTrue();

		await verifier.VerifyTokenAsync(response.AccessToken, (t, _) =>
		{
			var purposeClaim = t.Claims.FirstOrDefault(x => x.Type == "purpose");
			purposeClaim.Should().NotBeNull();
			purposeClaim.Value.Should().Contain("access");

			var userIdClaim = t.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
			userIdClaim.Should().NotBeNull();
			Guid.TryParse(userIdClaim.Value, out var userId).Should().BeTrue();
			userId.Should().NotBeEmpty();
			return Task.CompletedTask;
		});
	}
}