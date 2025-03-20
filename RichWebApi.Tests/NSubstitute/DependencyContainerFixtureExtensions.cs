using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using RichWebApi.Tests.DependencyInjection;

namespace RichWebApi.Tests.NSubstitute;

public static class DependencyContainerFixtureExtensions
{
	public static DependencyContainerFixture ReplaceWithMock<TService>(
		this DependencyContainerFixture fixture,
		Action<IServiceProvider, TService>? configure = null,
		object[]? args = null) where TService : class
		=> fixture
			.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton(sp =>
			{
				var mock = Substitute.For<TService>(args ?? []);
				configure?.Invoke(sp, mock);
				return mock;
			})));

	public static DependencyContainerFixture ReplaceWithMock<TService>(
		this DependencyContainerFixture fixture,
		TService instance) where TService : class
		=> fixture
			.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton(instance)));

	public static DependencyContainerFixture ReplaceWithMock<TService>(
		this DependencyContainerFixture fixture,
		Action<TService>? configure = null,
		object[]? args = null) where TService : class
		=> fixture.ReplaceWithMock<TService>((_, mock) => configure?.Invoke(mock), args);

	public static DependencyContainerFixture ReplaceWithEmptyMock<TService>(
		this DependencyContainerFixture fixture,
		object[]? args = null) where TService : class
		=> fixture.ReplaceWithMock<TService>((_, _) => { }, args);
}