using FluentValidation;
using JetBrains.Annotations;
using RichWebApi.Entities;
using RichWebApi.Mappers;
using Riok.Mapperly.Abstractions;

namespace RichWebApi.Models;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class WeatherForecastDto
	: IDateAuditableDto,
	  IHasAdapter<WeatherForecast, WeatherForecastDto>,
	  IHasAdapter<WeatherForecastDto, WeatherForecast>
{
	public DateOnly Date { get; set; }

	public int TemperatureC { get; set; }

	[MapperIgnore] public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

	public string Summary { get; set; } = null!;

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	[UsedImplicitly]
	public class Validator : AbstractValidator<WeatherForecastDto>
	{
		public Validator()
		{
			var date = new DateOnly(2023, 1, 1);
			RuleFor(x => x.Date).GreaterThanOrEqualTo(date);
			RuleFor(x => x.TemperatureC).InclusiveBetween(-100, 100);
			RuleFor(x => x.Summary).NotNull().NotEmpty().MaximumLength(500);
		}
	}

	public static partial WeatherForecastDto Map(WeatherForecast source);
	public static partial void Patch(WeatherForecast update, WeatherForecastDto destination);
	public static partial IQueryable<WeatherForecastDto> Project(IQueryable<WeatherForecast> source);

	[MapperRequiredMapping(RequiredMappingStrategy.Source)]
	public static partial WeatherForecast Map(WeatherForecastDto source);

	[MapperRequiredMapping(RequiredMappingStrategy.Source)]
	public static partial void Patch(WeatherForecastDto update, WeatherForecast destination);

	public static partial IQueryable<WeatherForecast> Project(IQueryable<WeatherForecastDto> source);
}