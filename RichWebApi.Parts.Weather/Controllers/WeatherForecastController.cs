using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RichWebApi.Models;
using RichWebApi.Operations;
using RichWebApi.Utilities.Paging;

namespace RichWebApi.Controllers;

[ApiController]
[Route("weatherForecasts")]
[Authorize(Policy = "weather")]
public class WeatherForecastController(IMediator mediator) : ControllerBase
{
	[HttpGet(Name = "GetWeatherForecasts")]
	public Task<PagedResult<WeatherForecastDto>> GetWeatherForecastsPage([FromQuery(Name = "page")] int page,
																		 [FromQuery(Name = "size")] int size,
																		 [FromQuery(Name = "fromDate")] DateTime? from,
																		 [FromQuery(Name = "toDate")] DateTime? to,
																		 CancellationToken ct)
		=> mediator.Send(new GetWeatherForecasts(page, size, from, to), ct);

	[HttpGet("{date:datetime}", Name = "GetWeatherForecast")]
	public Task<WeatherForecastDto> GetWeatherForecastByDate([FromRoute(Name = nameof(date))] DateTime date,
															 CancellationToken ct)
		=> mediator.Send(new GetWeatherForecast(date), ct);

	[HttpPatch(Name = "PatchWeatherForecast")]
	public Task PatchWeatherForecast([FromBody] WeatherForecastDto weatherForecast, CancellationToken ct)
		=> mediator.Send(new PatchWeatherForecast(weatherForecast), ct);
}