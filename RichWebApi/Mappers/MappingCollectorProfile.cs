using AutoMapper;

namespace RichWebApi.Mappers;

public abstract class MappingCollectorProfile : Profile
{
	protected MappingCollectorProfile()
	{
		var typesToScan = GetType().Assembly.DefinedTypes;
		var addProfileMappingMethods = typesToScan
			.Where(type => type.IsAssignableTo(typeof(IHasMapping)) && !type.IsAbstract)
			.Select(type => type.GetMethod(nameof(IHasMapping.AddProfileMapping))!)
			.ToList();

		foreach (var addProfileMappingMethod in addProfileMappingMethods)
		{
			addProfileMappingMethod.Invoke(null, [this]);
		}
	}
}