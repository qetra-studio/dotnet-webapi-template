using System.Security.Claims;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace RichWebApi.Handlers.OpenIddict;

public sealed record AuthorizeAcceptConsent : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<AuthorizeAcceptConsent>
	{
		public Validator(IHttpContextAccessor accessor)
			=> RuleFor(x => x)
				.HasOpenIddictServerRequest(accessor);
	}

	[UsedImplicitly]
	internal class AuthorizeAcceptConsentHandler(
		IHttpContextAccessor accessor,
		IRichWebApiUserContextAccessor userContextAccessor,
		OpenIddictApplicationManager<RichWebApiOpenApplication> applicationManager,
		OpenIddictAuthorizationManager<RichWebApiOpenAuthorization> authorizationManager,
		OpenIddictScopeManager<RichWebApiOpenScope> scopeManager,
		IClaimDestinationsProvider destinationsProvider,
		UserManager<RichWebApiUser> userManager)
		: IRequestHandler<AuthorizeAcceptConsent, IActionResult>
	{
		public async Task<IActionResult> Handle(AuthorizeAcceptConsent _, CancellationToken cancellationToken)
		{
			var request = accessor.HttpContext!.GetOpenIddictServerRequest()!;

			var user = (await userContextAccessor.User)!;

			// Retrieve the application details from the database.
			var application =
				await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken: cancellationToken)
				?? throw new InvalidOperationException(
					"Details concerning the calling client application cannot be found.");

			// Retrieve the permanent authorizations associated with the user and the calling client application.
			var authorizations = await authorizationManager.FindAsync(
				subject: await userManager.GetUserIdAsync(user),
				client: await applicationManager.GetIdAsync(application, cancellationToken),
				status: OpenIddictConstants.Statuses.Valid,
				type: OpenIddictConstants.AuthorizationTypes.Permanent,
				scopes: request.GetScopes(), cancellationToken).ToListAsync(cancellationToken);

			// Note: the same check is already made in the other action but is repeated
			// here to ensure a malicious user can't abuse this GET-only endpoint and
			// force it to return a valid response without the external authorization.
			if (authorizations.Count is 0
			    && await applicationManager.HasConsentTypeAsync(application, OpenIddictConstants.ConsentTypes.External,
				    cancellationToken))
			{
				return new ForbidResult(
					OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[Properties.Error] = OpenIddictConstants.Errors.ConsentRequired,
						[Properties.ErrorDescription] =
							"The logged in user is not allowed to access this client application."
					}));
			}

			// Create the claims-based identity that will be used by OpenIddict to generate tokens.
			var identity = new ClaimsIdentity(
				authenticationType: TokenValidationParameters.DefaultAuthenticationType,
				nameType: OpenIddictConstants.Claims.Name,
				roleType: OpenIddictConstants.Claims.Role);

			// Add the claims that will be persisted in the tokens.
			identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user))
				.SetClaim(OpenIddictConstants.Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
				.SetClaims(OpenIddictConstants.Claims.Role, [.. await userManager.GetRolesAsync(user)]);

			// Note: in this sample, the granted scopes match the requested scope
			// but you may want to allow the user to uncheck specific scopes.
			// For that, simply restrict the list of scopes before calling SetScopes.
			identity.SetScopes(request.GetScopes());
			var resources = await scopeManager.ListResourcesAsync(identity.GetScopes(), cancellationToken)
				.ToListAsync(cancellationToken); 
			identity.SetResources(resources);

			// Automatically create a permanent authorization to avoid requiring explicit consent
			// for future authorization or token requests containing the same scopes.
			var authorization = authorizations.LastOrDefault();
			authorization ??= await authorizationManager.CreateAsync(
				identity: identity,
				subject: user.Id.ToString(),
				client: application.Id.ToString(),
				type: OpenIddictConstants.AuthorizationTypes.Permanent,
				scopes: identity.GetScopes(), cancellationToken);

			var authorizationId = await authorizationManager.GetIdAsync(authorization, cancellationToken);
			identity.SetAuthorizationId(authorizationId);
			identity.SetDestinations(destinationsProvider.GetDestinations);

			// Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
			return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
		}
	}
}