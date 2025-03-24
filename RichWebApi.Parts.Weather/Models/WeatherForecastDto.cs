using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Models;

public class WeatherForecastDto : IAuditableDto
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
}