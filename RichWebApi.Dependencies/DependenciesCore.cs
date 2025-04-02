using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Dependencies;
using RichWebApi.Parts;
using Riok.Mapperly.Abstractions;

[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]
namespace RichWebApi;

internal class DependenciesCore : IAppDependency
{
	public void ConfigureServices(IServiceCollection services, IAppPartsCollection parts)
	{
	}

	public void ConfigureApplication(IApplicationBuilder builder)
	{
	}
}