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
using RichWebApi.Services.OpenIddict;
using RichWebApi.Validation;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace RichWebApi.Handlers.OpenIddict;

public record ConnectToken : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<ConnectToken>
	{
		public Validator(IHttpContextAccessor accessor)
		{
			RuleFor(x => x)
				.HasOpenIddictServerRequest(accessor,
					request => request.IsClientCredentialsGrantType()
					           || request.IsAuthorizationCodeGrantType()
					           || request.IsRefreshTokenGrantType());
		}
	}

	[UsedImplicitly]
	internal class ConnectTokenHandler(
		OpenIddictApplicationManager<RichWebApiOpenApplication> manager,
		OpenIddictScopeManager<RichWebApiOpenScope> scopeManager,
		UserManager<RichWebApiUser> userManager,
		SignInManager<RichWebApiUser> signInManager,
		IClaimDestinationsProvider destinationsProvider,
		IHttpContextAccessor accessor) : IRequestHandler<ConnectToken, IActionResult>
	{
		public Task<IActionResult> Handle(ConnectToken exchange, CancellationToken cancellationToken)
		{
			var request = accessor.HttpContext!.GetOpenIddictServerRequest()!;
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

		private async Task<IActionResult> HandleAuthorizationCodeAsync(OpenIddictRequest _,
		                                                               CancellationToken cancellationToken)
		{
			// Retrieve the claims principal stored in the authorization code/refresh token.
			var result =
				await accessor.HttpContext!.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
			cancellationToken.ThrowIfCancellationRequested();
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

			cancellationToken.ThrowIfCancellationRequested();

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

			cancellationToken.ThrowIfCancellationRequested();

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
				.SetClaims(OpenIddictConstants.Claims.Role, [.. await userManager.GetRolesAsync(user)]);

			identity.SetDestinations(destinationsProvider.GetDestinations);

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

			identity.SetScopes(request.GetScopes());
			identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes(), cancellationToken)
				.ToListAsync(cancellationToken));
			identity.SetDestinations(destinationsProvider.GetDestinations);

			return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
				new ClaimsPrincipal(identity));
		}
	}
}