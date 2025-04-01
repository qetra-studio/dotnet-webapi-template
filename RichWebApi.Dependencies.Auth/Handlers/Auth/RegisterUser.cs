using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Entities.Identity;
using RichWebApi.Extensions;
using RichWebApi.Models;

namespace RichWebApi.Handlers.Auth;

public record RegisterUser(RegisterDto Credentials) : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<RegisterUser>
	{
		public Validator(IValidator<RegisterDto> v) => RuleFor(x => x.Credentials).SetValidator(v);
	}

	[UsedImplicitly]
	internal class RegisterHandler(UserManager<RichWebApiUser> manager) : IRequestHandler<RegisterUser, IActionResult>
	{
		public async Task<IActionResult> Handle(RegisterUser request, CancellationToken cancellationToken)
		{
			var result = await manager.CreateAsync(new RichWebApiUser
			{
				Id = await FindNewIdAsync(),
				UserName = request.Credentials.UserName,
				Email = request.Credentials.Email,
				PhoneNumber = request.Credentials.PhoneNumber
			}, request.Credentials.Password);

			return result.Succeeded
				? new OkResult()
				: result.ToBadRequestResult();

			async Task<Guid> FindNewIdAsync()
			{
				var id = Guid.NewGuid();
				while (await manager.FindByIdAsync(id.ToString("D")) != null)
				{
					id = Guid.NewGuid();
				}

				return id;
			}
		}
	}
}