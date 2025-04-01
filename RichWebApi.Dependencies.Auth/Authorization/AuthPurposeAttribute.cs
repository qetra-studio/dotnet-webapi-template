using Microsoft.AspNetCore.Authorization;
using RichWebApi.Enums;
using RichWebApi.Extensions;

namespace RichWebApi.Authorization;

public sealed class AuthPurposeAttribute(RichWebApiAuthPurpose purpose) : AuthorizeAttribute($"purpose:{purpose:G}"), IHasAuthorization
{
	public static void ConfigureAuthorization(AuthorizationBuilder builder)
	{
		foreach (var action in Enum.GetValues<RichWebApiAuthPurpose>().Except([RichWebApiAuthPurpose.Unknown]))
		{
			builder.AddPolicy($"purpose:{action:G}", x => x.RequirePurpose(action));
		}
	}
}