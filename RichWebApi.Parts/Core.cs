using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Parts;
using Riok.Mapperly.Abstractions;

[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]
namespace RichWebApi;

internal class Core : IAppPart
{
	public void ConfigureServices(IServiceCollection services)
	{

	}
}