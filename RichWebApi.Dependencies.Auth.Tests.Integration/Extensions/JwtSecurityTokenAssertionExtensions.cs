using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;

namespace RichWebApi.Tests.Extensions;

public static class JwtSecurityTokenAssertionExtensions
{
	public static Claim ShouldHavePurposeClaim(this JwtSecurityToken token, string value)
	{
		var purposeClaim = token.Claims.FirstOrDefault(x => x.Type == "purpose");
		purposeClaim.Should().NotBeNull();
		purposeClaim.Value.Should().Contain(value);
		return purposeClaim;
	}

	public static Guid ShouldHaveUserId(this JwtSecurityToken token)
	{
		var userIdClaim = token.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub);
		userIdClaim.Should().NotBeNull();
		Guid.TryParse(userIdClaim.Value, out var userId).Should().BeTrue();
		userId.Should().NotBeEmpty();
		return userId;
	}

	public static string ShouldHaveStamp(this JwtSecurityToken token)
	{
		var stampClaim = token.Claims.FirstOrDefault(x => x.Type == "stamp");
		stampClaim.Should().NotBeNull();
		stampClaim.Value.Should().NotBeNull().And.NotBeEmpty();
		return stampClaim.Value;
	}
}