using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Handlers.OpenIddict;
using RichWebApi.Routing;

namespace RichWebApi.Controllers;

[ApiController]
[Route("auth/connect")]
public class ConnectController(
	IMediator mediator) : Controller
{
	[HttpPost("token", Name = nameof(ConnectToken)), Produces("application/json")]
	public Task<IActionResult> ConnectToken(CancellationToken cancellationToken) => mediator.Send(new ConnectToken(), cancellationToken);

	[HttpGet("authorize", Name = nameof(AuthorizeAcceptConsent)),
	 QueryValueRequired("consent", "accept"),
	 ProducesResponseType(StatusCodes.Status403Forbidden),
	 ProducesResponseType(StatusCodes.Status302Found)
	]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public Task<IActionResult> AuthorizeAcceptConsent(CancellationToken cancellationToken)
		=> mediator.Send(new AuthorizeAcceptConsent(), cancellationToken);

	[HttpGet("authorize", Name = nameof(AuthorizeDenyConsent)),
	 QueryValueRequired("consent", "deny"),
	 ProducesResponseType(StatusCodes.Status403Forbidden)]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public Task<IActionResult> AuthorizeDenyConsent(CancellationToken cancellationToken)
		=> mediator.Send(new AuthorizeDenyConsent(), cancellationToken);

	[HttpGet("authorize", Name = nameof(ConnectAuthorize) + "GET"),
	 HttpPost("authorize", Name = nameof(ConnectAuthorize) + "POST"),
	 NoQueryValue("consent"),
	 ProducesResponseType(StatusCodes.Status403Forbidden),
	 ProducesResponseType(StatusCodes.Status302Found)]
	public Task<IActionResult> ConnectAuthorize(CancellationToken cancellationToken)
		=> mediator.Send(new ConnectAuthorize(), cancellationToken);
	
	[HttpGet("userinfo", Name = nameof(ConnectUserInfo) + "GET"),
	 HttpPost("userinfo", Name = nameof(ConnectUserInfo) + "POST"),
	 ProducesResponseType<Dictionary<string, object>>(StatusCodes.Status200OK),
	 ProducesResponseType(StatusCodes.Status302Found)]
	public Task<IActionResult> ConnectUserInfo(CancellationToken cancellationToken)
		=> mediator.Send(new ConnectUserInfo(), cancellationToken);
}