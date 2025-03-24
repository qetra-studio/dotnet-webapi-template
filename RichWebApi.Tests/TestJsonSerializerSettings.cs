using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RichWebApi.Tests;

public class TestJsonSerializerSettings : JsonSerializerSettings
{
	public TestJsonSerializerSettings()
	{
		TypeNameHandling = TypeNameHandling.Objects;
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
		NullValueHandling = NullValueHandling.Include;
		DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
		Converters =
		[
			new DateOnlyConverter()
		];
	}
	
	public class DateOnlyConverter : JsonConverter<DateOnly>
	{
		private const string DateFormat = "yyyy-MM-dd";

		public override DateOnly ReadJson(JsonReader reader, Type objectType, DateOnly existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader is { TokenType: JsonToken.Date, Value: DateTime dateTime })
			{
				return DateOnly.FromDateTime(dateTime);
			}
			throw new JsonSerializationException("Invalid DateOnly format");
		}

		public override void WriteJson(JsonWriter writer, DateOnly value, JsonSerializer serializer)
		{
			writer.WriteValue(value.ToDateTime(new TimeOnly(0, 0, 0)));
		}
	}
}