namespace RichWebApi.Models;

public sealed class AuthErrorDto : IOutbound
{
	public required string ErrorCode { get; init; }

	public required string Message { get; init; }
}