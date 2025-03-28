using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[Route("auth/mfa")]
[Authorize]
public class MfaController(IMediator mediator) : ControllerBase
{
	[HttpPost("setup")]
	[ProducesResponseType<MfaSetupDto>(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorResponseDto>(StatusCodes.Status401Unauthorized)]
	public Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
		=> mediator.Send(new SetupMfa(), cancellationToken);

	[HttpPost("verify")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorResponseDto>(StatusCodes.Status401Unauthorized)]
	public Task<IActionResult> VerifyMfa([FromBody] VerifyMfaDto dto, CancellationToken cancellationToken)
		=> mediator.Send(new VerifyMfaSetup(dto), cancellationToken);
}