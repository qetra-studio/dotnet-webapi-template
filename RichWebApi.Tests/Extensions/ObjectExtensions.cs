using Newtonsoft.Json;

namespace RichWebApi.Tests.Extensions;

public static class ObjectExtensions
{
	public static Task<T> WrapInTask<T>(this T result) => Task.FromResult(result);

	public static string ToJson(this object o, TestJsonSerializerSettings? settings = null) => JsonConvert.SerializeObject(o, settings);

	public static string ToJson(this object o, Formatting formatting)
		=> JsonConvert.SerializeObject(o, formatting);

	public static T? FromJson<T>(this string o, TestJsonSerializerSettings? settings = null)
		=> JsonConvert.DeserializeObject<T>(o, settings);

	public static T JsonCopy<T>(this T value, TestJsonSerializerSettings? settings = null) where T : class
		=> value.ToJson(settings).FromJson<T>(settings)!;
}