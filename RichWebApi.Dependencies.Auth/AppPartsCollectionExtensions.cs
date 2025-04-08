using Microsoft.AspNetCore.Hosting;
using RichWebApi.Dependencies;

namespace RichWebApi;

public static class AppDependenciesCollectionExtensions
{
	public static IAppDependenciesCollection AddAuth(this IAppDependenciesCollection dependencies,
													 IWebHostEnvironment env)
	{
		dependencies.Add(new AuthDependency(env));
		return dependencies;
	}
}