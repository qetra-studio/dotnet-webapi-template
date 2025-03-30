using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
	}

	public static async Task<AuthSuccessDto> RegisterAndLoginUserAsync(this IServiceProvider serviceProvider,
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

		return response;
	}
}