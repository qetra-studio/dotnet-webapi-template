using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RichWebApi.Constants;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;

namespace RichWebApi.Utilities;

public static class RichWebApiClaimFactory
{
	public static Claim PurposeClaim(RichWebApiAuthPurpose purpose)
		=> new(RichWebApiJwtClaimTypes.Purpose, purpose.ToString("G").ToLower());

	public static Claim ActionClaim(RichWebApiAuthActions action)
		=> new(RichWebApiJwtClaimTypes.Action, action.ToString("G").ToLower());

	public static Claim UserIdClaim(RichWebApiUser user)
		=> new(JwtRegisteredClaimNames.Sub, user.Id.ToString("N"));

	public static Claim StampClaim(RichWebApiUser user)
		=> new(RichWebApiJwtClaimTypes.Stamp, user.SecurityStamp!);
}