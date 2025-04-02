using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichWebApi.Parts;
using RichWebApi.Persistence.Interceptors;
using RichWebApi.Services;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Entities;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Persistence.Interceptors;

public class AuditSaveChangesInterceptorTests : UnitTest
{
	private readonly DependencyContainerFixture _container;

	public AuditSaveChangesInterceptorTests(ITestOutputHelper testOutputHelper,
											UnitDependencyContainerFixture container) : base(testOutputHelper)
	{
		var parts = new AppPartsCollection
		{
			new DatabaseUnitTestsPart()
		};
		_container = container
			.WithXunitLogging(testOutputHelper)
			.ReplaceWithEmptyMock<ILoggingOptions>()
			.WithTestScopeInMemoryDatabase(parts);
	}

	[Fact]
	public async Task DontDoAnythingIfUnknownDbContext()
	{
		var now = DateTimeOffset.UtcNow;
		var sp = _container
			.ReplaceWithMock<ISystemClock>(mock => SetupClockMock(mock, now))
			.BuildServiceProvider();
		var clockMock = sp.GetRequiredService<ISystemClock>();
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), null);

		var interceptor = new AuditSaveChangesInterceptor(sp.GetRequiredService<ILogger<AuditSaveChangesInterceptor>>(),
			clockMock,
			sp.GetRequiredService<IIdentityProvider>());
		await interceptor.SavingChangesAsync(eventData, default);
		var _ = clockMock.DidNotReceive().UtcNow;
	}

	[Fact]
	public async Task AuditsCreatedAuditableEntity()
	{
		var now = DateTimeOffset.UtcNow;
		var sp = _container
			.ReplaceWithMock<ISystemClock>(mock => SetupClockMock(mock, now))
			.BuildServiceProvider();
		var clockMock = sp.GetRequiredService<ISystemClock>();
		var entity = new UnitDateAuditableEntity();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);

		await dbContext.AddAsync(entity);
		var interceptor = new AuditSaveChangesInterceptor(sp.GetRequiredService<ILogger<AuditSaveChangesInterceptor>>(),
			clockMock,
			sp.GetRequiredService<IIdentityProvider>());
		await interceptor.SavingChangesAsync(eventData, default);
		var _ = clockMock.Received(1).UtcNow;
		entity.Should().BeEquivalentTo(new UnitDateAuditableEntity
		{
			Id = 1,
			CreatedAt = now.DateTime,
			ModifiedAt = now.DateTime,
			DeletedAt = null
		});
	}

	[Fact]
	public async Task AuditsModifiedAuditableEntity()
	{
		var now = DateTimeOffset.UtcNow;
		var sp = _container
			.ReplaceWithMock<ISystemClock>(mock => SetupClockMock(mock, now))
			.BuildServiceProvider();
		var clockMock = sp.GetRequiredService<ISystemClock>();
		var entity = new UnitDateAuditableEntity();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);

		var entry = await dbContext.AddAsync(entity);
		var interceptor = new AuditSaveChangesInterceptor(sp.GetRequiredService<ILogger<AuditSaveChangesInterceptor>>(),
			clockMock,
			sp.GetRequiredService<IIdentityProvider>());
		await interceptor.SavingChangesAsync(eventData, default);

		var aBitLater = now.AddDays(1);
		clockMock.UtcNow
			.Returns(aBitLater);
		entry.State = EntityState.Modified;

		await interceptor.SavingChangesAsync(eventData, default);
		entity.Should().BeEquivalentTo(new UnitDateAuditableEntity
		{
			Id = 1,
			CreatedAt = now.DateTime,
			ModifiedAt = aBitLater.DateTime,
			DeletedAt = null
		});
		var _ = clockMock.Received(2).UtcNow;
	}

	[Fact]
	public async Task AuditsSoftDeletedEntity()
	{
		var now = DateTimeOffset.UtcNow;
		var sp = _container
			.ReplaceWithMock<ISystemClock>(mock => SetupClockMock(mock, now))
			.BuildServiceProvider();
		var clockMock = sp.GetRequiredService<ISystemClock>();
		var entity = new UnitDateAuditableEntity();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);

		var entry = await dbContext.AddAsync(entity);
		var interceptor = new AuditSaveChangesInterceptor(sp.GetRequiredService<ILogger<AuditSaveChangesInterceptor>>(),
			clockMock,
			sp.GetRequiredService<IIdentityProvider>());

		await interceptor.SavingChangesAsync(eventData, default);
		entry.State = EntityState.Deleted;

		await interceptor.SavingChangesAsync(eventData, default);

		entity.Should().BeEquivalentTo(new UnitDateAuditableEntity
		{
			Id = 1,
			CreatedAt = now.DateTime,
			ModifiedAt = now.DateTime,
			DeletedAt = now.DateTime
		});
		var _ = clockMock.Received(2).UtcNow;
	}

	private static void SetupClockMock(ISystemClock mock, DateTimeOffset now)
		=> mock.UtcNow
			.Returns(now);
}