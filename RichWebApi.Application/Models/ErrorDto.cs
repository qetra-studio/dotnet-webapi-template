using FluentValidation;

namespace RichWebApi.Models;

public class ErrorDto
{
	public required string ErrorCode { get; init; }
	public required string Message { get; init; }
	public object? CustomState { get; init; }
	public required Severity Severity { get; init; }
}