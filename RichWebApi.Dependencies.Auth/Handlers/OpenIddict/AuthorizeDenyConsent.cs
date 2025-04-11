using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;

namespace RichWebApi.Handlers.OpenIddict;

public sealed record AuthorizeDenyConsent : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<AuthorizeDenyConsent>;

	[UsedImplicitly]
	internal class AuthorizeDenyConsentHandler : IRequestHandler<AuthorizeDenyConsent, IActionResult>
	{
		public Task<IActionResult> Handle(AuthorizeDenyConsent request, CancellationToken cancellationToken)
			=> Task.FromResult<IActionResult>(new ForbidResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
	}
}