using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using RichWebApi.Entities.Identity;
using RichWebApi.Validation;

namespace RichWebApi.Handlers.OpenIddict;

public record ConnectUserInfo : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<ConnectUserInfo>
	{
		public Validator(IHttpContextAccessor accessor)
			=> RuleFor(x => x)
				.HasAuthenticatedUser(accessor);
	}

	[UsedImplicitly]
	internal class ConnectUserInfoHandler(UserManager<RichWebApiUser> userManager, IHttpContextAccessor accessor)
		: IRequestHandler<ConnectUserInfo, IActionResult>
	{
		public async Task<IActionResult> Handle(ConnectUserInfo request, CancellationToken cancellationToken)
		{
			var principal = accessor.HttpContext!.User;
			var subClaim = principal.GetClaim(OpenIddictConstants.Claims.Subject);
			if (string.IsNullOrEmpty(subClaim))
			{
				return new ChallengeResult(
					OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[OpenIddictServerAspNetCoreConstants.Properties.Error] =
							OpenIddictConstants.Errors.InvalidToken,
						[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
							"The specified access token is bound to an account that no longer exists."
					}));
			}

			var user = await userManager.FindByIdAsync(subClaim);
			if (user is null)
			{
				return new ChallengeResult(
					OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
					properties: new AuthenticationProperties(new Dictionary<string, string?>
					{
						[OpenIddictServerAspNetCoreConstants.Properties.Error] =
							OpenIddictConstants.Errors.InvalidToken,
						[OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
							"The specified access token is bound to an account that no longer exists."
					}));
			}

			var claims = new Dictionary<string, object>(StringComparer.Ordinal)
			{
				// Note: the "sub" claim is a mandatory claim and must be included in the JSON response.
				[OpenIddictConstants.Claims.Subject] = await userManager.GetUserIdAsync(user)
			};

			if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Email))
			{
				var email = await userManager.GetEmailAsync(user);
				if (!string.IsNullOrEmpty(email))
				{
					claims[OpenIddictConstants.Claims.Email] = email;
					claims[OpenIddictConstants.Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user);
				}
			}

			if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Phone))
			{
				var phoneNumber = await userManager.GetPhoneNumberAsync(user);
				if (!string.IsNullOrEmpty(phoneNumber))
				{
					claims[OpenIddictConstants.Claims.PhoneNumber] = phoneNumber;
					claims[OpenIddictConstants.Claims.PhoneNumberVerified] =
						await userManager.IsPhoneNumberConfirmedAsync(user);
				}
			}

			if (principal.HasScope(OpenIddictConstants.Permissions.Scopes.Roles))
			{
				claims[OpenIddictConstants.Claims.Role] = await userManager.GetRolesAsync(user);
			}

			// Note: the complete list of standard claims supported by the OpenID Connect specification
			// can be found here: http://openid.net/specs/openid-connect-core-1_0.html#StandardClaims

			return new OkObjectResult(claims);
		}
	}
}