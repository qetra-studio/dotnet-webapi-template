using FluentValidation;
using FluentValidation.Results;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using static RichWebApi.Utilities.AuthRegex;

namespace RichWebApi.Models;

public sealed class LoginDto : IInbound
{
	public string Login { get; set; } = null!;

	public string Password { get; set; } = null!;

	public sealed class Validator : AbstractValidator<LoginDto>
	{
		public Validator()
		{
			RuleFor(x => x.Login).NotNull().NotEmpty().MaximumLength(256);
			RuleFor(x => x).Custom((x, ctx) =>
			{
				var kind = x.DefineLoginKind();
				switch (kind)
				{
					case LoginValueKind.UserName:
						if (!UserNameRegex.IsMatch(x.Login))
						{
							ctx.AddFailure(new ValidationFailure
							{
								Severity = Severity.Error,
								AttemptedValue = x,
								ErrorCode = "invalidUserName",
								PropertyName = nameof(Login),
								ErrorMessage = "Username contains invalid characters."
							});
						}

						break;
					case LoginValueKind.Email:
					case LoginValueKind.PhoneNumber:
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(kind));
				}
			});
			RuleFor(x => x.Password).NotNull().NotEmpty().MaximumLength(100);
		}
	}
}