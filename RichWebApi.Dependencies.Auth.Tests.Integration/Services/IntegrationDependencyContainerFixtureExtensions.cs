using Microsoft.Extensions.DependencyInjection.Extensions;
using RichWebApi.Tests.DependencyInjection;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Services;

public static class IntegrationDependencyContainerFixtureExtensions
{
	public static DependencyContainerFixture WithAuthServices(this DependencyContainerFixture container)
		=> container.ConfigureServices(x => x.TryAddSingleton<JwtVerifier>());
}