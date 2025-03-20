using Microsoft.AspNetCore.Hosting;

namespace RichWebApi;

public static class AppDependenciesCollectionExtensions
{
	public static IAppDependenciesCollection AddDatabase(this IAppDependenciesCollection dependencies, IWebHostEnvironment hostEnvironment, Action<DatabaseDependencyOptions>? configure = null)
	{
		var options = new DatabaseDependencyOptions();
		configure?.Invoke(options);
		dependencies.Add(new DatabaseDependency(hostEnvironment, options));
		return dependencies;
	}
}