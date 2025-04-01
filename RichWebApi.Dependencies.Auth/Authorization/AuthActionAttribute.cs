using Microsoft.AspNetCore.Authorization;
using RichWebApi.Enums;
using RichWebApi.Extensions;

namespace RichWebApi.Authorization;

public sealed class AuthActionAttribute(RichWebApiAuthActions action)
	: AuthorizeAttribute($"action:{action:G}"), IHasAuthorization
{
	public static void ConfigureAuthorization(AuthorizationBuilder builder)
	{
		foreach (var action in Enum.GetValues<RichWebApiAuthActions>().Except([RichWebApiAuthActions.Unknown]))
		{
			builder.AddPolicy($"action:{action:G}", x => x.RequireAction(action));
		}
	}
}