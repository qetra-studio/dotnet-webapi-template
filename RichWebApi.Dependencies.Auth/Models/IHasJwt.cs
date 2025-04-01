namespace RichWebApi.Models;

public interface IHasJwt
{
	string AccessToken { get; }
	string TokenType { get; }
	int ExpiresIn { get; }
}