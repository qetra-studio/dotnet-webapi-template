using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Config;
using RichWebApi.Extensions;
using RichWebApi.Services;
using RichWebApi.Startup;
using RichWebApi.Validation;

[assembly: InternalsVisibleTo("RichWebApi.Parts.Weather.Tests.Unit")]
namespace RichWebApi;

internal class WeatherPart : IAppPart
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddOptionsWithValidator<WeatherWeekFillerServiceConfig, WeatherWeekFillerServiceConfig.Validator>(
			"Services:WeatherWeekFiller");
		services.AddCronService<WeatherWeekFillerService>();
		services.AddStartupAction<FillWeatherWeekAction>();
		services.AddAuthorizationBuilder()
			.AddAccessPolicy("weather", x => x.RequireClaim("scope", "weather"));
	}
}