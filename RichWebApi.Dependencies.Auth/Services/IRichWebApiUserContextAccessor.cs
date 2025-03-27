using RichWebApi.Entities.Identity;
using RichWebApi.Utilities;

namespace RichWebApi.Services;

public interface IRichWebApiUserContextAccessor
{
	Guid? UserId { get; }
	AsyncLazy<RichWebApiUser?> User { get; }
}