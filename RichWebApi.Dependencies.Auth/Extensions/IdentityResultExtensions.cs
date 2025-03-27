using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Models;

namespace RichWebApi.Extensions;

internal static class IdentityResultExtensions
{
	public static IActionResult ToUnauthorizedResult(this IdentityResult result) 
		=> new UnauthorizedObjectResult(FormatErrors(result.Errors));

	public static IActionResult ToBadRequestResult(this IdentityResult result) 
		=> new BadRequestObjectResult(FormatErrors(result.Errors));

	private static ICollection<AuthErrorDto> FormatErrors(IEnumerable<IdentityError> errors)
		=> errors.Select(x => new AuthErrorDto
		{
			ErrorCode = x.Code,
			Message = x.Description
		}).ToArray();
}