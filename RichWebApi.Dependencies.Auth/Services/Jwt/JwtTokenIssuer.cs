using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RichWebApi.Config;
using RichWebApi.Constants;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;

namespace RichWebApi.Services.Jwt;

internal sealed class JwtTokenIssuer(IOptionsMonitor<AuthConfig> config,
									 IOptionsMonitor<BearerConfig> bearerConfig,
									 ISystemClock clock) : IJwtTokenIssuer
{
	private readonly Random _random = new();

	public async Task<IssuedJWT> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken)
	{
		var expiresIn = TimeSpan.FromHours(1);
		var token = await IssueTokenAsync(new ClaimsIdentity(GetClaims(user)), clock.UtcNow.Add(expiresIn));

		return new IssuedJWT(JwtBearerDefaults.AuthenticationScheme, token, expiresIn);
		IEnumerable<Claim> GetClaims(RichWebApiUser u)
		{
			yield return UserIdClaim(u);
			yield return PurposeClaim(RichWebApiJwtPurpose.Access);
			yield return StampClaim(u);
		}
	}

	public async Task<IssuedJWT> IssueTwoFactorTokenAsync(RichWebApiUser user, CancellationToken cancellationToken)
	{
		var expiresIn = TimeSpan.FromMinutes(5);
		var token = await IssueTokenAsync(new ClaimsIdentity(GetClaims(user)), clock.UtcNow.Add(expiresIn));

		return new IssuedJWT(JwtBearerDefaults.AuthenticationScheme, token, expiresIn);
		IEnumerable<Claim> GetClaims(RichWebApiUser u)
		{
			yield return UserIdClaim(u);
			yield return PurposeClaim(RichWebApiJwtPurpose.Mfa);
			yield return StampClaim(u);
		}
	}

	private Task<string> IssueTokenAsync(ClaimsIdentity identity, DateTimeOffset expires)
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
			Expires = expires.UtcDateTime,
			SigningCredentials = credentials,
			Audience = bearerConfig.CurrentValue.Audience,
			Issuer = bearerConfig.CurrentValue.Issuer
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return Task.FromResult(tokenHandler.WriteToken(token));
	}

	private static Claim PurposeClaim(RichWebApiJwtPurpose purpose)
		=> new(RichWebApiJwtClaimTypes.Purpose, purpose.ToString("G").ToLower());
	
	private static Claim UserIdClaim(RichWebApiUser user)
		=> new(JwtRegisteredClaimNames.Sub, user.Id.ToString("N"));
	
	private static Claim StampClaim(RichWebApiUser user)
		=> new(RichWebApiJwtClaimTypes.Stamp, user.SecurityStamp!);
}