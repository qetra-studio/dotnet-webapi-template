using System.Text.Json;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using RichWebApi.Entities.Identity;
using RichWebApi.Models;
using RichWebApi.Services;

namespace RichWebApi.Handlers;

public record LoginWithMfa(VerifyMfaDto Mfa) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<LoginWithMfa>
	{
		public Validator(IValidator<VerifyMfaDto> validator)
			=> RuleFor(x => x.Mfa).SetValidator(validator);
	}

	[UsedImplicitly]
	internal class LoginWithMfaHandler(
		IRichWebApiUserContextAccessor accessor,
		UserManager<RichWebApiUser> manager,
		IJwtTokenIssuer jwtTokenIssuer) : IRequestHandler<LoginWithMfa, IActionResult>
	{
		public async Task<IActionResult> Handle(LoginWithMfa request, CancellationToken cancellationToken)
		{
			var user = await accessor.User;
			if (user is null)
			{
				return new UnauthorizedResult();
			}

			var isValid = await manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider,
				request.Mfa.Token);

			if (!isValid)
			{
				return new UnauthorizedObjectResult(new AuthErrorResponseDto()
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

			var token = await jwtTokenIssuer.IssueUserTokenAsync(user, cancellationToken);
			return new ObjectResult(new AuthSuccessDto
			{
				AccessToken = token,
				TokenType = JwtBearerDefaults.AuthenticationScheme
			});
		}
	}
}