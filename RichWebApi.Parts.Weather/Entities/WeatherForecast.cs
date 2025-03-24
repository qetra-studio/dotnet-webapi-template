using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RichWebApi.Entities.Configuration;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Entities;

[Table("WeatherForecasts", Schema = "weather")]
[Comment("Weather per day forecasts")]
public class WeatherForecast : IAuditableEntity, ISoftDeletableEntity
{
	public Guid WeatherForecastId { get; set; }

	public DateOnly Date { get; set; }

	[Range(-100, 100)] public int TemperatureC { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public DateTimeOffset? DeletedAt { get; set; }

	[MaxLength(500)] public string Summary { get; set; } = null!;

	public Guid? CreatedById { get; set; }

	public RichWebApiUser? CreatedBy { get; set; }

	public Guid? ModifiedById { get; set; }

	public RichWebApiUser? ModifiedBy { get; set; }

	public Guid? DeletedById { get; set; }

	public RichWebApiUser? DeletedBy { get; set; }

	public class Configurator : EntityConfiguration<WeatherForecast>
	{
		public override void Configure(EntityTypeBuilder<WeatherForecast> builder)
		{
			base.Configure(builder);
			builder.HasKey(x => x.WeatherForecastId);
			builder.Property(x => x.Date).HasColumnType("date");
			builder.HasIndex(x => new { x.Date, x.DeletedAt })
				.IsUnique()
				.HasFilter(null);
		}
	}

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<WeatherForecast>
	{
		public Validator()
		{
			RuleFor(x => x.TemperatureC).InclusiveBetween(-100, 100);
		}
	}
}