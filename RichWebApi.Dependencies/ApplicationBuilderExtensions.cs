using Microsoft.AspNetCore.Builder;
using RichWebApi.Dependencies;

namespace RichWebApi;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseDependencies(this IApplicationBuilder builder, IAppDependenciesCollection dependencies)
	{
		foreach (var d in dependencies)
		{
			d.ConfigureApplication(builder);
		}

		return builder;
	}
}