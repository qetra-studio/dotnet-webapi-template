using RichWebApi.Entities.Identity;

namespace RichWebApi.Services.Jwt;

public interface IJwtTokenIssuer
{
	Task<IssuedJWT> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken);

	Task<IssuedJWT> IssueTwoFactorTokenAsync(RichWebApiUser user, CancellationToken cancellationToken);
}