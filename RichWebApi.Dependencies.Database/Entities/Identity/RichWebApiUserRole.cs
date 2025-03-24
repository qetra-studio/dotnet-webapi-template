using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("UserRoles", Schema = "Identity")]
public class RichWebApiUserRole : IdentityUserRole<Guid>, IAuditableEntity
{
	public RichWebApiUser User { get; set; } = null!;

	public RichWebApiRole Role { get; set; } = null!;

	public class Configurator : EntityConfiguration<RichWebApiUserRole>
	{
		public override void Configure(EntityTypeBuilder<RichWebApiUserRole> builder)
		{
			base.Configure(builder);
			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId);
			builder.HasOne(x => x.Role)
				.WithMany(x => x.UserRoles)
				.HasForeignKey(x => x.RoleId);
		}
	}

	[ForeignKey(nameof(CreatedBy))]
	public Guid? CreatedById { get; set; }

	public RichWebApiUser? CreatedBy { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	[ForeignKey(nameof(ModifiedBy))]
	public Guid? ModifiedById { get; set; }

	public RichWebApiUser? ModifiedBy { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	[ForeignKey(nameof(DeletedBy))]
	public Guid? DeletedById { get; set; }

	public RichWebApiUser? DeletedBy { get; set; }

	public DateTime? DeletedAt { get; set; }
	
	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiUserRole>;
}