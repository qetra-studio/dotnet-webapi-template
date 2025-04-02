using JetBrains.Annotations;

namespace RichWebApi.Mappers;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithInheritors)]
public interface IHasAdapter<in TSource, TDestination>
{
	static abstract TDestination Map(TSource source);
	static abstract void Patch(TSource update, TDestination destination);

	static abstract IQueryable<TDestination> Project(IQueryable<TSource> source);
}