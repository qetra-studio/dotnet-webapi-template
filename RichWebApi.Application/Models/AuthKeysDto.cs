namespace RichWebApi.Models;

public sealed class AuthKeysDto : IOutbound
{
	public required JwkDto[] Keys { get; init; } = [];
}