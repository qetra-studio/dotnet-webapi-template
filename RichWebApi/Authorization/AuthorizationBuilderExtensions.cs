using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace RichWebApi.Authorization;

public static class AuthorizationBuilderExtensions
{
	public static void ConfigureForAssembly(this AuthorizationBuilder builder, Assembly assembly)
	{
		var typesToScan = assembly.DefinedTypes;
		var addProfileMappingMethods = typesToScan
			.Where(type => type.IsAssignableTo(typeof(IHasAuthorization)) && !type.IsAbstract)
			.Select(type => type.GetMethod(nameof(IHasAuthorization.ConfigureAuthorization))!)
			.ToList();

		foreach (var addProfileMappingMethod in addProfileMappingMethods)
		{
			addProfileMappingMethod.Invoke(null, [builder]);
		}
	}

}