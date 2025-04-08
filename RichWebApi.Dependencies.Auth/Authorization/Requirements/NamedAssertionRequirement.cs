using Microsoft.AspNetCore.Authorization;

namespace RichWebApi.Authorization.Requirements;

internal sealed class NamedAssertionRequirement(
	Lazy<string> name,
	Func<AuthorizationHandlerContext, ValueTask<bool>> handler)
	: IAuthorizationRequirement, IAuthorizationHandler
{
	public async Task HandleAsync(AuthorizationHandlerContext context)
	{
		if (await handler(context).ConfigureAwait(false))
		{
			context.Succeed(this);
		}
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"{name.Value}: Assertion should evaluate to true.";
}