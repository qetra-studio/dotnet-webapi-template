using Microsoft.AspNetCore.Authorization;

namespace RichWebApi.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
	public static AuthorizationPolicyBuilder RequireSecurityStamp(this AuthorizationPolicyBuilder builder)
		=> builder.AddRequirements(new SecurityStampRequirement());
}