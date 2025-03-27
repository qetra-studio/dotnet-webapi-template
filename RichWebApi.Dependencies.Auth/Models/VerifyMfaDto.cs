using FluentValidation;

namespace RichWebApi.Models;

public sealed class VerifyMfaDto : IInbound
{
	public string Token { get; set; } = null!;

	public sealed class Validator : AbstractValidator<VerifyMfaDto>
	{
		public Validator()
		{
			RuleFor(x => x.Token).NotNull().NotEmpty().MaximumLength(6);
		}
	}
}