using RichWebApi.Entities.Identity;
using RichWebApi.Enums;

namespace RichWebApi.Services.Jwt;

public interface IJwtTokenIssuer
{
	Task<IssuedJWT> IssueUserTokenAsync(RichWebApiUser user, CancellationToken cancellationToken = default);

	Task<IssuedJWT> IssueAuthActionTokenAsync(RichWebApiUser user, RichWebApiAuthActions action,
											  CancellationToken cancellationToken = default);
}