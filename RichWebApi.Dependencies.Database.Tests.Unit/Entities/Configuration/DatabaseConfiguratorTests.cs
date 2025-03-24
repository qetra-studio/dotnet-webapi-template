using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using RichWebApi.Entities.Configuration;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Entities.Configuration;

public class DatabaseConfiguratorTests : UnitTest
{
	private readonly DependencyContainerFixture _container;

	public DatabaseConfiguratorTests(ITestOutputHelper testOutputHelper, UnitDependencyContainerFixture container) :
		base(testOutputHelper)
	{
		var parts = new AppPartsCollection
		{
			new DatabaseUnitTestsPart()
		};
		_container = container
			.WithXunitLogging(testOutputHelper)
			.WithTestScopeInMemoryDatabase(parts, b =>
			{
				// as we need to reset model state which impacts on condition whether OnModelCreating will be called or not
				b.EnableServiceProviderCaching(false);
			})
			.ConfigureServices(s => s.RemoveAll<INonGenericEntityConfiguration>());
	}

	[Fact]
	public async Task IgnoresIgnoredConfigurations()
	{
		var configMock = Substitute.For<IgnoredEntity.Configurator>();
		configMock.Configure(Arg.Any<EntityTypeBuilder<IgnoredEntity>>());
		var sp = _container
			.ReplaceWithMock<INonGenericEntityConfiguration>(configMock)
			.ConfigureServices(s
				=> s.TryAddEnumerable(ServiceDescriptor
					.Singleton<INonGenericEntityConfiguration, UnitDateAuditableEntity.Configurator>()))
			.BuildServiceProvider();
		await TriggerDbContextConfigurationAsync(sp);
		configMock.DidNotReceive().Configure(Arg.Any<EntityTypeBuilder<IgnoredEntity>>());
	}

	[Fact]
	public async Task CallsEntityConfiguration()
	{
		var configMock = Substitute.For<ConfigurableEntity.Configurator>();
		configMock.When(x => x.Configure(Arg.Any<EntityTypeBuilder<ConfigurableEntity>>()))
			.Do(x => x.Arg<EntityTypeBuilder<ConfigurableEntity>>().HasNoKey());
		var sp = _container
			.ReplaceWithMock<INonGenericEntityConfiguration>(configMock)
			.ConfigureServices(s
				=> s.TryAddEnumerable(ServiceDescriptor
					.Singleton<INonGenericEntityConfiguration, UnitDateAuditableEntity.Configurator>()))
			.BuildServiceProvider();
		await TriggerDbContextConfigurationAsync(sp);
		configMock
			.Received(1)
			.Configure(Arg.Any<EntityTypeBuilder<ConfigurableEntity>>());
	}

	private static ValueTask<EntityEntry<UnitDateAuditableEntity>> TriggerDbContextConfigurationAsync(IServiceProvider sp)
	{
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		return dbContext.AddAsync(new UnitDateAuditableEntity());
	}

	public override async Task DisposeAsync()
	{
		await base.DisposeAsync();
		_container.Clear();
	}
}