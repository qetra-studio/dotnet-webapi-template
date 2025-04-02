using Microsoft.Extensions.DependencyInjection;

namespace RichWebApi.Parts;

public interface IAppPart
{
	void ConfigureServices(IServiceCollection services);
}