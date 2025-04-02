using System.Collections.Frozen;

namespace RichWebApi.Mappers;

internal sealed class TypeMapper<TSource>(FrozenDictionary<Type, TypeAdapter> adapters)
	: ITypeMapper<TSource>, ITypeMapper
	where TSource : class
{
	public TDestination Map<TDestination>(TSource source) where TDestination : class
	{
		var adapter = adapters.GetValueRefOrNullRef(typeof(TDestination));
		if (adapter == null!)
		{
			throw new InvalidOperationException(
				$"Adapter of {typeof(TSource)} to {typeof(TDestination)} has not been registered.");
		}

		return (TDestination)adapter.Map(source);
	}

	public void Patch<TDestination>(TSource source, TDestination destination) where TDestination : class
	{
		var adapter = adapters.GetValueRefOrNullRef(typeof(TDestination));
		if (adapter == null!)
		{
			throw new InvalidOperationException(
				$"Adapter of {typeof(TSource)} to {typeof(TDestination)} has not been registered.");
		}

		adapter.Update(source, destination);
	}

	public IQueryable<TDestination> Project<TDestination>(IQueryable<TSource> source) where TDestination : class
	{
		var adapter = adapters.GetValueRefOrNullRef(typeof(TDestination));
		if (adapter == null!)
		{
			throw new InvalidOperationException(
				$"Adapter of {typeof(TSource)} to {typeof(TDestination)} has not been registered.");
		}

		return (IQueryable<TDestination>)adapter.Project(source);
	}

	public TDestination Map<TDestination>(object source) where TDestination : class => Map<TDestination>((TSource)source);

	public void Patch<TDestination>(object source, TDestination destination) where TDestination : class
		=> Patch((TSource)source, destination);

	public IQueryable<TDestination> Project<TDestination>(IQueryable source) where TDestination : class => Project<TDestination>((IQueryable<TSource>)source);
}