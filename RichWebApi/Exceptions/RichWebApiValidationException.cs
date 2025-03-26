using System.Text;
using FluentValidation.Results;

namespace RichWebApi.Exceptions;

public class RichWebApiValidationException : RichWebApiException
{
	public IEnumerable<ValidationFailure> Errors { get; }
	private const string PrimaryMessage = "The server couldn't make sense of your request";

	public RichWebApiValidationException(ICollection<ValidationFailure> errors)
		: base($"{PrimaryMessage}: {errors.Aggregate(new StringBuilder(), (prev, next) => prev.AppendLine(next.ErrorMessage).Append(','))}")
	{
		Errors = errors;
	}

	public override int StatusCode { get; } = 400;
}