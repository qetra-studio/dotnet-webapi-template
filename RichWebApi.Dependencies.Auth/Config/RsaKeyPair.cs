using System.Security.Cryptography;
using System.Text;
using Destructurama.Attributed;
using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Config;

[UsedImplicitly]
public sealed class RsaKeyPair
{
	[NotLogged]
	public string Public { get; set; } = null!;

	[NotLogged]
	public string Private { get; set; } = null!;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RsaKeyPair>
	{
		public Validator()
		{
			RuleFor(x => x.Private).NotNull().NotEmpty().Custom((x, ctx) =>
			{
				try
				{
					using var rsa = RSA.Create();
					rsa.ImportFromPem(x);
				}
				catch (Exception)
				{
					ctx.AddFailure(nameof(Private), "Invalid private RSA key");
				}
			});

			RuleFor(x => x.Public).NotNull().NotEmpty().Custom((x, ctx) =>
			{
				try
				{
					using var rsa = RSA.Create();
					rsa.ImportFromPem(x);
				}
				catch (Exception)
				{
					ctx.AddFailure(nameof(Public), "Invalid public RSA key");
				}
			});
		}
	}
}