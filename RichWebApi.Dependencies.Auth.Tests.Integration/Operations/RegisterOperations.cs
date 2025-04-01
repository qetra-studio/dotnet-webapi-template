using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.Enums;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Fakers;
using RichWebApi.Tests.Services;

namespace RichWebApi.Tests.Operations;

public static class RegisterOperations
{
	public sealed class RegisterAndLoginRandomUserOptions
	{
		public Action<TokenValidationParameters>? ConfigureTokenValidation { get; set; }

		public LoginValueKind? LoginValueKind { get; set; }

		public Func<JwtSecurityToken, CancellationToken, Task>? VerifyJwtTokenAsync { get; set; }
		
		public bool SkipMfa { get; set; }
	}

	public static async Task<(AuthSuccessDto,LoginDto)> RegisterAndLoginUserAsync(this IServiceProvider serviceProvider,
	                                                                     Func<IFakerFactory, ValueTask<RegisterDto>>
		                                                                     creds,
	                                                                     Action<RegisterAndLoginRandomUserOptions>?
		                                                                     configureOptions = null)
	{
		var operationOptions = new RegisterAndLoginRandomUserOptions();
		configureOptions?.Invoke(operationOptions);
		var client = serviceProvider.GetRequiredService<IAuthClient>();
		var user = await creds(serviceProvider
			.GetRequiredService<IFakerFactory>());
		await client.RegisterAsync(user);
		var loginValueKind = operationOptions.LoginValueKind ?? LoginValueKind.UserName;
		var loginDto = new LoginDto
		{
			Password = user.Password,
			Login = loginValueKind switch
			{
				LoginValueKind.UserName => user.UserName,
				LoginValueKind.Email => user.Email,
				_ => throw new ArgumentOutOfRangeException(nameof(loginValueKind), loginValueKind, null)
			}
		};
		var login = () => client.LoginWithCredentialsAsync(loginDto);

		var loginAssert = await login.Should().NotThrowAsync();
		loginAssert.Which.ShouldHaveStatusCode(HttpStatusCode.OK);
		var response = loginAssert.Which.Result;

		var verifier = serviceProvider.GetRequiredService<JwtVerifier>();

		await verifier.VerifySignatureAsync(response.AccessToken, o =>
		{
			o.ValidateIssuer = true;
			o.ValidateAudience = true;
			o.ValidateLifetime = true;
			o.ValidIssuer = "identity.richwebapi.com";
			o.ValidAudience = "api.richwebapi.com";
			operationOptions.ConfigureTokenValidation?.Invoke(o);
		});

		await verifier.VerifyTokenAsync(response.AccessToken, async (t, ct) =>
		{
			t.ShouldHavePurposeClaim("access");
			t.ShouldHaveUserId();
			if (operationOptions.VerifyJwtTokenAsync is not null)
			{
				await operationOptions.VerifyJwtTokenAsync(t, ct);
			}
		});

		if (operationOptions.SkipMfa)
		{
			return (response, loginDto);
		}

		var totp = await serviceProvider.SetupMfaAsync(response);
		return await serviceProvider.LoginWithMfaAsync(loginDto, totp);
	}

	public static async Task<(AuthSuccessDto, LoginDto)> LoginWithMfaAsync(this IServiceProvider serviceProvider,
	                                                                       LoginDto login, Totp totp)
	{
		var client = serviceProvider.GetRequiredService<IAuthClient>();
		var credsLogin = () => client.LoginWithCredentialsAsync(login);
		var assertCredsLogin = await credsLogin.Should().ThrowAsync<ApiException<AuthActionRequiredDto>>();
		var credsLoginResponse = assertCredsLogin.Which;
		credsLoginResponse.ShouldHaveStatusCode(HttpStatusCode.Found);
		credsLoginResponse.Result.ErrorCode.Should().Be("two_factor_required");
		client.SetAccessToken(credsLoginResponse.Result);
		var action = () => client.LoginWithMfaAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});
		var assert = await action.Should().NotThrowAsync();
		var response = assert.Subject;
		response.ShouldHaveStatusCode(HttpStatusCode.OK);

		return (response.Result, login);
	}
}