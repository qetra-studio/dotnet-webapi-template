using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

			if (type.IsAssignableTo(typeof(IAuditableEntity)))
			{
				builder.Property(nameof(IAuditableEntity.CreatedAt)).HasPrecision(0);
				builder.Property(nameof(IAuditableEntity.ModifiedAt)).HasPrecision(0);
				indexPropertyNames.AddRange(new[]
				{
					nameof(IAuditableEntity.CreatedAt),
					nameof(IAuditableEntity.ModifiedAt)
				});
			}

			if (type.IsAssignableTo(typeof(ISoftDeletableEntity)))
			{
				builder.Property(nameof(ISoftDeletableEntity.DeletedAt)).HasPrecision(0);
				var parameter = Parameter(type);
				var deletedAt = MakeMemberAccess(parameter, type.GetProperty(nameof(ISoftDeletableEntity.DeletedAt))!);
				var eq = Equal(deletedAt, Constant(null));
				var expression = Lambda<Func<T, bool>>(eq, parameter);
				builder.HasQueryFilter(expression);
				indexPropertyNames.Add(nameof(ISoftDeletableEntity.DeletedAt));
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