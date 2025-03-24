using FluentValidation;
using JetBrains.Annotations;

namespace RichWebApi.Entities;

public interface IDateSoftDeletableEntity : IEntity
{
	DateTimeOffset? DeletedAt { get; set; }

	[UsedImplicitly]
	public class Validator : AbstractValidator<IDateSoftDeletableEntity>
	{
		public Validator()
			=> RuleFor(x => x.DeletedAt)
				.GreaterThanOrEqualTo(EntityValidatorConstants.DefaultDateTime)
				.When(x => x.DeletedAt is not null);
	}
}