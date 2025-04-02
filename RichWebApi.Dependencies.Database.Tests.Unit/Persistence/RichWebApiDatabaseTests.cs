using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Polly;
using RichWebApi.Entities;
using RichWebApi.Parts;
using RichWebApi.Persistence;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Entities;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using RichWebApi.Utilities;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Persistence;

public class RichWebApiDatabaseTests(ITestOutputHelper testOutputHelper, UnitDependencyContainerFixture container)
	: UnitTest(testOutputHelper)
{
	private readonly DependencyContainerFixture _container = container
		.WithXunitLogging(testOutputHelper);

	[Fact]
	public async Task CallsReadPolicyOnRead()
	{
		var sp = _container
			.ReplaceWithMock<IAsyncPolicy>(mock =>
				mock.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<IEntity>>>(),
							Arg.Any<CancellationToken>()))
			.ReplaceWithMock<IDatabasePolicySet>((sp, mock) =>
				mock.DatabaseReadPolicy
					.Returns(_ => sp.GetRequiredService<IAsyncPolicy>())
				)
			.WithTestScopeInMemoryDatabase(new AppPartsCollection())
			.BuildServiceProvider();
		var policySetMock = sp.GetRequiredService<IDatabasePolicySet>();
		var database = new RichWebApiDatabase(sp.GetRequiredService<RichWebApiDbContext>(), policySetMock);
		await database.ReadAsync((db, ct) => Task.FromResult<IEntity>(new ConfigurableEntity()), default);

		var asyncPolicyMock = sp.GetRequiredService<IAsyncPolicy>();

		var _ = policySetMock.Received(1).DatabaseReadPolicy;
		await asyncPolicyMock.Received(1).ExecuteAsync(
				Arg.Any<Func<CancellationToken, Task<IEntity>>>(),
				Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CallsWritePolicyOnWrite()
	{
		var sp = _container
			.ReplaceWithMock<IAsyncPolicy>(mock =>
				mock.ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(),
							Arg.Any<CancellationToken>()))
			.ReplaceWithMock<IDatabasePolicySet>((sp, mock) =>
				mock.DatabaseWritePolicy
					.Returns(_ => sp.GetRequiredService<IAsyncPolicy>()))
			.WithTestScopeInMemoryDatabase(new AppPartsCollection())
			.BuildServiceProvider();
		var policySetMock = sp.GetRequiredService<IDatabasePolicySet>();
		var database = new RichWebApiDatabase(sp.GetRequiredService<RichWebApiDbContext>(), policySetMock);
		await database.WriteAsync((_, _) => Task.CompletedTask, default);

		var asyncPolicyMock = sp.GetRequiredService<IAsyncPolicy>();

		var _ = policySetMock.Received(1).DatabaseWritePolicy;
		await asyncPolicyMock.Received(1).ExecuteAsync(
				Arg.Any<Func<CancellationToken, Task>>(),
				Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CallsWritePolicyOnPersist()
	{
		var sp = _container
			.ReplaceWithMock<IAsyncPolicy>(mock =>
				mock.ExecuteAsync(Arg.Any<Func<CancellationToken, Task>>(),
							Arg.Any<CancellationToken>()))
			.ReplaceWithMock<IDatabasePolicySet>((sp, mock) =>
				mock.DatabaseWritePolicy
					.Returns(_ => sp.GetRequiredService<IAsyncPolicy>()))
			.WithTestScopeInMemoryDatabase(new AppPartsCollection())
			.BuildServiceProvider();
		var policySetMock = sp.GetRequiredService<IDatabasePolicySet>();
		var database = new RichWebApiDatabase(sp.GetRequiredService<RichWebApiDbContext>(), policySetMock);
		await database.PersistAsync();

		var asyncPolicyMock = sp.GetRequiredService<IAsyncPolicy>();

		var _ = policySetMock.Received(1).DatabaseWritePolicy;
		await asyncPolicyMock.Received(1).ExecuteAsync(
				Arg.Any<Func<CancellationToken, Task>>(),
				Arg.Any<CancellationToken>());
	}
}