namespace RichWebApi.Models;

public sealed class AuthSuccessDto : IOutbound, IHasJwt
{
	public required string AccessToken { get; init; } = null!;

	public required string TokenType { get; init; } = null!;
	public required int ExpiresIn { get; init; }
}