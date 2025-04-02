using RichWebApi.Dependencies;

namespace RichWebApi;

public static class AppDependenciesCollectionExtensions
{
	public static IAppDependenciesCollection AddSignalR(this IAppDependenciesCollection collection, Action<ISignalRConfigurator> configureSignalR)
	{
		var dependency = new SignalRDependency();
		configureSignalR.Invoke(dependency);
		collection.Add(dependency);
		return collection;
	}
}