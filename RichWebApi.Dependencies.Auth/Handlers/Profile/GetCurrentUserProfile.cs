using AutoMapper;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Models;
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
			var user = await accessor.User;
			var result = mapper.Map<UserProfileDto>(user);
			return new OkObjectResult(result);
		}
	}
}