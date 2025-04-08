using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using RichWebApi.Authorization;
using RichWebApi.Authorization.Requirements;
using RichWebApi.Enums;

namespace RichWebApi.Extensions;

public static class AuthorizationBuilderExtensions
{
	public static AuthorizationBuilder AddAccessPolicy(this AuthorizationBuilder builder, string name,
													   Action<AuthorizationPolicyBuilder> configurePolicy)
		=> builder.AddPolicy(name,
			x => configurePolicy(x
				.RequireAuthenticatedUser()
				.RequireSecurityStamp()
				.RequirePurpose(RichWebApiAuthPurpose.Access)));
	public static AuthorizationBuilder AddScopePolicy(this AuthorizationBuilder builder, string scope)
		=> builder.AddPolicy($"scp:{scope}", x => x
			.RequireNamedAssertion(new Lazy<string>(() => $"Verify scope '{scope}'"),
				ctx => new ValueTask<bool>(ctx.User.HasScope(scope))));
}