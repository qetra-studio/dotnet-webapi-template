using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichWebApi.Config;
using RichWebApi.Parts;
using RichWebApi.Persistence.Interceptors;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Entities;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.NSubstitute;
using Xunit.Abstractions;

namespace RichWebApi.Tests.Persistence.Interceptors;

public class ValidationSaveChangesInterceptorTests : UnitTest
{
	private readonly DependencyContainerFixture _container;

	public ValidationSaveChangesInterceptorTests(ITestOutputHelper testOutputHelper,
												 UnitDependencyContainerFixture container) : base(testOutputHelper)
	{
		var parts = new AppPartsCollection
		{
			new DatabaseUnitTestsPart()
		};
		_container = container
			.WithXunitLogging(testOutputHelper)
			.ReplaceWithEmptyMock<ILoggingOptions>()
			.WithTestScopeInMemoryDatabase(parts)
			.ReplaceWithEmptyMock<IValidator<ConfigurableEntity>>()
			.ReplaceWithEmptyMock<IValidator<IgnoredEntity>>()
			.ConfigureServices(s => s.AddAppParts(parts));
	}

	[Fact]
	public async Task DontDoAnythingIfUnknownDbContext()
	{
		var sp = _container
			.SetDatabaseEntitiesConfig(EntitiesValidationOption.Required)
			.BuildServiceProvider();
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), null);
		var interceptor = new ValidationSaveChangesInterceptor(sp,
			sp.GetRequiredService<ILogger<ValidationSaveChangesInterceptor>>(),
			sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>());

		await interceptor.SavingChangesAsync(eventData, default);

		var optionsMock = sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>();
		var _ = optionsMock.DidNotReceive().CurrentValue;
	}

	[Fact]
	public async Task DontDoAnythingIfValidationNone()
	{
		var sp = _container
			.SetDatabaseEntitiesConfig(EntitiesValidationOption.None)
			.BuildServiceProvider();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		await dbContext.AddAsync(new UnitDateAuditableEntity
		{
			Invalid = true
		});
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);
		var interceptor = new ValidationSaveChangesInterceptor(sp,
			sp.GetRequiredService<ILogger<ValidationSaveChangesInterceptor>>(),
			sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>());

		await interceptor.SavingChangesAsync(eventData, default);

		var optionsMock = sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>();
		var _ = optionsMock.Received().CurrentValue;
	}

	[Fact]
	public async Task CallsProvidedValidator()
	{
		var entity = new UnitDateAuditableEntity();
		var sp = _container
			.SetDatabaseEntitiesConfig(EntitiesValidationOption.Required)
			.ReplaceWithMock<IValidator<UnitDateAuditableEntity>>(mock => mock.ValidateAsync(Arg.Is<UnitDateAuditableEntity>(e => e == entity),
					Arg.Any<CancellationToken>())
				.Returns(new ValidationResult()))
			.BuildServiceProvider();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		await dbContext.AddAsync(entity);
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);
		var interceptor = new ValidationSaveChangesInterceptor(sp,
			sp.GetRequiredService<ILogger<ValidationSaveChangesInterceptor>>(),
			sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>());

		await interceptor.SavingChangesAsync(eventData, default);

		var validatorMock = sp.GetRequiredService<IValidator<UnitDateAuditableEntity>>();
		await validatorMock.Received(1).ValidateAsync(Arg.Is<UnitDateAuditableEntity>(e => e == entity), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ThrowsIfSomeOfEntitiesAreInvalid()
	{
		var entity = new UnitDateAuditableEntity
		{
			Invalid = true
		};
		var sp = _container
			.SetDatabaseEntitiesConfig(EntitiesValidationOption.Required)
			.BuildServiceProvider();
		var dbContext = sp.GetRequiredService<RichWebApiDbContext>();
		await dbContext.AddAsync(entity);
		var eventData = new UnitTestDbContextEventData(sp.GetRequiredService<ILoggingOptions>(), dbContext);
		var interceptor = new ValidationSaveChangesInterceptor(sp,
			sp.GetRequiredService<ILogger<ValidationSaveChangesInterceptor>>(),
			sp.GetRequiredService<IOptionsMonitor<DatabaseEntitiesConfig>>());

		var intercept = () => interceptor.SavingChangesAsync(eventData, default).AsTask();
		var exceptionAssert = await intercept.Should()
			.ThrowExactlyAsync<AggregateException>();
		exceptionAssert.Which.InnerExceptions
			.Should().ContainEquivalentOf(
				new
				{
					Errors = new[]
					{
						new
						{
							AttemptedValue = true,
							PropertyName = nameof(UnitDateAuditableEntity.Invalid),
							Severity = Severity.Error,
							ErrorCode = "EqualValidator"
						}
					}
				});
	}

	public override async Task DisposeAsync()
	{
		await base.DisposeAsync();
		_container.Clear();
	}
}