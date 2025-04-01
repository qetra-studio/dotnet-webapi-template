using Microsoft.AspNetCore.Authorization;
using RichWebApi.Constants;
using RichWebApi.Enums;

namespace RichWebApi.Extensions;

public static class AuthorizationPolicyBuilderExtensions
{
	public static AuthorizationPolicyBuilder RequirePurpose(this AuthorizationPolicyBuilder builder,
													  RichWebApiAuthPurpose purpose)
	{
		var value = purpose.ToString("G").ToLower();
		return builder.RequireClaim(RichWebApiJwtClaimTypes.Purpose, value);
	}

	public static AuthorizationPolicyBuilder RequirePurpose(this AuthorizationPolicyBuilder builder) => builder.RequireClaim(RichWebApiJwtClaimTypes.Purpose);

	public static AuthorizationPolicyBuilder RequireAction(this AuthorizationPolicyBuilder builder,
														   RichWebApiAuthActions action)
	{
		var value = action.ToString("G").ToLower();
		return builder.RequireClaim(RichWebApiJwtClaimTypes.Action, value);
	}
}