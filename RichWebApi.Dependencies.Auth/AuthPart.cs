using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Entities.Identity;

[assembly: InternalsVisibleTo("RichWebApi.Dependencies.Auth.Tests.Unit")]
namespace RichWebApi.Parts.Auth;

public class AuthPart : IAppDependency
{
	public void ConfigureServices(IServiceCollection services, IAppPartsCollection parts)
	{
		services.AddIdentity<RichWebApiUser, RichWebApiRole>()
			.AddEntityFrameworkStores<RichWebApiDbContext>()
			.AddDefaultTokenProviders();
		throw new NotImplementedException();
	}

	public void ConfigureApplication(IApplicationBuilder builder)
	{
		throw new NotImplementedException();
	}
}