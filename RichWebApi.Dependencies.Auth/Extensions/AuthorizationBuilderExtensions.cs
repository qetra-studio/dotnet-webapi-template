using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using RichWebApi.Authorization.Requirements;

namespace RichWebApi.Extensions;

public static class AuthorizationBuilderExtensions
{
	public static AuthorizationBuilder AddScopePolicy(this AuthorizationBuilder builder, string scope)
		=> builder.AddPolicy($"scp:{scope}", x => x
			.RequireNamedAssertion(new Lazy<string>(() => $"Verify scope '{scope}'"),
				ctx => new ValueTask<bool>(ctx.User.HasScope(scope))));
}