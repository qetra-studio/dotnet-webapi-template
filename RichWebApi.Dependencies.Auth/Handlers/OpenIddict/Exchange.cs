using System.Security.Claims;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using RichWebApi.Entities.Identity;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace RichWebApi.Handlers.OpenIddict;

public record Exchange(OpenIddictRequest Request) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<Exchange>;

	[UsedImplicitly]
	internal class ExchangeHandler(
		IOpenIddictApplicationManager manager,
		UserManager<RichWebApiUser> userManager,
		SignInManager<RichWebApiUser> signInManager,
		IHttpContextAccessor accessor) : IRequestHandler<Exchange, IActionResult>
	{
		public Task<IActionResult> Handle(Exchange exchange, CancellationToken cancellationToken)
		{
			var request = exchange.Request;
			return request switch
			{
				_ when request.IsClientCredentialsGrantType() => HandleClientCredentialsAsync(request,
					cancellationToken),
				_ when request.IsAuthorizationCodeGrantType() => HandleAuthorizationCodeAsync(request,
					cancellationToken),
				_ when request.IsRefreshTokenGrantType() => HandleAuthorizationCodeAsync(request,
					cancellationToken),
				_ => Task.FromResult<IActionResult>(new ForbidResult())
			};
		}

		private async Task<IActionResult> HandleAuthorizationCodeAsync(OpenIddictRequest request,
																	   CancellationToken cancellationToken)
		{
			// Retrieve the claims principal stored in the authorization code/refresh token.
			var result =
				await accessor.HttpContext!.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

			// Retrieve the user profile corresponding to the authorization code/refresh token.
			var claim = result.Principal?.GetClaim(OpenIddictConstants.Claims.Subject);
			if (string.IsNullOrEmpty(claim))
			{
				return new ForbidResult();
			}

			var user = await userManager.FindByIdAsync(claim);
			if (user is null)
			{
				return new ForbidResult(
					OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					new AuthenticationProperties(new Dictionary<string, string?>
					{
						[Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
						[Properties.ErrorDescription] = "The token is no longer valid."
					}));
			}

			// Ensure the user is still allowed to sign in.
			if (!await signInManager.CanSignInAsync(user))
			{
				return new ForbidResult(
					OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
						[Properties.ErrorDescription] = "The user is no longer allowed to sign in."
					}));
			}

			var identity = new ClaimsIdentity(result.Principal?.Claims,
				authenticationType: TokenValidationParameters.DefaultAuthenticationType,
				nameType: OpenIddictConstants.Claims.Name,
				roleType: OpenIddictConstants.Claims.Role);

			// Override the user claims present in the principal in case they
			// changed since the authorization code/refresh token was issued.
			identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Email, await userManager.GetEmailAsync(user))
				.SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user))
				.SetClaim(OpenIddictConstants.Claims.PreferredUsername, await userManager.GetUserNameAsync(user))
				.SetClaims(OpenIddictConstants.Claims.Role, [.. (await userManager.GetRolesAsync(user))]);

			identity.SetDestinations(GetDestinations);

			// Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
			return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
				new ClaimsPrincipal(identity));
		}

		private async Task<IActionResult> HandleClientCredentialsAsync(OpenIddictRequest request,
																	   CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(request.ClientId))
			{
				return new ForbidResult();
			}

			var application = await manager.FindByClientIdAsync(request.ClientId, cancellationToken)
							  ?? throw new InvalidOperationException("The application cannot be found.");

			var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType,
				OpenIddictConstants.Claims.Name, OpenIddictConstants.Claims.Role);

			identity.SetClaim(OpenIddictConstants.Claims.Subject,
				await manager.GetClientIdAsync(application, cancellationToken));
			identity.SetClaim(OpenIddictConstants.Claims.Name,
				await manager.GetDisplayNameAsync(application, cancellationToken));

			identity.SetDestinations(GetDestinations);

			return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
				new ClaimsPrincipal(identity));
		}
	}

	private static IEnumerable<string> GetDestinations(Claim claim)
	{
		// Note: by default, claims are NOT automatically included in the access and identity tokens.
		// To allow OpenIddict to serialize them, you must attach them a destination, that specifies
		// whether they should be included in access tokens, in identity tokens or in both.

		switch (claim.Type)
		{
			case OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.PreferredUsername:
				yield return OpenIddictConstants.Destinations.AccessToken;

				if (HasScope(OpenIddictConstants.Scopes.Profile))
					yield return OpenIddictConstants.Destinations.IdentityToken;

				yield break;

			case OpenIddictConstants.Claims.Email:
				yield return OpenIddictConstants.Destinations.AccessToken;

				if (HasScope(OpenIddictConstants.Scopes.Email))
					yield return OpenIddictConstants.Destinations.IdentityToken;

				yield break;

			case OpenIddictConstants.Claims.Role:
				yield return OpenIddictConstants.Destinations.AccessToken;

				if (HasScope(OpenIddictConstants.Scopes.Roles))
					yield return OpenIddictConstants.Destinations.IdentityToken;

				yield break;

			// Never include the security stamp in the access and identity tokens, as it's a secret value.
			case "AspNet.Identity.SecurityStamp": yield break;

			default:
				yield return OpenIddictConstants.Destinations.AccessToken;
				yield break;
		}

		bool HasScope(string role) => claim.Subject is not null && claim.Subject.HasScope(role);
	}
}