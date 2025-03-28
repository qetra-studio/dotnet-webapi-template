using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Services;

internal sealed class JwtVerifier(IWellKnownClient client, ILogger<JwtVerifier> logger)
{
	public Task VerifyTokenAsync(string token, Func<JwtSecurityToken, CancellationToken, Task> verify, CancellationToken cancellationToken = default)
	{
		var handler = new JwtSecurityTokenHandler();
		var jwt = handler.ReadJwtToken(token);
		return verify(jwt, cancellationToken);
	}

	public async Task<bool> VerifySignatureAsync(string token, Action<TokenValidationParameters>? configure = null,
												 CancellationToken cancellationToken = default)
	{
		var handler = new JwtSecurityTokenHandler();
		var jwt = handler.ReadJwtToken(token);

		if (jwt is null)
		{
			throw new InvalidOperationException("JWT could not be parsed.");
		}

		var kid = jwt.Header.Kid;

		var response = await client.GetJwksAsync(cancellationToken);
		var jwks = response.Result;
		var jwkSet = new JsonWebKeySet(JsonConvert.SerializeObject(jwks));
		var key = jwkSet.Keys.FirstOrDefault(x => x.Kid == kid);
		if (key is null)
		{
			throw new InvalidOperationException("JWK could not be found.");
		}

		var rsa = JwkToRsa(key);

		var validationParameters = new TokenValidationParameters
		{
			IssuerSigningKey = new RsaSecurityKey(rsa),
			ValidateIssuer = false,
			ValidateAudience = false,
		};

		configure?.Invoke(validationParameters);

		try
		{
			handler.ValidateToken(token, validationParameters, out _);
			return true;
		}
		catch (SecurityTokenException ex)
		{
			logger.LogError(ex, "Error during JWT validation");
			return false;
		}

		static RSA JwkToRsa(JsonWebKey key)
		{
			var modulus = Base64UrlDecode(key.N);
			var exponent = Base64UrlDecode(key.E);

			var rsa = RSA.Create();
			rsa.ImportParameters(new RSAParameters
			{
				Modulus = modulus,
				Exponent = exponent
			});

			return rsa;

			static byte[] Base64UrlDecode(string input)
			{
				var base64 = input
					.Replace('-', '+')
					.Replace('_', '/');

				base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

				return Convert.FromBase64String(base64);
			}
		}
	}
}