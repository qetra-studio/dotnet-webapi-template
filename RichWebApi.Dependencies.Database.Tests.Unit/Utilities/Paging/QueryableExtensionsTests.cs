using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Parts;
using RichWebApi.Persistence;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Entities;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using RichWebApi.Tests.Persistence;
using RichWebApi.Utilities.Paging;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Utilities.Paging;

public class QueryableExtensionsTests : UnitTest
{
	private readonly IServiceProvider _serviceProvider;

	public QueryableExtensionsTests(ITestOutputHelper testOutputHelper, UnitDependencyContainerFixture container) :
		base(testOutputHelper)
	{
		var parts = new AppPartsCollection
		{
			new DatabaseUnitTestsPart()
		};
		_serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithTestScopeInMemoryDatabase(parts)
			.ReplaceWithEmptyMock<IValidator<ConfigurableEntity>>()
			.ReplaceWithEmptyMock<IValidator<IgnoredEntity>>()
			.ConfigureServices(s => s.AddAppParts(parts))
			.BuildServiceProvider();
	}

	[Fact]
	public async Task CountsCorrectly()
	{
		var database = _serviceProvider.GetRequiredService<IRichWebApiDatabase>();
		var entities = new[]
		{
			new UnitDateAuditableEntity(),
			new UnitDateAuditableEntity()
		};
		await database.PersistEntitiesAsync(entities);
		await database.PersistAsync();
		var page = await database.ReadAsync((db, ct) => db.Context
			.Set<UnitDateAuditableEntity>()
			.ToPagedResultAsync(new UnitPagedRequest(0, 0), ct), default);
		page.Should().BeEquivalentTo(new
		{
			Page = 0,
			Size = 0,
			Total = 2
		});
	}

	[Fact]
	public async Task RetrievesSetPart()
	{
		var database = _serviceProvider.GetRequiredService<IRichWebApiDatabase>();
		var entities = new[]
		{
			new UnitDateAuditableEntity(),
			new UnitDateAuditableEntity()
		};
		await database.PersistEntitiesAsync(entities);
		await database.PersistAsync();
		var page = await database.ReadAsync((db, ct) => db.Context
			.Set<UnitDateAuditableEntity>()
			.ToPagedResultAsync(new UnitPagedRequest(0, 1), ct), default);
		page.Should().BeEquivalentTo(new PagedResult<UnitDateAuditableEntity>(entities.Take(1).ToArray(), 0, 1, 2));
	}

	[Fact]
	public async Task RetrievesEmptyPage()
	{
		var database = _serviceProvider.GetRequiredService<IRichWebApiDatabase>();
		var entities = new[]
		{
			new UnitDateAuditableEntity(),
			new UnitDateAuditableEntity()
		};
		await database.PersistEntitiesAsync(entities);
		await database.PersistAsync();
		var page = await database.ReadAsync((db, ct) => db.Context
			.Set<UnitDateAuditableEntity>()
			.Where(x => x.Invalid)
			.ToPagedResultAsync(new UnitPagedRequest(0, entities.Length), ct), default);
		page.Should().BeEquivalentTo(new PagedResult<UnitDateAuditableEntity>(Array.Empty<UnitDateAuditableEntity>(), 0, entities.Length, 0));
	}

	[Fact]
	public async Task RetrievesConditionBasedPage()
	{
		var database = _serviceProvider.GetRequiredService<IRichWebApiDatabase>();
		var entities = new[]
		{
			new UnitDateAuditableEntity(),
			new UnitDateAuditableEntity()
		};
		await database.PersistEntitiesAsync(entities);
		await database.PersistAsync();
		var page = await database.ReadAsync((db, ct) => db.Context
			.Set<UnitDateAuditableEntity>()
			.Where(x => x.Id == 2)
			.ToPagedResultAsync(new UnitPagedRequest(0, entities.Length), ct), default);
		page.Should().BeEquivalentTo(new PagedResult<UnitDateAuditableEntity>(entities.Skip(1).ToArray(), 0, entities.Length, 1));
	}

	private record UnitPagedRequest(int Page, int Size) : IPagedRequest;
}