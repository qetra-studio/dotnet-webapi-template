using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[Route(".well-known")]
[AllowAnonymous]
public class WellKnownController(IMediator mediator) : ControllerBase
{
	[HttpGet("jwks.json", Name = nameof(GetJwks))]
	[ProducesResponseType<AuthKeysDto>(StatusCodes.Status200OK)]
	public Task<IActionResult> GetJwks(CancellationToken cancellationToken)
		=> mediator.Send(new GetKeys(), cancellationToken);
}