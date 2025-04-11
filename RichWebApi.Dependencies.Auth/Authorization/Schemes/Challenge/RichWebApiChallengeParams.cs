namespace RichWebApi.Authorization.Schemes.Challenge;

internal static class RichWebApiChallengeParams
{
	public static KeyValuePair<string, object?> Path(string path) => new("path", path);
	public static KeyValuePair<string, object?> QueryParams(string query) => new("query", query);
}