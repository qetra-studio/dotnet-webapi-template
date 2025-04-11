using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace RichWebApi.Services.OpenIddict;

internal sealed class ClaimDestinationsProvider(IOptionsMonitor<IdentityOptions> optionsMonitor) : IClaimDestinationsProvider
{
	public IEnumerable<string> GetDestinations(Claim claim)
	{
		var claimsIdentityOptions = optionsMonitor.CurrentValue.ClaimsIdentity;
		// Never include the security stamp in the access and identity tokens, as it's a secret value.
		if (claim.Type == claimsIdentityOptions.SecurityStampClaimType)
		{
			yield break;
		}
		
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

			default:
				yield return OpenIddictConstants.Destinations.AccessToken;
				yield break;
		}

		bool HasScope(string role) => claim.Subject is not null && claim.Subject.HasScope(role);
	}
}