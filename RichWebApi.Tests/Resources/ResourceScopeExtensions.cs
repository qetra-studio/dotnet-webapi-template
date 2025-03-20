using System.Runtime.CompilerServices;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Newtonsoft.Json;
using RichWebApi.Tests.Exceptions;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Resources;

public static class ResourceScopeExtensions
{
	public static IResourceScope CreateMethodScope(this IResourceScope scope,
												   [CallerMemberName] string? callerMemberName = null)
	{
		if (string.IsNullOrEmpty(callerMemberName))
		{
			throw new ArgumentNullException(nameof(callerMemberName));
		}

		if (callerMemberName.Equals(".ctor"))
		{
			throw new NotSupportedException("Constructor scopes are not supported");
		}

		return scope.CreateScope(callerMemberName);
	}

	public static T GetJsonInputResource<T>(this IResourceScope resourceScope, string suffix = "input",
											TestJsonSerializerSettings? jsonSettings = null)
		=> GetJsonResource<T>(resourceScope, suffix, jsonSettings);

	public static Stream GetInputResourceStream(this IResourceScope resourceScope, string suffix = "input")
		=> resourceScope.GetResourceStream(suffix);

	public static void CompareWithJsonExpectation<T>(this IResourceScope resourceScope,
													 ITestOutputHelper testOutputHelper,
													 T actual,
													 string suffix = "expected",
													 TestJsonSerializerSettings? jsonSettings = null,
													 Func<EquivalencyOptions<T>,
														 EquivalencyOptions<T>>? configure = null)
		where T : class
	{
		var definedJsonSettings = jsonSettings
								  ?? new TestJsonSerializerSettings
								  {
									  Formatting = Formatting.Indented
								  };
		var expectation = GetJsonResource<T>(resourceScope, suffix);
		var actualCopy = actual.JsonCopy(definedJsonSettings);

		try
		{
			actualCopy.Should().BeEquivalentTo(expectation, x =>
			{
				configure?.Invoke(x);
				x.PreferringRuntimeMemberTypes();
				return x;
			});
		}
		catch
		{
			testOutputHelper.WriteLine("Actual for {0} is:\n{1}", resourceScope.Scope.ConcatScopeString(suffix),
				actualCopy.ToJson(definedJsonSettings));
			throw;
		}
	}


	private static T GetJsonResource<T>(IResourceScope resourceScope, string suffix,
										TestJsonSerializerSettings? jsonSettings = null)
	{
		var stream = resourceScope.GetResourceStream(suffix);
		using var sr = new StreamReader(stream);
		using var reader = new JsonTextReader(sr);
		return JsonSerializer
				   .Create(jsonSettings ?? new TestJsonSerializerSettings())
				   .Deserialize<T>(reader)
			   ?? throw new TestDataException(
				   $"Failed to deserialize JSON object with {resourceScope.Scope.ConcatScopeString(suffix)} in it's name");
	}
}