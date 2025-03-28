namespace RichWebApi.Models;

public sealed class AuthErrorResponseDto : IOutbound
{
	public required AuthErrorDto[] Errors { get; init; }
}