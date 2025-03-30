using AutoMapper;
using RichWebApi.Entities.Identity;
using RichWebApi.Mappers;

namespace RichWebApi.Models;

public class UserProfileDto : IOutbound, IDateAuditableDto, IHasMapping
{
	public string? UserName { get; set; }

	public string? PhoneNumber { get; set; }
	public bool PhoneNumberConfirmed { get; set; }


	public string Email { get; set; } = null!;
	public bool EmailConfirmed { get; set; }
	public DateTimeOffset? EmailConfirmedAt { get; set; }

	public bool TwoFactorEnabled { get; set; }
	public DateTimeOffset? TwoFactorEnabledAt { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public static void AddProfileMapping(Profile profile)
		=> profile.CreateMap<RichWebApiUser, UserProfileDto>(MemberList.Destination);
}