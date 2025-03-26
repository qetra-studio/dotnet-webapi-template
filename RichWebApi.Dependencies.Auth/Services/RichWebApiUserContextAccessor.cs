using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Identity;
using RichWebApi.Utilities;

namespace RichWebApi.Services;

internal sealed class RichWebApiUserContextAccessor(IHttpContextAccessor accessor, UserManager<RichWebApiUser> manager)
	: IRichWebApiUserContextAccessor
{
	public Guid? UserId { get; }
		= accessor.HttpContext?.User is { } user && manager.GetUserId(user) is { } uid
			? Guid.Parse(uid)
			: null;

	public AsyncLazy<RichWebApiUser?> User { get; } = new(() =>
	{
		if (accessor.HttpContext?.User is { } user)
		{
			return manager.GetUserAsync(user);
		}

		return Task.FromResult<RichWebApiUser?>(null);
	});
}