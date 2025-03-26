using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RichWebApi.Exceptions;
using RichWebApi.Extensions;

namespace RichWebApi.MediatR;

public sealed class ValidationBehavior<TRequest, TResponse>(
	IEnumerable<IValidator<TRequest>> validators,
	ILogger<ValidationBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IBaseRequest
{
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
										CancellationToken cancellationToken)
	{
		await logger.TimeAsync(async () =>
		{
			var validationResults = await Task.WhenAll(validators
				.Select(v => v.ValidateAsync(request, cancellationToken)));
			var failures = validationResults
				.Where(f => !f.IsValid)
				.ToList();

			if (failures.Any())
			{
				var errors = failures.SelectMany(x => x.Errors).ToArray();
				logger.LogWarning("Validation failed for {Request}, failures: {@Failures}", request,
					errors.Select(e => new { e.ErrorMessage, e.PropertyName, e.ErrorCode }));
				throw new RichWebApiValidationException(errors);
			}
		}, "MediatR request validation");

		return await next();
	}
}