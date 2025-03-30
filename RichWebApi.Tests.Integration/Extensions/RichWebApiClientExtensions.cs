using System.Net.Http.Headers;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Extensions;

public static class RichWebApiClientExtensions
{
	public static void SetAccessToken<T>(this T client, AuthSuccessDto auth) where T : IRichWebApiClient
		=> client.AuthorizationHeader = new AuthenticationHeaderValue(auth.TokenType, auth.AccessToken);

	public static void SetRefreshToken<T>(this IRichWebApiClient client, AuthSuccessDto auth) where T : IRichWebApiClient
		=> throw new NotImplementedException();
}