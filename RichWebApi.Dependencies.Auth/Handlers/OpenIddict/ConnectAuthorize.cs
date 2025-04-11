using System.Security.Claims;
using System.Text;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.Server.AspNetCore;
using RichWebApi.Entities.Identity;
using RichWebApi.Entities.OpenIddict;
using RichWebApi.Extensions;
using RichWebApi.Services;
using RichWebApi.Services.OpenIddict;
using RichWebApi.Validation;
using ISystemClock = Microsoft.Extensions.Internal.ISystemClock;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;
using static RichWebApi.Authorization.Schemes.Challenge.RichWebApiChallengeParams;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace RichWebApi.Handlers.OpenIddict;

public record ConnectAuthorize : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<ConnectAuthorize>
	{
		public Validator(IHttpContextAccessor accessor)
			=> RuleFor(x => x)
				.HasOpenIddictServerRequest(accessor);
	}

	[UsedImplicitly]
	internal class ConnectAuthorizeHandler(
		IRichWebApiUserContextAccessor accessor,
		ISystemClock systemClock,
		UserManager<RichWebApiUser> userManager,
		OpenIddictApplicationManager<RichWebApiOpenApplication> applicationManager,
		OpenIddictAuthorizationManager<RichWebApiOpenAuthorization> authorizationManager,
		IClaimDestinationsProvider destinationsProvider,
		OpenIddictScopeManager<RichWebApiOpenScope> scopeManager) : IRequestHandler<ConnectAuthorize, IActionResult>
	{
		public async Task<IActionResult> Handle(ConnectAuthorize _, CancellationToken cancellationToken)
		{
			var httpContext = accessor.HttpContext!;
			var factory = httpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
			var tempData = factory.GetTempData(httpContext);
			var request = httpContext.GetOpenIddictServerRequest()!;

			// Try to retrieve the user principal stored in the authentication cookie and redirect
			// the user agent to the login page (or to an external provider) in the following cases:
			//
			//  - If the user principal can't be extracted or the cookie is too old.
			//  - If prompt=login was specified by the client application.
			//  - If max_age=0 was specified by the client application (max_age=0 is equivalent to prompt=login).
			//  - If a max_age parameter was provided and the authentication cookie is not considered "fresh" enough.
			//
			// For scenarios where the default authentication handler configured in the ASP.NET Core
			// authentication options shouldn't be used, a specific scheme can be specified here.
			var result = await httpContext.AuthenticateAsync();
			if (result is not { Succeeded: true }
			    || ((request.HasPromptValue(OpenIddictConstants.PromptValues.Login)
			         || request.MaxAge is 0
			         || (request.MaxAge != null
			             && result.Properties?.IssuedUtc != null
			             && systemClock.UtcNow - result.Properties.IssuedUtc
			             > TimeSpan.FromSeconds(request.MaxAge.Value)))
			        && tempData["IgnoreAuthenticationChallenge"] is null or false))
			{
				if (request.HasPromptValue(OpenIddictConstants.PromptValues.None))
				{
					return new ForbidResult(
						OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
						properties: new AuthenticationProperties(new Dictionary<string, string?>
						{
							[Properties.Error] = OpenIddictConstants.Errors.LoginRequired,
							[Properties.ErrorDescription] = "The user is not logged in."
						}));
				}

				// To avoid endless login endpoint -> authorization endpoint redirects, a special temp data entry is
				// used to skip the challenge if the user agent has already been redirected to the login endpoint.
				//
				// Note: this flag doesn't guarantee that the user has accepted to re-authenticate. If such a guarantee
				// is needed, the existing authentication cookie MUST be deleted AND revoked (e.g using ASP.NET Core
				// Identity's security stamp feature with an extremely short revalidation time span) before triggering
				// a challenge to redirect the user agent to the login endpoint.
				tempData["IgnoreAuthenticationChallenge"] = true;

				return new ChallengeResult(new AuthenticationProperties
				{
					RedirectUri = httpContext.Request.PathBase
					              + httpContext.Request.Path
					              + httpContext.Request.QueryString
				});
			}

			var user = await userManager.GetUserAsync(result.Principal!);

			var application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
			                  ?? throw new InvalidOperationException(
				                  "Details concerning the calling client application cannot be found.");

			// Retrieve the permanent authorizations associated with the user and the calling client application.
			var authorizations = await authorizationManager.FindAsync(
					subject: await userManager.GetUserIdAsync(user!),
					client: await applicationManager.GetIdAsync(application, cancellationToken),
					status: OpenIddictConstants.Statuses.Valid,
					type: OpenIddictConstants.AuthorizationTypes.Permanent,
					scopes: request.GetScopes(), cancellationToken)
				.ToListAsync(cancellationToken);

			switch (await applicationManager.GetConsentTypeAsync(application, cancellationToken))
			{
				// If the consent is external (e.g when authorizations are granted by a sysadmin),
				// immediately return an error if no authorization can be found in the database.
				case OpenIddictConstants.ConsentTypes.External when authorizations.Count is 0:
					return new ForbidResult(
						OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
						properties: new AuthenticationProperties(new Dictionary<string, string?>
						{
							[Properties.Error] =
								OpenIddictConstants.Errors.ConsentRequired,
							[Properties.ErrorDescription] =
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
					identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user!))
						.SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user!))
						.SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user!))
						.SetClaim(OpenIddictConstants.Claims.PreferredUsername,
							await userManager.GetUserNameAsync(user!))
						.SetClaims(OpenIddictConstants.Claims.Role, [.. await userManager.GetRolesAsync(user!)]);

					// Note: in this sample, the granted scopes match the requested scope
					// but you may want to allow the user to uncheck specific scopes.
					// For that, simply restrict the list of scopes before calling SetScopes.
					identity.SetScopes(request.GetScopes());
					identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes(), cancellationToken)
						.ToListAsync(cancellationToken));

					// Automatically create a permanent authorization to avoid requiring explicit consent
					// for future authorization or token requests containing the same scopes.
					var authorization = authorizations.LastOrDefault();
					authorization ??= await authorizationManager.CreateAsync(
						identity: identity,
						subject: await userManager.GetUserIdAsync(user!),
						client: (await applicationManager.GetIdAsync(application, cancellationToken))!,
						type: OpenIddictConstants.AuthorizationTypes.Permanent,
						scopes: identity.GetScopes(), cancellationToken: cancellationToken);

					identity.SetAuthorizationId(
						await authorizationManager.GetIdAsync(authorization, cancellationToken));
					identity.SetDestinations(destinationsProvider.GetDestinations);

					return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
						new ClaimsPrincipal(identity));

				// At this point, no authorization was found in the database and an error must be returned
				// if the client application specified prompt=none in the authorization request.
				case OpenIddictConstants.ConsentTypes.Explicit
					when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
				case OpenIddictConstants.ConsentTypes.Systematic
					when request.HasPromptValue(OpenIddictConstants.PromptValues.None):
					return new ForbidResult(
						OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
						properties: new AuthenticationProperties(new Dictionary<string, string?>
						{
							[Properties.Error] =
								OpenIddictConstants.Errors.ConsentRequired,
							[Properties.ErrorDescription] =
								"Interactive user consent is required."
						}));
			}

			var redirectUri = httpContext.Request.PathBase
			                  + httpContext.Request.Path
			                  + httpContext.Request.QueryString;

			return new ChallengeResult(new AuthenticationProperties
			{
				RedirectUri = redirectUri,
				Parameters =
				{
					Path("consent"),
					QueryParams(new StringBuilder()
						.Append("app_name=")
						.Append(await applicationManager.GetDisplayNameAsync(application, cancellationToken))
						.Append('&')
						.Append("scope=")
						.Append(request.Scope)
						.ToString())
				}
			});
		}
	}
}