using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using RichWebApi.Constants;
using RichWebApi.Services;

namespace RichWebApi.Authorization;

public sealed class SecurityStampRequirement : IAuthorizationRequirement
{
	[UsedImplicitly]
	public sealed class Handler(IRichWebApiUserContextAccessor accessor) : AuthorizationHandler<SecurityStampRequirement>
	{

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SecurityStampRequirement requirement)
		{
			if (!context.User.Identities.Any(x => x.IsAuthenticated))
			{
				return;
			}

			var user = await accessor.User;
			var stampClaim = context.User.FindFirst(RichWebApiJwtClaimTypes.Stamp);

			if (user is null)
			{
				return;
			}

			if (string.IsNullOrEmpty(stampClaim?.Value))
			{
				return;
			}

			if (string.Compare(user.SecurityStamp, stampClaim.Value, StringComparison.OrdinalIgnoreCase) == 0)
			{
				context.Succeed(requirement);
			}
		}
	}
}