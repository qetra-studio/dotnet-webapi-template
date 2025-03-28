using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Models;

namespace RichWebApi.Extensions;

internal static class IdentityResultExtensions
{
	public static IActionResult ToUnauthorizedResult(this IdentityResult result)
		=> new UnauthorizedObjectResult(new AuthErrorResponseDto
		{
			Errors = result.Errors.Select(x => new AuthErrorDto
			{
				ErrorCode = x.Code,
				Message = x.Description
			}).ToArray()
		});

	public static IActionResult ToBadRequestResult(this IdentityResult result)
		=> new BadRequestObjectResult(new ValidationResponseDto
		{
			Errors = new Dictionary<string, ValidationErrorDto[]>
			{
				{
					"auth", result.Errors.Select(x => new ValidationErrorDto
					{
						Message = x.Description,
						ErrorCode = x.Code,
						Severity = Severity.Error
					}).ToArray()
				}
			}
		});
}