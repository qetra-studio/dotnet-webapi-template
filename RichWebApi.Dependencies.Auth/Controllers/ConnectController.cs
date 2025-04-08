using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using RichWebApi.Entities.Identity;
using RichWebApi.Handlers.OpenIddict;
using RichWebApi.Models;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;
using ISystemClock = Microsoft.Extensions.Internal.ISystemClock;

namespace RichWebApi.Controllers;

[ApiController]
[Route("auth/connect")]
public class ConnectController(
	IMediator mediator,
	ISystemClock systemClock,
	IOpenIddictApplicationManager applicationManager,
	IOpenIddictAuthorizationManager authorizationManager,
	IOpenIddictScopeManager scopeManager,
	UserManager<RichWebApiUser> manager) : ControllerBase
{
	[HttpPost("token"), Produces("application/json")]
	public Task<IActionResult> Exchange(CancellationToken cancellationToken)
	{
		var request = HttpContext.GetOpenIddictServerRequest();
		return request == null
			? Task.FromResult<IActionResult>(new NotFoundResult())
			: mediator.Send(new Exchange(request), cancellationToken);
	}

	[HttpGet("authorize"),
	 HttpPost("authorize")]
	public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
	{
		var request = HttpContext.GetOpenIddictServerRequest();
		if (request == null)
		{
			return new UnauthorizedResult();
		}

		var result = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

		switch (result)
		{
			case { Succeeded: false }:
			case var _ when request.HasPromptValue(OpenIddictConstants.PromptValues.Login):
			case var _ when request.MaxAge is 0:
			case var _ when request.MaxAge.HasValue
							&& result.Properties?.IssuedUtc != null
							&& systemClock.UtcNow - result.Properties.IssuedUtc
							> TimeSpan.FromSeconds(request.MaxAge.Value):
				{
					if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
					{
						return Forbid(
							authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
							properties: new AuthenticationProperties(new Dictionary<string, string?>
							{
								[Properties.Error] = OpenIddictConstants.Errors.LoginRequired,
								[Properties.ErrorDescription] = "The user is not logged in."
							}));
					}

					return Redirect($"https://local.richwebapi.com/login{QueryString.Create(
						Request.HasFormContentType
							? Request.Form
							: Request.Query)}");

					return new UnauthorizedObjectResult(new AuthErrorResponseDto
					{
						Errors =
						[
							new AuthErrorDto
						{
							ErrorCode = "login_required",
							Message = "Complete login in order to continue."
						}
						]
					});
				}
		}

		var user = await manager.GetUserAsync(result.Principal!);

		var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
						  ?? throw new InvalidOperationException(
							  "Details concerning the calling client application cannot be found.");

		// Retrieve the permanent authorizations associated with the user and the calling client application.
		var authorizations = authorizationManager.FindAsync(
				subject: await manager.GetUserIdAsync(user!),
				client: await applicationManager.GetIdAsync(application, cancellationToken),
				status: OpenIddictConstants.Statuses.Valid,
				type: OpenIddictConstants.AuthorizationTypes.Permanent,
				scopes: request.GetScopes(), cancellationToken)
			.ToBlockingEnumerable(cancellationToken)
			.ToList();

		switch (await applicationManager.GetConsentTypeAsync(application, cancellationToken))
		{
			// If the consent is external (e.g when authorizations are granted by a sysadmin),
			// immediately return an error if no authorization can be found in the database.
			case OpenIddictConstants.ConsentTypes.External when authorizations.Count is 0:
				return Forbid(
					authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[OpenIddictServerAspNetCoreConstants.Properties.Error] =
							OpenIddictConstants.Errors.ConsentRequired,
						[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
							"The logged in user is not allowed to access this client application."
					}));

			// If the consent is implicit or if an authorization was found,
			// return an authorization response without displaying the consent form.
			case OpenIddictConstants.ConsentTypes.Implicit:
			case OpenIddictConstants.ConsentTypes.External when authorizations.Count is not 0:
			case OpenIddictConstants.ConsentTypes.Explicit when authorizations.Count is not 0
																&& !request.HasPromptValue(OpenIddictConstants
																	.PromptValues.Consent):
				// Create the claims-based identity that will be used by OpenIddict to generate tokens.
				var identity = new ClaimsIdentity(
					authenticationType: TokenValidationParameters.DefaultAuthenticationType,
					nameType: OpenIddictConstants.Claims.Name,
					roleType: OpenIddictConstants.Claims.Role);

				// Add the claims that will be persisted in the tokens.
				identity.SetClaim(OpenIddictConstants.Claims.Subject, await manager.GetUserIdAsync(user!))
					.SetClaim(OpenIddictConstants.Claims.Email, await manager.GetEmailAsync(user!))
					.SetClaim(OpenIddictConstants.Claims.Name, await manager.GetUserNameAsync(user!))
					.SetClaim(OpenIddictConstants.Claims.PreferredUsername, await manager.GetUserNameAsync(user!))
					.SetClaims(OpenIddictConstants.Claims.Role, [.. await manager.GetRolesAsync(user!)]);

				// Note: in this sample, the granted scopes match the requested scope
				// but you may want to allow the user to uncheck specific scopes.
				// For that, simply restrict the list of scopes before calling SetScopes.
				identity.SetScopes(request.GetScopes());
				identity.SetResources(scopeManager.ListResourcesAsync(identity.GetScopes(), cancellationToken)
					.ToBlockingEnumerable(cancellationToken).ToList());

				// Automatically create a permanent authorization to avoid requiring explicit consent
				// for future authorization or token requests containing the same scopes.
				var authorization = authorizations.LastOrDefault();
				authorization ??= await authorizationManager.CreateAsync(
					identity: identity,
					subject: await manager.GetUserIdAsync(user!),
					client: await applicationManager.GetIdAsync(application, cancellationToken),
					type: OpenIddictConstants.AuthorizationTypes.Permanent,
					scopes: identity.GetScopes(), cancellationToken: cancellationToken);

				identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization, cancellationToken));
				identity.SetDestinations(GetDestinations);

				return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

			// At this point, no authorization was found in the database and an error must be returned
			// if the client application specified prompt=none in the authorization request.
			case OpenIddictConstants.ConsentTypes.Explicit
				when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
			case OpenIddictConstants.ConsentTypes.Systematic
				when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
				return Forbid(
					authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[OpenIddictServerAspNetCoreConstants.Properties.Error] =
							OpenIddictConstants.Errors.ConsentRequired,
						[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
							"Interactive user consent is required."
					}));
		}

		// Note: the same check is already made in the other action but is repeated
		// here to ensure a malicious user can't abuse this POST-only endpoint and
		// force it to return a valid response without the external authorization.
		if (authorizations.Count is 0 && await applicationManager.HasConsentTypeAsync(application, OpenIddictConstants.ConsentTypes.External, cancellationToken))
		{
			return Forbid(
				authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
				properties: new AuthenticationProperties(new Dictionary<string, string?>
				{
					[OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.ConsentRequired,
					[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
						"The logged in user is not allowed to access this client application."
				}));
		}

		// Create the claims-based identity that will be used by OpenIddict to generate tokens.
		var newIdentity = new ClaimsIdentity(
			authenticationType: TokenValidationParameters.DefaultAuthenticationType,
			nameType: OpenIddictConstants.Claims.Name,
			roleType: OpenIddictConstants.Claims.Role);

		// Add the claims that will be persisted in the tokens.
		newIdentity.SetClaim(OpenIddictConstants.Claims.Subject, await manager.GetUserIdAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Email, await manager.GetEmailAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Name, await manager.GetUserNameAsync(user))
				.SetClaim(OpenIddictConstants.Claims.PreferredUsername, await manager.GetUserNameAsync(user))
				.SetClaims(OpenIddictConstants.Claims.Role, [.. (await manager.GetRolesAsync(user))]);

		// Note: in this sample, the granted scopes match the requested scope
		// but you may want to allow the user to uncheck specific scopes.
		// For that, simply restrict the list of scopes before calling SetScopes.
		newIdentity.SetScopes(request.GetScopes());
		newIdentity.SetResources(scopeManager.ListResourcesAsync(newIdentity.GetScopes(), cancellationToken).ToBlockingEnumerable(cancellationToken).ToList());

		// Automatically create a permanent authorization to avoid requiring explicit consent
		// for future authorization or token requests containing the same scopes.
		var a = authorizations.LastOrDefault();
		a ??= await authorizationManager.CreateAsync(
			identity: newIdentity,
			subject: await manager.GetUserIdAsync(user),
			client: await applicationManager.GetIdAsync(application),
			type: OpenIddictConstants.AuthorizationTypes.Permanent,
			scopes: newIdentity.GetScopes(), cancellationToken: cancellationToken);

		newIdentity.SetAuthorizationId(await authorizationManager.GetIdAsync(a, cancellationToken));
		newIdentity.SetDestinations(GetDestinations);
		// Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
		return SignIn(new ClaimsPrincipal(newIdentity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);


		static IEnumerable<string> GetDestinations(Claim claim)
		{
			// Note: by default, claims are NOT automatically included in the access and identity tokens.
			// To allow OpenIddict to serialize them, you must attach them a destination, that specifies
			// whether they should be included in access tokens, in identity tokens or in both.

			switch (claim.Type)
			{
				case OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.PreferredUsername:
					yield return OpenIddictConstants.Destinations.AccessToken;

					if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Profile))
						yield return OpenIddictConstants.Destinations.IdentityToken;

					yield break;

				case OpenIddictConstants.Claims.Email:
					yield return OpenIddictConstants.Destinations.AccessToken;

					if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
						yield return OpenIddictConstants.Destinations.IdentityToken;

					yield break;

				case OpenIddictConstants.Claims.Role:
					yield return OpenIddictConstants.Destinations.AccessToken;

					if (claim.Subject!.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
						yield return OpenIddictConstants.Destinations.IdentityToken;

					yield break;

				// Never include the security stamp in the access and identity tokens, as it's a secret value.
				case "AspNet.Identity.SecurityStamp": yield break;

				default:
					yield return OpenIddictConstants.Destinations.AccessToken;
					yield break;
			}
		}
	}
}