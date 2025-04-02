namespace RichWebApi.Mappers;

public static class QueryableExtensions
{
	public static IQueryable<TDestination> ProjectTo<TDestination>(this IQueryable source, IMapper mapper) where TDestination : class => mapper.Of(source.ElementType).Project<TDestination>(source);
}