using AutoMapper;
using FluentValidation;
using JetBrains.Annotations;
using RichWebApi.Entities;
using RichWebApi.Mappers;

namespace RichWebApi.Models;

public class WeatherForecastDto : IDateAuditableDto, IHasMapping
{
	public DateOnly Date { get; set; }

	public int TemperatureC { get; set; }

	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

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

	public static void AddProfileMapping(Profile profile)
		=> profile.CreateMap<WeatherForecast, WeatherForecastDto>(MemberList.Destination)
			.ForMember(x => x.TemperatureF, x => x.Ignore())
			.ReverseMap()
			.IgnoreAuditableProperties();
}