using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Identity;
using static RichWebApi.Utilities.AuthRegex;

namespace RichWebApi.Models;

public partial class RegisterDto : IInbound
{
	public string UserName { get; set; }
	public string Password { get; set; }
	public string Email { get; set; }
	public string? PhoneNumber { get; set; }

	public sealed class Validator : AbstractValidator<RegisterDto>
	{
		public Validator(UserManager<RichWebApiUser> manager)
		{
			RuleFor(x => x.UserName)
				.NotNull()
				.NotEmpty()
				.MaximumLength(256)
				.CustomAsync(async (v, ctx, _) =>
				{
					if (!UserNameRegex.IsMatch(v))
					{
						ctx.AddFailure(new ValidationFailure
						{
							Severity = Severity.Error,
							AttemptedValue = v,
							ErrorCode = "invalidUserName",
							PropertyName = nameof(UserName),
							ErrorMessage = "Username contains invalid characters."
						});
						return;
					}

					if (await manager.FindByNameAsync(v) is not null)
					{
						ctx.AddFailure(new ValidationFailure
						{
							Severity = Severity.Error,
							AttemptedValue = v,
							ErrorCode = "userNameTaken",
							PropertyName = nameof(UserName),
							ErrorMessage = "Username is taken."
						});
					}
				});

			RuleFor(x => x.Email)
				.NotNull()
				.NotEmpty()
				.MaximumLength(256)
				.CustomAsync(async (v, ctx, _) =>
				{
					if (await manager.FindByEmailAsync(v) is not null)
					{
						ctx.AddFailure(new ValidationFailure
						{
							Severity = Severity.Error,
							AttemptedValue = v,
							ErrorCode = "emailTaken",
							PropertyName = nameof(Email),
							ErrorMessage = "Email is taken."
						});
					}
				});

			RuleFor(x => x.Password)
				.NotNull()
				.NotEmpty()
				.MaximumLength(100);
		}
	}
}