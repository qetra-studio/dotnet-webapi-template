using System.Collections.Frozen;
using Microsoft.Extensions.Logging;
using RichWebApi.Dependencies;
using RichWebApi.Extensions;
using RichWebApi.Parts;
using static System.Linq.Expressions.Expression;
using Maps = System.Collections.Frozen.FrozenDictionary<System.Type, RichWebApi.Mappers.TypeAdapter>;

namespace RichWebApi.Mappers;

internal sealed class Mapper(
	IEnumerable<IAppPart> parts,
	IEnumerable<IAppDependency> dependencies,
	ILogger<Mapper> logger)
	: IMapper
{
	private readonly FrozenDictionary<Type, Maps> _sourceMap =
		logger.Time(() => CollectMappers(parts, dependencies), "Collect mappers across app");

	private static FrozenDictionary<Type, Maps> CollectMappers(
		IEnumerable<IAppPart> parts, IEnumerable<IAppDependency> dependencies)
		=> parts.Select(x => x.GetType())
			.Concat(dependencies.Select(x => x.GetType()))
			.Select(x => x.Assembly)
			.Distinct()
			.SelectMany(assembly =>
				assembly
					.GetTypes()
					.Where(t => t is { IsClass: true, IsAbstract: false }
								&& t.GetInterfaces().Any(i =>
									i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasAdapter<,>)))
					.SelectMany(mapperType => mapperType.GetInterfaces()
						.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasAdapter<,>))
						.Select(i =>
						{
							var genericArgs = i.GetGenericArguments();
							var destinationType = genericArgs[1];

							var mapInfo = mapperType.GetMethods()
								.First(m => m.Name == nameof(IHasAdapter<object, object>.Map)
											&& m.ReturnType == destinationType);
							var map = Delegate.CreateDelegate(
								typeof(Func<,>).MakeGenericType(genericArgs[0], destinationType), mapInfo);
							var mapParams = new[]
							{
								Parameter(typeof(object), "source"),
							};
							var castedMap = Lambda<Func<object, object>>(
								Convert(
									Invoke(Constant(map), Convert(mapParams[0], genericArgs[0])),
									typeof(object)),
								mapParams
							).Compile();

							var queryableType = typeof(IQueryable<>).MakeGenericType(destinationType);
							var projectInfo = mapperType.GetMethods()
								.First(m => m.Name == nameof(IHasAdapter<object, object>.Project)
											&& m.ReturnType == queryableType);
							var project = Delegate.CreateDelegate(
								typeof(Func<,>).MakeGenericType(
									typeof(IQueryable<>).MakeGenericType(genericArgs[0]),
									typeof(IQueryable<>).MakeGenericType(destinationType)),
								projectInfo);
							var projectParams = new[]
							{
								Parameter(typeof(IQueryable), "source"),
							};
							var castedProject = Lambda<Func<IQueryable, IQueryable>>(
								Convert(
									Invoke(Constant(project),
										Convert(projectParams[0],
											typeof(IQueryable<>).MakeGenericType(genericArgs[0]))),
									typeof(IQueryable)),
								projectParams
							).Compile();


							var updateInfo = mapperType.GetMethods()
								.First(m =>
								{
									var parameters = m.GetParameters();
									return m.Name == nameof(IHasAdapter<object, object>.Patch)
										   && parameters.Length == 2
										   && parameters[0].ParameterType == genericArgs[0]
										   && parameters[1].ParameterType == destinationType;
								});
							var update = Delegate.CreateDelegate(
								typeof(Action<,>).MakeGenericType(genericArgs[0], destinationType),
								updateInfo);
							var updateParams = new[]
							{
								Parameter(typeof(object), "source"),
								Parameter(typeof(object), "destination")
							};
							var castedUpdate = Lambda<Action<object, object>>(
								Invoke(Constant(update),
									Convert(updateParams[0], genericArgs[0]),
									Convert(updateParams[1], destinationType)),
								updateParams
							).Compile();
							return new
							{
								SourceType = genericArgs[0],
								DestinationType = genericArgs[1],
								Adapter = new TypeAdapter(castedUpdate, castedMap, castedProject)
							};
						})))
			.GroupBy(x => x.SourceType)
			.ToFrozenDictionary(x => x.Key,
				x => x.ToFrozenDictionary(e => e.DestinationType, e => e.Adapter));

	public ITypeMapper<TSource> Of<TSource>() where TSource : class
	{
		var reference = _sourceMap.GetValueRefOrNullRef(typeof(TSource));
		if (reference == null!)
		{
			throw new InvalidOperationException($"Adapter set for {typeof(TSource)} has not been registered.)");
		}

		return new TypeMapper<TSource>(reference);
	}

	public ITypeMapper Of(Type type)
	{
		var reference = _sourceMap.GetValueRefOrNullRef(type);
		if (reference == null!)
		{
			throw new InvalidOperationException($"Adapter set for {type} has not been registered.)");
		}

		return new TypeMapper<object>(reference);
	}

	public TDestination Map<TDestination>(object source) where TDestination : class
	{
		var reference = _sourceMap.GetValueRefOrNullRef(source.GetType());
		if (reference == null!)
		{
			throw new InvalidOperationException($"Adapter set for {source.GetType()} has not been registered.)");
		}

		if (reference.GetValueRefOrNullRef(typeof(TDestination)) is { } adapter)
		{
			return (TDestination)adapter.Map(source);
		}

		throw new InvalidOperationException(
			$"Adapter of {source.GetType()} to {typeof(TDestination)} has not been registered.");
	}

	public void Patch<TSource, TDestination>(TDestination destination, TSource value)
		where TSource : class where TDestination : class
	{
		var reference = _sourceMap.GetValueRefOrNullRef(typeof(TSource));
		if (reference == null!)
		{
			throw new InvalidOperationException($"Adapter set for {typeof(TSource)} has not been registered.)");
		}

		if (reference.GetValueRefOrNullRef(typeof(TDestination)) is { } adapter)
		{
			adapter.Update(value, destination);
			return;
		}

		throw new InvalidOperationException(
			$"Adapter of {typeof(TSource)} to {typeof(TDestination)} has not been registered.");
	}
}