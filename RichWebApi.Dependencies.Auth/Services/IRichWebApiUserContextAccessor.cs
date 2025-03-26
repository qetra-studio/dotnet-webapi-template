using RichWebApi.Entities.Identity;
using RichWebApi.Utilities;

namespace RichWebApi.Services;

public interface IRichWebApiUserContextAccessor : IIdentityProvider
{
	AsyncLazy<RichWebApiUser?> User { get; }
}