using FluentValidation;

namespace RichWebApi.Config;

public class BearerConfig : IAppConfig
{
	public string Issuer { get; set; } = null!;

	public string Audience { get; set; } = null!;
	
	public sealed class Validator : AbstractValidator<BearerConfig>
	{
		public Validator()
		{
			RuleFor(x => x.Issuer).NotNull().NotEmpty();
			RuleFor(x => x.Audience).NotNull().NotEmpty();
		}
	}
}