using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Authorization;
using RichWebApi.Enums;
using RichWebApi.Handlers.Profile;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[Route("profile")]
[ApiController]
[Authorize]
[AuthPurpose(RichWebApiAuthPurpose.Access)]
public sealed class ProfileController(IMediator mediator) : ControllerBase
{
	[HttpGet(Name = nameof(GetCurrentUserProfile))]
	[ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
	public Task<IActionResult> GetCurrentUserProfile(CancellationToken cancellationToken)
		=> mediator.Send(new GetCurrentUserProfile(), cancellationToken);
}