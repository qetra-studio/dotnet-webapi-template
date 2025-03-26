using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers;

namespace RichWebApi.Controllers;

[Route(".well-known")]
[AllowAnonymous]
public class WellKnownController(IMediator mediator) : ControllerBase
{
	[HttpGet("jwks.json", Name = nameof(GetJwks))]
	public Task<IActionResult> GetJwks(CancellationToken cancellationToken)
		=> mediator.Send(new GetKeys(), cancellationToken);
}