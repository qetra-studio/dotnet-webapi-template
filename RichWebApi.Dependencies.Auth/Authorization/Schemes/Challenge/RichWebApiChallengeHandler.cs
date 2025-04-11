using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RichWebApi.Authorization.Schemes.Challenge;

internal sealed class RichWebApiChallengeHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		// This scheme doesn't authenticate; it just handles challenges
		return Task.FromResult(AuthenticateResult.NoResult());
	}

	protected override Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		var returnUrl = string.IsNullOrEmpty(properties.RedirectUri)
			? Context.Request.Path + Context.Request.QueryString
			: properties.RedirectUri;
		var path = DefinePath(properties);
		var builder =
			new StringBuilder($"https://local.richwebapi.com/{path}?returnUrl={Uri.EscapeDataString(returnUrl)}");
		if (properties.Parameters.TryGetValue("query", out var query))
		{
			var stringified = query?.ToString();
			if (!string.IsNullOrEmpty(stringified))
			{
				builder.Append('&')
					.Append(stringified);
			}
		}

		var redirectUrl = builder.ToString();
		Context.Response.Redirect(redirectUrl);
		return Task.CompletedTask;
	}

	private static string DefinePath(AuthenticationProperties properties)
	{
		const string loginPath = "login";
		if (!properties.Parameters.TryGetValue("path", out var pathParameter))
		{
			return loginPath;
		}

		var pathString = pathParameter?.ToString();
		return string.IsNullOrEmpty(pathString)
			? loginPath
			: pathString;
	}
}