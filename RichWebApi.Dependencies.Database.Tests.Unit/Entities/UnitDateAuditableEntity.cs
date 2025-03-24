using FluentValidation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RichWebApi.Entities;
using RichWebApi.Entities.Configuration;

namespace RichWebApi.Tests.Entities;

public class UnitDateAuditableEntity : IDateAuditableEntity, IDateSoftDeletableEntity
{
	public long Id { get; set; }
	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	public DateTimeOffset? DeletedAt { get; set; }

	public bool Invalid { get; set; }

	public class Configurator : EntityConfiguration<UnitDateAuditableEntity>
	{
		public override void Configure(EntityTypeBuilder<UnitDateAuditableEntity> builder)
		{
			base.Configure(builder);
			builder.HasKey(x => x.Id);
		}
	}

	[UsedImplicitly]
	public class Validator : AbstractValidator<UnitDateAuditableEntity>
	{
		public Validator()
		{
			RuleFor(x => x.Invalid).Equal(false);
		}
	}
}