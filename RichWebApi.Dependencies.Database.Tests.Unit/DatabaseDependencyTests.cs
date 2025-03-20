using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using NSubstitute;
using RichWebApi.Config;
using RichWebApi.Entities.Configuration;
using RichWebApi.Persistence;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Entities;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class DatabaseDependencyTests : UnitTest
{
	private readonly ITestOutputHelper _testOutputHelper;
	private readonly DependencyContainerFixture _container;

	public DatabaseDependencyTests(ITestOutputHelper testOutputHelper, UnitDependencyContainerFixture container) : base(
		testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
		_container = container
			.WithXunitLogging(TestOutputHelper);
	}

	[Fact]
	public void CollectsIgnoredEntitiesFromAssembly()
		=> _container
			.WithTestScopeInMemoryDatabase(new AppPartsCollection
			{
				new DatabaseUnitTestsPart()
			})
			.BuildServiceProvider()
			.GetServices<INonGenericEntityConfiguration>()
			.Should()
			.ContainItemsAssignableTo<IIgnoredEntityConfiguration>();

	[Fact]
	public void CollectsConfigurableEntities()
		=> _container
			.WithTestScopeInMemoryDatabase(new AppPartsCollection
			{
				new DatabaseUnitTestsPart()
			})
			.BuildServiceProvider()
			.GetServices<INonGenericEntityConfiguration>()
			.Should()
			.ContainItemsAssignableTo<IEntityConfiguration<ConfigurableEntity>>();

	private static DependencyContainerFixture SetEnvironment(DependencyContainerFixture container,
	                                                         string environmentName)
		=> container
			.ReplaceWithMock<IHostEnvironment>(mock => mock.EnvironmentName
				.Returns(environmentName));
}