using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("RoleClaims", Schema = "Identity")]
public class RichWebApiRoleClaim : IdentityRoleClaim<Guid>, IAuditableEntity, ISoftDeletableEntity
{
	public RichWebApiRole Role { get; set; } = null!;


	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
	public DateTimeOffset? ModifiedAt { get; set; }
	public Guid? DeletedById { get; set; }
	public RichWebApiUser? DeletedBy { get; set; }
	public DateTimeOffset? DeletedAt { get; set; }
	
	public class Configurator : EntityConfiguration<RichWebApiRoleClaim>;
	
	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiRoleClaim>;

}