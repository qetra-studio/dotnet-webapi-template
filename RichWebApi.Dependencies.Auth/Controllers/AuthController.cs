using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers;
using RichWebApi.Models;

namespace RichWebApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IMediator mediator, IHttpContextAccessor accessor) : ControllerBase
{
	[HttpPost("register", Name = nameof(Register))]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(AuthErrorDto))]
	[AllowAnonymous]
	public Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken token)
		=> mediator.Send(new RegisterUser(request), token);
	
	[HttpPost("login", Name = nameof(Login))]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthSuccessDto))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthErrorDto[]))]
	[AllowAnonymous]
	public Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken token)
		=> mediator.Send(new Login(request), token);
}