using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Config;

public sealed class MfaConfig : IAppConfig
{
	public string Issuer { get; set; } = null!;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<MfaConfig>
	{
		public Validator()
		{
			RuleFor(x => x.Issuer).NotNull().NotEmpty();
		}
	}
}