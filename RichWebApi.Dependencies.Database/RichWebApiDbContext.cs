using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RichWebApi.Entities.Configuration;
using RichWebApi.Entities.Identity;

namespace RichWebApi;

public sealed class RichWebApiDbContext(
	IDatabaseConfigurator databaseConfigurator,
	DbContextOptions<RichWebApiDbContext> options)
	: IdentityDbContext<RichWebApiUser, RichWebApiRole, Guid, RichWebApiUserClaim, RichWebApiUserRole, RichWebApiUserLogin, RichWebApiRoleClaim, RichWebApiUserToken>(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
		databaseConfigurator.OnModelCreating(builder);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);
		databaseConfigurator.ConfigureConventions(configurationBuilder, Database);
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new NotSupportedException();

	public override int SaveChanges() => throw new NotSupportedException();
}