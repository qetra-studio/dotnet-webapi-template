namespace RichWebApi.Services;

public interface IIdentityProvider
{
	Guid? UserId { get; }
}