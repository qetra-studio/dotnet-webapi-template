using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Entities.Identity;

[Table("Users", Schema = "Identity")]
public class RichWebApiUser : IdentityUser<Guid>, IDateAuditableEntity
{
	public DateTime? EmailConfirmedAt { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public class Configurator : EntityConfiguration<RichWebApiUser>;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiUser>;
}