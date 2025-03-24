using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using RichWebApi.Config;
using RichWebApi.Entities;
using RichWebApi.Persistence;

namespace RichWebApi.Services;

[UsedImplicitly]
internal sealed class WeatherWeekFillerService(
	IServiceProvider serviceProvider,
	IOptionsMonitor<WeatherWeekFillerServiceConfig> optionsMonitor)
	: CronScheduleService(serviceProvider)
{
	public override async Task PerformServiceFunctionAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
	{
		var logger = serviceProvider.GetRequiredService<ILogger<WeatherWeekFillerService>>();
		var database = serviceProvider.GetRequiredService<IRichWebApiDatabase>();
		var clock = serviceProvider.GetRequiredService<ISystemClock>();
		var now = DateOnly.FromDateTime(clock.UtcNow.DateTime);
		var schedule = CrontabSchedule.Parse("0 0 * * *");
		var weekAfterNow = now.AddDays(7);
		var weatherWeek = await database.ReadAsync((db, ct) => db.Context
			.Set<WeatherForecast>()
			.Where(x => x.Date > now && x.Date <= weekAfterNow)
			.AsNoTracking()
			.Select(x => x.Date)
			.ToListAsync(ct), cancellationToken);

		var week = schedule.GetNextOccurrences(now.ToDateTime(new TimeOnly(0,0,0)), weekAfterNow.ToDateTime(new TimeOnly(0,0,0))).ToArray();
		foreach (var day in week)
		{
			var date = DateOnly.FromDateTime(day.Date);
			if (!weatherWeek.Contains(date))
			{
				logger.LogInformation("Add weather for date {Date}", date.ToString("d"));
				database.Context.Add(new WeatherForecast
				{
					WeatherForecastId = Guid.NewGuid(),
					Date = date,
					Summary = "Cold",
					TemperatureC = 0
				});
			}
		}

		await database.PersistAsync(cancellationToken);
	}

	protected override CrontabSchedule GetSchedule()
		=> CrontabSchedule.Parse(optionsMonitor.CurrentValue.Schedule);
}