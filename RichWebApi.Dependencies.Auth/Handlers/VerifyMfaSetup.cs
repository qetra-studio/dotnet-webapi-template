using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;
using RichWebApi.Entities.Identity;
using RichWebApi.Models;
using RichWebApi.Services;

namespace RichWebApi.Handlers;

public record VerifyMfaSetup(VerifyMfaDto Setup) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<VerifyMfaSetup>
	{
		public Validator(IValidator<VerifyMfaDto> validator) => RuleFor(x => x.Setup).SetValidator(validator);
	}

	[UsedImplicitly]
	internal class VerifyMfaSetupHandler(IRichWebApiUserContextAccessor accessor,
	                                     ISystemClock clock,
	                                     UserManager<RichWebApiUser> manager) : IRequestHandler<VerifyMfaSetup, IActionResult>
	{
		public async Task<IActionResult> Handle(VerifyMfaSetup request, CancellationToken cancellationToken)
		{
			var user = await accessor.User;
			if (user is null)
			{
				return new UnauthorizedResult();
			}

			var isValid = await manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider,
				request.Setup.Token);
			if (!isValid)
			{
				return new BadRequestObjectResult(new AuthErrorDto
				{
					ErrorCode = "invalid_token",
					Message = "Provided token is invalid."
				});
			}

			user.TwoFactorEnabled = true;
			user.TwoFactorEnabledAt = clock.UtcNow;
			
			await manager.UpdateAsync(user);
			
			return new OkResult();
		}
	}
}