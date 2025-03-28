using Microsoft.AspNetCore.Authorization;
using RichWebApi.Enums;

namespace RichWebApi.Extensions;

public static class AuthorizationBuilderExtensions
{

	public static AuthorizationBuilder AddAccessPolicy(this AuthorizationBuilder builder, string name, Action<AuthorizationPolicyBuilder> configurePolicy)
		=> builder.AddPolicy(name, x => configurePolicy(x.RequirePurpose(RichWebApiJwtPurpose.Access)));
}