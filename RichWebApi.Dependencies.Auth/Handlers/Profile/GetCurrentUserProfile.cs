using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Mappers;
using RichWebApi.Models;
using RichWebApi.Persistence;
using RichWebApi.Services;

namespace RichWebApi.Handlers.Profile;

public sealed record GetCurrentUserProfile : IRequest<IActionResult>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<GetCurrentUserProfile>;

	[UsedImplicitly]
	internal class GetCurrentUserProfileHandler(IRichWebApiUserContextAccessor accessor, IMapper mapper) : IRequestHandler<GetCurrentUserProfile, IActionResult>
	{
		public async Task<IActionResult> Handle(GetCurrentUserProfile request, CancellationToken cancellationToken)
		{
			if (accessor.UserId is null)
			{
				return new UnauthorizedResult();
			}
			var user = await accessor.User;
			if (user is null)
			{
				return new UnauthorizedResult();
			}
			var result = mapper.Map<UserProfileDto>(user);
			return new OkObjectResult(result);
		}
	}
}