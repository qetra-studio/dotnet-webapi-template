using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Models;
using RichWebApi.Services;

namespace RichWebApi.Handlers;

public record LoginWithCredentials(LoginDto Credentials) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<LoginWithCredentials>
	{
		public Validator(IValidator<LoginDto> v) => RuleFor(x => x.Credentials).SetValidator(v);
	}

	[UsedImplicitly]
	internal class LoginHandler(
		SignInManager<RichWebApiUser> signInManager,
		IJwtTokenIssuer jwtTokenIssuer,
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

			var result = await signInManager.CheckPasswordSignInAsync(user, request.Credentials.Password, true);

			if (await userManager.GetTwoFactorEnabledAsync(user))
			{
				var token = await jwtTokenIssuer.IssueTwoFactorTokenAsync(user, cancellationToken);
				return new ObjectResult(new AuthActionRequiredDto
				{
					ErrorCode = "two_factor_required",
					Message = "Complete 2FA in order to continue.",
					AccessToken = token
				})
				{
					StatusCode = StatusCodes.Status302Found
				};
			}

			if (result.Succeeded)
			{
				var token = await jwtTokenIssuer.IssueUserTokenAsync(user, cancellationToken);
				return new ObjectResult(new AuthSuccessDto
				{
					AccessToken = token
				});
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