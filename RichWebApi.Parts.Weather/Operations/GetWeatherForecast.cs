using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using RichWebApi.Entities;
using RichWebApi.Extensions;
using RichWebApi.Mappers;
using RichWebApi.Models;
using RichWebApi.Persistence;

namespace RichWebApi.Operations;

public record GetWeatherForecast(DateTime Date) : IRequest<WeatherForecastDto>
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<GetWeatherForecast>
	{
		public Validator() => RuleFor(x => x.Date).NotEqual(default(DateTime));
	}

	[UsedImplicitly]
	internal class GetWeatherForecastHandler(IRichWebApiDatabase database, IMapper mapper)
		: IRequestHandler<GetWeatherForecast, WeatherForecastDto>
	{
		public Task<WeatherForecastDto> Handle(GetWeatherForecast request, CancellationToken cancellationToken)
		{
			var date = DateOnly.FromDateTime(request.Date);
			return database.ReadAsync((db, ct) => db.Context
				.Set<WeatherForecast>()
				.Where(x => x.Date == date)
				.Select(x => 1)
				.ProjectTo<WeatherForecastDto>(mapper)
				.FirstOrExceptionAsync(ct), cancellationToken);
		}
	}
}