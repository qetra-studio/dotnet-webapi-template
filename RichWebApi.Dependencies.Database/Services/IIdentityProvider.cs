namespace RichWebApi.Services;

public interface IIdentityProvider
{
	public Guid? UserId { get; }
}