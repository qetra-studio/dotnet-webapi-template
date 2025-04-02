using System.Collections;

namespace RichWebApi.Dependencies;

public sealed class AppDependenciesCollection : IAppDependenciesCollection
{
	private readonly HashSet<IAppDependency> _dependencies = new(new TypeEqualityComparer<IAppDependency>())
	{
		new DependenciesCore()
	};

	public IEnumerator<IAppDependency> GetEnumerator() => _dependencies.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void Add(IAppDependency item) => _dependencies.Add(item);

	public void Clear()
		=> _dependencies.Clear();

	public bool Contains(IAppDependency item)
		=> _dependencies.Contains(item);

	public void CopyTo(IAppDependency[] array, int arrayIndex)
		=> _dependencies.CopyTo(array, arrayIndex);

	public bool Remove(IAppDependency item)
		=> _dependencies.Remove(item);

	public int Count => _dependencies.Count;

	public bool IsReadOnly => false;
}