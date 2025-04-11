using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers.Auth;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[EnableCors("auth")]
[ApiController]
[Route("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
	[HttpPost("register", Name = nameof(Register))]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[AllowAnonymous]
	public Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken token)
		=> mediator.Send(new RegisterUser(request), token);

	[HttpPost("login/credentials", Name = nameof(LoginWithCredentials))]
	[ProducesResponseType<AuthSuccessDto>(StatusCodes.Status200OK)]
	[ProducesResponseType<AuthErrorResponseDto>(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType<AuthErrorResponseDto>(StatusCodes.Status302Found)]
	[AllowAnonymous]
	public Task<IActionResult> LoginWithCredentials([FromBody] LoginDto request, CancellationToken token)
		=> mediator.Send(new LoginWithCredentials(request), token);
}