using System.Security.Cryptography;
using Destructurama.Attributed;
using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Config;

[UsedImplicitly]
public sealed class RsaKey
{
	[NotLogged]
	public string Public { get; set; } = null!;
	
	[NotLogged]
	public string Private { get; set; } = null!;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RsaKey>
	{
		public Validator()
		{
			RuleFor(x => x.Private).NotNull().NotEmpty().Custom((x, ctx) =>
			{
				try
				{
					using var rsa = RSA.Create();
					rsa.ImportRSAPrivateKey(Convert.FromBase64String(x), out _);
				}
				catch (Exception)
				{
					ctx.AddFailure(nameof(Private),"Invalid private RSA key");
				}
			});
			
			RuleFor(x => x.Public).NotNull().NotEmpty().Custom((x, ctx) =>
			{
				try
				{
					using var rsa = RSA.Create();
					rsa.ImportRSAPublicKey(Convert.FromBase64String(x), out _);
				}
				catch (Exception)
				{
					ctx.AddFailure(nameof(Public),"Invalid public RSA key");
				}
			});
		}
	}
}