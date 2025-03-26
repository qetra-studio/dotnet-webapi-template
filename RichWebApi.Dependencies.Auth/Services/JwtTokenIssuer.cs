using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RichWebApi.Config;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Services;

internal sealed class JwtTokenIssuer(IOptionsMonitor<AuthConfig> config,
                                     IOptionsMonitor<BearerConfig> bearerConfig,
                                     ISystemClock clock) : IJwtTokenIssuer
{
	private readonly Random _random = new();

	public Task<string> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken)
	{
		var keys = config.CurrentValue.RsaKeys;
		var k = keys.ElementAt(_random.Next(0, keys.Count));
		var rsa = RSA.Create();
		rsa.ImportRSAPrivateKey(Convert.FromBase64String(k.Value.Private), out _);
		var rsaKey = new RsaSecurityKey(rsa)
		{
			KeyId = k.Key
		};
		var credentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
		var tokenHandler = new JwtSecurityTokenHandler();
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(GetClaims(user)),
			Expires = clock.UtcNow.UtcDateTime.AddHours(1),
			SigningCredentials = credentials,
			Audience = bearerConfig.CurrentValue.Audience,
			Issuer	= bearerConfig.CurrentValue.Issuer
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return Task.FromResult(tokenHandler.WriteToken(token));

		IEnumerable<Claim> GetClaims(RichWebApiUser u)
		{
			yield return new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString("N"));
			yield return new Claim("scope", "weather");
		}
	}
}