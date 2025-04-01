namespace RichWebApi.Models;

public sealed class AuthActionRequiredDto : IOutbound, IHasJwt
{
	public required string ErrorCode { get; init; }

	public required string Message { get; init; }

	public required string AccessToken { get; init; }
	public required string TokenType { get; init; }
	public required int ExpiresIn { get; init; }
}