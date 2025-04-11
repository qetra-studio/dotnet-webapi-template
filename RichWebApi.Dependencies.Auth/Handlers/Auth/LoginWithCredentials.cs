using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Models;

namespace RichWebApi.Handlers.Auth;

public record LoginWithCredentials(LoginDto Credentials) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<LoginWithCredentials>
	{
		public Validator(IValidator<LoginDto> v) => RuleFor(x => x.Credentials).SetValidator(v);
	}

	[UsedImplicitly]
	internal class LoginWithCredentialsHandler(
		SignInManager<RichWebApiUser> signInManager,
		UserManager<RichWebApiUser> userManager)
		: IRequestHandler<LoginWithCredentials, IActionResult>
	{
		public async Task<IActionResult> Handle(LoginWithCredentials request, CancellationToken cancellationToken)
		{
			var username = await FindUsernameAsync(request.Credentials);
			if (string.IsNullOrEmpty(username))
			{
				return new UnauthorizedResult();
			}

			var user = await userManager.FindByNameAsync(username);

			if (user == null)
			{
				return new UnauthorizedObjectResult(new AuthErrorResponseDto
				{
					Errors =
					[
						new AuthErrorDto
						{
							ErrorCode = "invalid_credentials",
							Message = "Login attempt failed."
						}
					]
				});
			}
			
			if (await userManager.GetTwoFactorEnabledAsync(user))
			{
				if (string.IsNullOrEmpty(request.Credentials.TwoFactorToken))
				{
					return new UnauthorizedObjectResult(new AuthErrorResponseDto
					{
						Errors =
						[
							new AuthErrorDto
							{
								ErrorCode = "invalid_token",
								Message = "Provided token is invalid."
							}
						]
					});
				}
				
				var isValidTwoFactorToken = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider,
					request.Credentials.TwoFactorToken);

				if (!isValidTwoFactorToken)
				{
					return new UnauthorizedObjectResult(new AuthErrorResponseDto
					{
						Errors =
						[
							new AuthErrorDto
							{
								ErrorCode = "invalid_token",
								Message = "Provided token is invalid."
							}
						]
					});
				}
			}
			
			var result = await signInManager.PasswordSignInAsync(user, request.Credentials.Password, request.Credentials.RememberMe, true);

			if (result.Succeeded)
			{
				return new OkResult();
			}

			if (result.IsLockedOut)
			{
				return new UnauthorizedObjectResult(new AuthErrorResponseDto
				{
					Errors =
					[
						new AuthErrorDto
						{
							ErrorCode = "locked_out",
							Message = "Locked out, try to login later."
						}
					]
				});
			}

			return new UnauthorizedResult();
		}

		private async Task<string> FindUsernameAsync(LoginDto credentials)
		{
			switch (credentials.DefineLoginKind())
			{
				case LoginValueKind.UserName:
					return credentials.Login;
				case LoginValueKind.Email:
					var emailUser = await userManager.FindByEmailAsync(credentials.Login);
					return emailUser?.UserName ?? string.Empty;
				case LoginValueKind.PhoneNumber:
					throw new NotSupportedException();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}