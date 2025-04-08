using Destructurama.Attributed;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Identity;
using RichWebApi.Validation;

namespace RichWebApi.Models;

public sealed class RegisterDto : IInbound
{
	public string UserName { get; set; } = null!;
	[NotLogged] public string Password { get; set; } = null!;
	public string Email { get; set; } = null!;
	[NotLogged] public string? PhoneNumber { get; set; }

	public sealed class Validator : AbstractValidator<RegisterDto>
	{
		public Validator(UserManager<RichWebApiUser> manager)
		{
			RuleFor(x => x.UserName)
				.NotNull()
				.NotEmpty()
				.RichWebApiUniqueUsername(manager);

			RuleFor(x => x.Email)
				.NotNull()
				.NotEmpty()
				.RichWebApiEmail(manager);

			RuleFor(x => x.Password)
				.NotNull()
				.NotEmpty()
				.RichWebApiPassword(manager);
		}
	}
}