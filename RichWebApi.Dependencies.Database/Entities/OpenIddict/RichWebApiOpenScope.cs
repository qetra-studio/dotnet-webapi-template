using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using OpenIddict.EntityFrameworkCore.Models;
using RichWebApi.Entities.Configuration;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Entities.OpenIddict;

[Table("Scopes", Schema = "OpenIddict")]
public class RichWebApiOpenScope : OpenIddictEntityFrameworkCoreScope<Guid>, IAuditableEntity
{
	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? ModifiedAt { get; set; }

	public class Configurator : EntityConfiguration<RichWebApiOpenScope>;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiOpenScope>;
}