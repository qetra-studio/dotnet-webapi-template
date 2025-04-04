using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Config;

public class AuthConfig : IAppConfig
{
	public IReadOnlyDictionary<string, RsaKeyPair> RsaKeys { get; set; } = new Dictionary<string, RsaKeyPair>();

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<AuthConfig>
	{
		public Validator()
		{
			RuleFor(x => x.RsaKeys).NotEmpty();
			RuleForEach(x => x.RsaKeys.Values).SetValidator(new RsaKeyPair.Validator());
		}
	}
}