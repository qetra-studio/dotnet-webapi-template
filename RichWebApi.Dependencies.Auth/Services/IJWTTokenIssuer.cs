using RichWebApi.Entities.Identity;

namespace RichWebApi.Services;

public interface IJwtTokenIssuer
{
	Task<string> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken);
}