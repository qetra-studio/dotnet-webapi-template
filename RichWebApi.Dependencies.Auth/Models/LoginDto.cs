using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Validation;

namespace RichWebApi.Models;

public sealed class LoginDto : IInbound
{
	public string Login { get; set; } = null!;

	public string Password { get; set; } = null!;

	public sealed class Validator : AbstractValidator<LoginDto>
	{
		public Validator(UserManager<RichWebApiUser> manager)
		{
			RuleFor(x => x.Login).NotNull().NotEmpty();
			RuleFor(x => x.Login)
				.RichWebApiUsername()
				.When(x => x.DefineLoginKind() == LoginValueKind.UserName);

			RuleFor(x => x.Login)
				.EmailAddress()
				.When(x => x.DefineLoginKind() == LoginValueKind.Email);

			RuleFor(x => x).Custom((x, ctx) =>
			{
				switch (x.DefineLoginKind())
				{
					case LoginValueKind.UserName:
					case LoginValueKind.Email:
						break;
					case LoginValueKind.PhoneNumber:
					default:
						{
							ctx.AddFailure(new ValidationFailure
							{
								Severity = Severity.Error,
								AttemptedValue = x,
								ErrorCode = "invalidLogin",
								PropertyName = $"{ctx.PropertyPath}.{nameof(Login)}",
								ErrorMessage = "Login contains invalid characters."
							});
							break;
						}
				}
			});
			RuleFor(x => x.Password).NotNull().NotEmpty().RichWebApiPassword(manager);
		}
	}
}