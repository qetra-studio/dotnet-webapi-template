using RichWebApi.Dependencies;

namespace RichWebApi;

public static class AppDependenciesCollectionExtensions
{
	public static IAppDependenciesCollection AddAuth(this IAppDependenciesCollection dependencies)
	{
		dependencies.Add(new AuthDependency());
		return dependencies;
	}
}