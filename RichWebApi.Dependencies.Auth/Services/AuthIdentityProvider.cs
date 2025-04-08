namespace RichWebApi.Services;

internal sealed class AuthIdentityProvider(Lazy<IRichWebApiUserContextAccessor> accessor) : IIdentityProvider
{
	public Guid? UserId => accessor.Value.UserId;
}