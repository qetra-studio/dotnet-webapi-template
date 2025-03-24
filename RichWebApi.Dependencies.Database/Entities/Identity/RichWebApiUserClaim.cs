using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("UserClaims", Schema = "Identity")]
public class RichWebApiUserClaim : IdentityUserClaim<Guid>, IAuditableEntity
{
	public RichWebApiUser User { get; set; } = null!;

	public Guid? CreatedById { get; set; }

	public RichWebApiUser? CreatedBy { get; set; }

	public Guid? ModifiedById { get; set; }

	public RichWebApiUser? ModifiedBy { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public class Configurator : EntityConfiguration<RichWebApiUserClaim>;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiUserClaim>;
}