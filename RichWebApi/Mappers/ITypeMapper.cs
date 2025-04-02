namespace RichWebApi.Mappers;

public interface ITypeMapper
{
	TDestination Map<TDestination>(object source) where TDestination : class;
	void Patch<TDestination>(object source, TDestination destination) where TDestination : class;

	IQueryable<TDestination> Project<TDestination>(IQueryable source) where TDestination : class;
}

public interface ITypeMapper<in TSource> where TSource : class
{
	TDestination Map<TDestination>(TSource source) where TDestination : class;
	void Patch<TDestination>(TSource source, TDestination destination) where TDestination : class;

	IQueryable<TDestination> Project<TDestination>(IQueryable<TSource> source) where TDestination : class;
}