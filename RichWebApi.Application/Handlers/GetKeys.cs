using System.Security.Cryptography;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RichWebApi.Config;
using RichWebApi.Models;

namespace RichWebApi.Handlers;

public record GetKeys : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<GetKeys>;

	[UsedImplicitly]
	internal class GetKeysHandler(IOptionsMonitor<AuthConfig> config) : IRequestHandler<GetKeys, IActionResult>
	{
		public Task<IActionResult> Handle(GetKeys request, CancellationToken cancellationToken)
		{
			var keys = config.CurrentValue.RsaKeys;
			var jwks = new List<JwkDto>(keys.Count);

			foreach (var (kid, key) in keys)
			{
				using var rsa = RSA.Create();
				rsa.ImportRSAPublicKey(Convert.FromBase64String(key.Public), out _);
				var parameters = rsa.ExportParameters(false);
				jwks.Add(new JwkDto
				{
					Kid = kid,
					Kty = "RSA",
					Use = "sig",
					Alg = "RS256",
					E = Base64UrlEncode(parameters.Exponent),
					N = Base64UrlEncode(parameters.Modulus),
				});
			}

			IActionResult result = new ObjectResult(new AuthKeysDto
			{
				Keys = jwks.ToArray()
			});
			
			return Task.FromResult(result);

			static string Base64UrlEncode(byte[]? input) => Convert.ToBase64String(input ?? []).Split('=')[0]
				.Replace('+', '-').Replace('/', '_');
		}
	}
}