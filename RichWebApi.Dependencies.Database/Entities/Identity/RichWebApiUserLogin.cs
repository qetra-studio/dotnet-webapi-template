using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("UserLogins", Schema = "Identity")]
public class RichWebApiUserLogin : IdentityUserLogin<Guid>, IAuditableEntity
{
	public class Configurator : EntityConfiguration<RichWebApiUserLogin>;

	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? ModifiedAt { get; set; }
	
	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiUserLogin>;
}