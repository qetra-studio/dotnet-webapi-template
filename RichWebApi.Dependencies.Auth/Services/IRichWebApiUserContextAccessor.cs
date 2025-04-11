using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RichWebApi.Entities.Identity;
using RichWebApi.Utilities;

namespace RichWebApi.Services;

public interface IRichWebApiUserContextAccessor
{
	Guid? UserId { get; }
	AsyncLazy<RichWebApiUser?> User { get; }
	
	ClaimsPrincipal? UserPrincipal { get; }
	
	HttpContext? HttpContext { get; }
}