using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Entities;

public interface IDateAuditableEntity : IEntity
{
	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset? ModifiedAt { get; set; }

	[UsedImplicitly]
	public class Validator : AbstractValidator<IDateAuditableEntity>
	{
		public Validator()
		{
			RuleFor(x => x.CreatedAt).GreaterThanOrEqualTo(EntityValidatorConstants.DefaultDateTime);
			RuleFor(x => x.ModifiedAt)
				.GreaterThanOrEqualTo(EntityValidatorConstants.DefaultDateTime)
				.When(x => x.ModifiedAt is not null);
		}
	}
}