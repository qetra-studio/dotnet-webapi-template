namespace RichWebApi.Models;

public sealed class MfaSetupDto : IOutbound, IHasJwt
{
	public required string Key { get; init; }

	public required string Uri { get; init; }
	public required string AccessToken { get; init; }
	public required string TokenType { get; init; }
	public required int ExpiresIn { get; init; }
}