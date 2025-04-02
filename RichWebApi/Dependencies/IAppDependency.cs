using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Parts;

namespace RichWebApi.Dependencies;

public interface IAppDependency
{
	void ConfigureServices(IServiceCollection services, IAppPartsCollection parts);
	void ConfigureApplication(IApplicationBuilder builder);
}