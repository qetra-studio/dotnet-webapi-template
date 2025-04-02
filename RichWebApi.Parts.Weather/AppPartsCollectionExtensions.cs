using RichWebApi.Parts;

namespace RichWebApi;

public static class AppPartsCollectionExtensions
{
	public static IAppPartsCollection AddWeather(this IAppPartsCollection parts)
	{
		parts.Add(new WeatherPart());
		return parts;
	}
}