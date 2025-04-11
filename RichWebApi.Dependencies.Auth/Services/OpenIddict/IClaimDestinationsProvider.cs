using System.Security.Claims;

namespace RichWebApi.Services.OpenIddict;

public interface IClaimDestinationsProvider
{
	IEnumerable<string> GetDestinations(Claim claim);
}