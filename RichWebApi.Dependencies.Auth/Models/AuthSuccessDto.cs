namespace RichWebApi.Models;

public sealed class AuthSuccessDto : IOutbound
{
	public required string AccessToken { get; set; } = null!;
}