using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RichWebApi.Entities;
using RichWebApi.Extensions;
using RichWebApi.Mappers;
using RichWebApi.Models;
using RichWebApi.Persistence;
using RichWebApi.Utilities.Paging;

namespace RichWebApi.Operations;

public record GetWeatherForecasts(int Page, int Size, DateTime? From = null, DateTime? To = null) : IRequest<PagedResult<WeatherForecastDto>>, IPagedRequest
{
	[UsedImplicitly]
	public class Validator : AbstractValidator<GetWeatherForecasts>
	{
		public Validator(IValidator<IPagedRequest> validator)
		{
			Include(validator);
			RuleFor(x => x.From)
				.LessThan(x => x.To)
				.When(x => x.From is not null && x.To is not null);

			RuleFor(x => x.To)
				.GreaterThan(x => x.From)
				.When(x => x.From is not null && x.To is not null);
		}
	}

	[UsedImplicitly]
	internal class GetWeatherForecastsHandler(IRichWebApiDatabase database, IMapper mapper)
		: IRequestHandler<GetWeatherForecasts, PagedResult<WeatherForecastDto>>
	{
		public Task<PagedResult<WeatherForecastDto>> Handle(GetWeatherForecasts request,
															CancellationToken cancellationToken)
		{
			var from = request.From.HasValue ? DateOnly.FromDateTime(request.From.Value) : (DateOnly?)null;
			var to = request.To.HasValue ? DateOnly.FromDateTime(request.To.Value) : (DateOnly?)null;
			return database.ReadAsync((db, ct) => db.Context
				.Set<WeatherForecast>()
				.MaybeWhere(from is not null, x => x.Date >= from)
				.MaybeWhere(to is not null, x => x.Date <= to)
				.OrderBy(x => x.Date)
				.AsNoTracking()
				.ProjectTo<WeatherForecastDto>(mapper)
				.ToPagedResultAsync(request, ct), cancellationToken);
		}
	}
}