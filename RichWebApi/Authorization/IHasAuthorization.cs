using Microsoft.AspNetCore.Authorization;

namespace RichWebApi.Authorization;

public interface IHasAuthorization
{
	static abstract void ConfigureAuthorization(AuthorizationBuilder builder);
}