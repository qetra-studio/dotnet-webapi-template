using System.Collections.Concurrent;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using RichWebApi.Tests.Exceptions;
using RichWebApi.Tests.Extensions;

namespace RichWebApi.Tests.Resources;

// designed for usage as collection fixture
// but at the moment of July 26, 2023 - xunit not supports assembly-level fixtures (in other words, xunit scans only executing assembly for collection fixture definitions)
// so you need to implement collection definitions for every assembly where you want to use it as collection fixture
[UsedImplicitly]
public sealed class ResourceRepositoryFixture : IResourceScope
{
	private readonly ConcurrentDictionary<Assembly, string[]> _assemblyEmbeddedResources = new();

	private readonly List<ResourceScope> _innerScopes = [];

	public string Scope => string.Empty;

	public Stream GetResourceStream(string nameSubstring)
	{
		var foundMatches = _assemblyEmbeddedResources
			.Select(x => new
			{
				Assembly = x.Key,
				ResourceNames = x.Value
					.Where(v => v.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase))
					.ToArray()
			})
			.Where(x => x.ResourceNames.Length != 0)
			.ToArray();

		switch (foundMatches.Length)
		{
			case 0:
				{
					var formattedAssemblies = _assemblyEmbeddedResources
						.Select(x => x.Key.FullName)
						.ToJson(Formatting.Indented);
					throw new TestConfigurationException(
						$"Resources with [{nameSubstring}] in it's name have not been found across assemblies: {formattedAssemblies}"
						+ $"{Environment.NewLine}"
						+ $"Check if you've marked it as EmbeddedResource");
				}
			case > 1:
				throw new TestConfigurationException(
					$"Resources with [{nameSubstring}] in it's name have been found in more than one assembly: {foundMatches.ToJson(Formatting.Indented)}");
		}

		var match = foundMatches[0];

		if (match.ResourceNames.Length > 1)
		{
			throw new TestConfigurationException(
				$"Multiple resources [{nameSubstring}] with in it's name have been found in assembly: {match.ToJson(Formatting.Indented)}");
		}

		var stream = match.Assembly.GetManifestResourceStream(match.ResourceNames[0])!;
		return stream;
	}

	public IResourceScope CreateTestScope<T>(T test) where T : Test
	{
		ReadFromAssembly<T>();
		return CreateScope(test.GetType().Name);
	}

	public void Dispose()
	{
		_assemblyEmbeddedResources.Clear();

		foreach (var scope in _innerScopes)
		{
			scope.Dispose();
		}
	}

	public IResourceScope CreateScope(string scope)
	{
		var newScope = new ResourceScope(scope, this);
		_innerScopes.Add(newScope);
		return newScope;
	}

	private string[] ReadFromAssembly<T>() =>
		_assemblyEmbeddedResources.GetOrAdd(typeof(T).Assembly, static a => a.GetManifestResourceNames());
}