using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RichWebApi.Services;

internal sealed class RichWebApiUserContextAccessor(IHttpContextAccessor accessor) : IRichWebApiUserContextAccessor
{
	public Guid? UserId { get; }
		= Guid.TryParse(
			accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
			out var userId)
			? userId
			: null;
}