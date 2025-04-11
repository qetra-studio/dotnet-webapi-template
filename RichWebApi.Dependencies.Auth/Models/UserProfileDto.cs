using RichWebApi.Entities.Identity;
using RichWebApi.Mappers;
using Riok.Mapperly.Abstractions;

namespace RichWebApi.Models;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]

public partial class UserProfileDto : IOutbound, IDateAuditableDto, IHasAdapter<RichWebApiUser, UserProfileDto>
{
	public string? UserName { get; set; }

	public string? PhoneNumber { get; set; }
	public bool PhoneNumberConfirmed { get; set; }


	public string? Email { get; set; }
	public bool EmailConfirmed { get; set; }
	public DateTimeOffset? EmailConfirmedAt { get; set; }

	public bool TwoFactorEnabled { get; set; }
	public DateTimeOffset? TwoFactorEnabledAt { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public static partial UserProfileDto Map(RichWebApiUser source);

	public static partial void Patch(RichWebApiUser update, UserProfileDto destination);

	public static partial IQueryable<UserProfileDto> Project(IQueryable<RichWebApiUser> source);
}