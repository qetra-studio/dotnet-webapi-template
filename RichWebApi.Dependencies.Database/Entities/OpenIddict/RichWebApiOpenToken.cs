using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using OpenIddict.EntityFrameworkCore.Models;
using RichWebApi.Entities.Configuration;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Entities.OpenIddict;

[Table("Tokens", Schema = "OpenIddict")]
public class RichWebApiOpenToken
	: OpenIddictEntityFrameworkCoreToken<Guid, RichWebApiOpenApplication, RichWebApiOpenAuthorization>, IAuditableEntity
{
	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? ModifiedAt { get; set; }

	public class Configurator : EntityConfiguration<RichWebApiOpenToken>;

	[UsedImplicitly]
	public sealed class Validator : AbstractValidator<RichWebApiOpenToken>;
}