using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RichWebApi.Config;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Models;
using RichWebApi.Services;
using RichWebApi.Services.Jwt;

namespace RichWebApi.Handlers.Mfa;

public record SetupMfa : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<SetupMfa>;

	[UsedImplicitly]
	internal class SetupMfaHandler(IRichWebApiUserContextAccessor accessor,
								   IJwtTokenIssuer jwtTokenIssuer,
								   UserManager<RichWebApiUser> manager,
								   IOptionsMonitor<MfaConfig> config) : IRequestHandler<SetupMfa, IActionResult>
	{
		public async Task<IActionResult> Handle(SetupMfa request, CancellationToken cancellationToken)
		{
			var user = await accessor.User;
			if (user is null)
			{
				return new UnauthorizedResult();
			}

			var key = await manager.GetAuthenticatorKeyAsync(user);
			if (!string.IsNullOrEmpty(key))
			{
				return await MfaResponse(user, key, cancellationToken);
			}
			var result = await manager.ResetAuthenticatorKeyAsync(user);
			if (!result.Succeeded)
			{
				return result.ToUnauthorizedResult();
			}

			key = await manager.GetAuthenticatorKeyAsync(user);

			return await MfaResponse(user, key!, cancellationToken);
		}

		private async Task<IActionResult> MfaResponse(RichWebApiUser user, string key, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(user.Email))
			{
				return new UnauthorizedResult();
			}

			var configValue = config.CurrentValue;
			var issuer = Uri.EscapeDataString(configValue.Issuer);
			var uri =
				$"otpauth://totp/{issuer}:{Uri.EscapeDataString(user.Email)}?secret={key}&issuer={issuer}&digits={configValue.Digits}";
			var jwt = await jwtTokenIssuer.IssueAuthActionTokenAsync(user, RichWebApiAuthActions.SetupMfa, cancellationToken);
			return new ObjectResult(new MfaSetupDto
			{
				Key = key,
				Uri = uri,
				AccessToken = jwt.Token,
				ExpiresIn = (int)jwt.ExpiresIn.TotalSeconds,
				TokenType = jwt.Type
			});
		}
	}
}