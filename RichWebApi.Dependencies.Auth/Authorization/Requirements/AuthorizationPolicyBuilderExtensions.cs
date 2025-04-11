using Microsoft.AspNetCore.Authorization;

namespace RichWebApi.Authorization.Requirements;

public static class AuthorizationPolicyBuilderExtensions
{
	public static AuthorizationPolicyBuilder RequireNamedAssertion(this AuthorizationPolicyBuilder builder, Lazy<string> name, Func<AuthorizationHandlerContext, ValueTask<bool>> assertion)
		=> builder.AddRequirements(new NamedAssertionRequirement(name, assertion));
}