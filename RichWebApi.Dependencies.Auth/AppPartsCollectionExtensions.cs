namespace RichWebApi.Parts.Auth;

public static class AppDependenciesCollectionExtensions
{
	public static IAppDependenciesCollection AddAuth(this IAppDependenciesCollection dependencies)
	{
		dependencies.Add(new AuthPart());
		return dependencies;
	}
}