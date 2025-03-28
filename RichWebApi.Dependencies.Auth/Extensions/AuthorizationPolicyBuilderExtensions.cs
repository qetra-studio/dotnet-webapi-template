using Microsoft.AspNetCore.Authorization;
using RichWebApi.Constants;
using RichWebApi.Enums;

namespace RichWebApi.Extensions;

public static class AuthorizationPolicyBuilderExtensions
{

	public static AuthorizationPolicyBuilder AddAccessPolicy(this AuthorizationPolicyBuilder builder, Action<AuthorizationPolicyBuilder> configurePolicy)
	{
		configurePolicy(builder.RequirePurpose(RichWebApiJwtPurpose.Access));
		return builder;
	}

	public static AuthorizationPolicyBuilder RequirePurpose(this AuthorizationPolicyBuilder builder,
													  RichWebApiJwtPurpose purpose)
	{
		var value = purpose.ToString("G").ToLower();
		return builder.RequireClaim(RichWebApiJwtClaimTypes.Purpose, value);
	}
}