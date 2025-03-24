using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("Roles", Schema = "Identity")]
public class RichWebApiRole : IdentityRole<Guid>, IAuditableEntity
{
	public ICollection<RichWebApiUserRole> UserRoles { get; set; } = new List<RichWebApiUserRole>();

	public ICollection<RichWebApiRoleClaim> Claims { get; set; } = new List<RichWebApiRoleClaim>();

	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
	public Guid? DeletedById { get; set; }
	public RichWebApiUser? DeletedBy { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }
	public DateTimeOffset? DeletedAt { get; set; }

	public class Configurator : EntityConfiguration<RichWebApiRole>
	{
		public override void Configure(EntityTypeBuilder<RichWebApiRole> builder)
		{
			base.Configure(builder);
			builder.HasIndex(x => x.Name);
			builder.HasMany(x => x.Claims).WithOne(x => x.Role).HasForeignKey(x => x.RoleId);
		}
	}

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiRole>;
}