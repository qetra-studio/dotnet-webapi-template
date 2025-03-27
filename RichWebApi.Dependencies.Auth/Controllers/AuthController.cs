using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
	[HttpPost("register", Name = nameof(Register))]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorDto>(StatusCodes.Status400BadRequest)]
	[AllowAnonymous]
	public Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken token)
		=> mediator.Send(new RegisterUser(request), token);
	
	[HttpPost("login/credentials", Name = nameof(LoginWithCredentials))]
	[ProducesResponseType<AuthSuccessDto>(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorDto[]>(StatusCodes.Status401Unauthorized)]
	[AllowAnonymous]
	public Task<IActionResult> LoginWithCredentials([FromBody] LoginDto request, CancellationToken token)
		=> mediator.Send(new LoginWithCredentials(request), token);
	
	[HttpPost("login/mfa", Name = nameof(LoginWithMfa))]
	[ProducesResponseType<AuthSuccessDto>(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorDto[]>(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[Authorize]
	public Task<IActionResult> LoginWithMfa([FromBody] VerifyMfaDto request, CancellationToken token)
		=> mediator.Send(new LoginWithMfa(request), token);
}