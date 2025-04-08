using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using RichWebApi.Extensions;

namespace RichWebApi.Authorization;

public sealed class ScopeAttribute(string scope) : AuthorizeAttribute($"scp:{scope}"), IHasAuthorization
{
	public static void ConfigureAuthorization(AuthorizationBuilder builder)
	{
		string[] scopes = [OpenIddictConstants.Scopes.Profile];
		foreach (var s in scopes)
		{
			builder.AddScopePolicy(s);
		}
	}
}