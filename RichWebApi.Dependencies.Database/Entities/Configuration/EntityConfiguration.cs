using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RichWebApi.Entities.Identity;
using static System.Linq.Expressions.Expression;

namespace RichWebApi.Entities.Configuration;

public abstract class EntityConfiguration<T> : IEntityConfiguration<T>
	where T : class, IEntity
{
	public virtual void Configure(EntityTypeBuilder<T> builder)
	{
		var type = typeof(T);
		var customAttributes = Attribute.GetCustomAttributes(type);
		var tableAttribute = customAttributes.OfType<TableAttribute>().FirstOrDefault();

		if (tableAttribute != null)
		{
			builder.Metadata.SetTableName(tableAttribute.Name);
			if (!string.IsNullOrEmpty(tableAttribute.Schema))
			{
				builder.Metadata.SetSchema($"app_{tableAttribute.Schema}");
			}
		}

		var commentAttribute = customAttributes.OfType<CommentAttribute>().FirstOrDefault();

		if (commentAttribute != null)
		{
			builder.Metadata.SetComment(commentAttribute.Comment);
		}

		if (builder.Metadata.BaseType == null)
		{
			// 6 bytes per date, just for economy - dates won't have millis;
			var indexPropertyNames = new List<string>(3);

			if (type.IsAssignableTo(typeof(IDateAuditableEntity)))
			{
				builder.Property(nameof(IDateAuditableEntity.CreatedAt)).HasPrecision(0);
				builder.Property(nameof(IDateAuditableEntity.ModifiedAt)).HasPrecision(0);
				indexPropertyNames.AddRange(new[]
				{
					nameof(IDateAuditableEntity.CreatedAt),
					nameof(IDateAuditableEntity.ModifiedAt)
				});
			}

			if (type.IsAssignableTo(typeof(IIdentityAuditableEntity)))
			{
				builder.HasOne(typeof(RichWebApiUser), nameof(IIdentityAuditableEntity.CreatedBy))
					.WithMany()
					.HasForeignKey(nameof(IIdentityAuditableEntity.CreatedById));
				builder.HasOne(typeof(RichWebApiUser), nameof(IIdentityAuditableEntity.ModifiedBy))
					.WithMany()
					.HasForeignKey(nameof(IIdentityAuditableEntity.ModifiedById));
			}

			if (type.IsAssignableTo(typeof(IDateSoftDeletableEntity)))
			{
				builder.Property(nameof(IDateSoftDeletableEntity.DeletedAt)).HasPrecision(0);
				var parameter = Parameter(type);
				var deletedAt = MakeMemberAccess(parameter,
					type.GetProperty(nameof(IDateSoftDeletableEntity.DeletedAt))!);
				var eq = Equal(deletedAt, Constant(null));
				var expression = Lambda<Func<T, bool>>(eq, parameter);
				builder.HasQueryFilter(expression);
				indexPropertyNames.Add(nameof(IDateSoftDeletableEntity.DeletedAt));
			}

			if (type.IsAssignableTo(typeof(IIdentitySoftDeletableEntity)))
			{
				builder.HasOne(typeof(RichWebApiUser), nameof(IIdentitySoftDeletableEntity.DeletedBy))
					.WithMany()
					.HasForeignKey(nameof(IIdentitySoftDeletableEntity.DeletedById));
			}

			if (indexPropertyNames.Count != 0)
			{
				builder.HasIndex(indexPropertyNames.ToArray());
			}
		}

		foreach (var mutableNavigation in builder.Metadata.GetNavigations())
		{
			mutableNavigation.ForeignKey.DeleteBehavior = DeleteBehavior.ClientCascade;
		}
	}

	
}

[UsedImplicitly]
public sealed class BasicEntityValidator<T> : AbstractValidator<T> where T : class, IEntity
{
	public BasicEntityValidator(IValidator<IDateAuditableEntity> aeValidator,
	                            IValidator<IDateSoftDeletableEntity> sdeValidator)
	{
		var type = typeof(T);
		if (type.IsAssignableTo(typeof(IDateAuditableEntity)))
		{
			RuleFor(x => (IDateAuditableEntity)x).SetValidator(aeValidator);
		}
		if (type.IsAssignableTo(typeof(IDateSoftDeletableEntity)))
		{
			RuleFor(x => (IDateSoftDeletableEntity)x).SetValidator(sdeValidator);
		}
	}
}