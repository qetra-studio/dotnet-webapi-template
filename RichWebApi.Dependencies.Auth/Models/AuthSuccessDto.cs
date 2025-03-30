namespace RichWebApi.Models;

public sealed class AuthSuccessDto : IOutbound
{
	public required string AccessToken { get; set; } = null!;

	public required string TokenType { get; set; } = null!;
}