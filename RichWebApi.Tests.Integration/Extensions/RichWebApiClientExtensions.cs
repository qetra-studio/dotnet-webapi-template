using System.Net.Http.Headers;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Extensions;

public static class RichWebApiClientExtensions
{
	public static void SetAccessToken<T>(this T client, IHasJwt jwt) where T : IRichWebApiClient
		=> client.AuthorizationHeader = new AuthenticationHeaderValue(jwt.TokenType, jwt.AccessToken);

	public static void SetRefreshToken<T>(this T client, AuthSuccessDto auth) where T : IRichWebApiClient
		=> throw new NotImplementedException();
}