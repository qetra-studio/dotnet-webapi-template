using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RichWebApi.Exceptions;
using RichWebApi.Models;

namespace RichWebApi.Filters;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class ExceptionFilter : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionFilter>>();
		switch (context.Exception)
		{
			case RichWebApiValidationException ve:
				var errors = new Dictionary<string, List<object>>();
				foreach (var validationFailure in ve.Errors)
				{
					var error = new ErrorDto
					{
						ErrorCode = validationFailure.ErrorCode,
						Message = validationFailure.ErrorMessage,
						CustomState = validationFailure.CustomState,
						Severity = validationFailure.Severity,
					};

					if (errors.TryGetValue(validationFailure.PropertyName, out var value))
					{
						value.Add(error);
					}
					else
					{
						errors[validationFailure.PropertyName] =
						[
							error
						];
					}
				}

				context.Result = new ObjectResult(errors) { StatusCode = StatusCodes.Status400BadRequest };
				break;
			default:
				logger.LogError(context.Exception, "Unhandled exception during request execution");
				context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
				break;
		}

		context.ExceptionHandled = true;
	}
}