using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RichWebApi.Config;
using RichWebApi.Constants;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;

namespace RichWebApi.Services;

internal sealed class JwtTokenIssuer(IOptionsMonitor<AuthConfig> config,
									 IOptionsMonitor<BearerConfig> bearerConfig,
									 ISystemClock clock) : IJwtTokenIssuer
{
	private readonly Random _random = new();

	public Task<string> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken)
	{
		return IssueTokenAsync(new ClaimsIdentity(GetClaims(user)), clock.UtcNow.UtcDateTime.AddHours(1));

		IEnumerable<Claim> GetClaims(RichWebApiUser u)
		{
			yield return new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString("N"));
			yield return PurposeClaim(RichWebApiJwtPurpose.Access);
		}
	}

	public Task<string> IssueTwoFactorTokenAsync(RichWebApiUser user, CancellationToken cancellationToken)
	{
		return IssueTokenAsync(new ClaimsIdentity(GetClaims(user)), clock.UtcNow.UtcDateTime.AddMinutes(5));

		IEnumerable<Claim> GetClaims(RichWebApiUser u)
		{
			yield return new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString("N"));
			yield return PurposeClaim(RichWebApiJwtPurpose.Mfa);
		}
	}

	private Task<string> IssueTokenAsync(ClaimsIdentity identity, DateTime expires)
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
			Subject = identity,
			Expires = expires,
			SigningCredentials = credentials,
			Audience = bearerConfig.CurrentValue.Audience,
			Issuer = bearerConfig.CurrentValue.Issuer
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return Task.FromResult(tokenHandler.WriteToken(token));
	}

	private static Claim PurposeClaim(RichWebApiJwtPurpose purpose)
		=> new(RichWebApiJwtClaimTypes.Purpose, purpose.ToString("G").ToLower());
}