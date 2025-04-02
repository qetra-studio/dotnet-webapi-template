namespace RichWebApi.Mappers;

public interface IMapper
{
	ITypeMapper<TSource> Of<TSource>() where TSource : class;
	ITypeMapper Of(Type type);

	TDestination Map<TDestination>(object source) where TDestination : class;

	void Patch<TSource, TDestination>(TDestination destination, TSource value) where TSource : class
																			  where TDestination : class;
}