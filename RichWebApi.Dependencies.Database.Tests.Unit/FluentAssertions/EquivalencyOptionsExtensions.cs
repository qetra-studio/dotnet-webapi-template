using FluentAssertions.Equivalency;
using RichWebApi.Entities;
using RichWebApi.Models;

namespace RichWebApi.Tests.FluentAssertions;

public static class EquivalencyOptionsExtensions
{
	public static EquivalencyOptions<T> ExcludingAuditableEntityProperties<T>(
		this EquivalencyOptions<T> options) where T : class
	{
		var auditableEntityType = typeof(IAuditableEntity);
		return options
			.Excluding(info => info.DeclaringType.IsAssignableTo(auditableEntityType)
								 && (info.Name == nameof(IAuditableEntity.CreatedAt)
									 || info.Name == nameof(IAuditableEntity.ModifiedAt)));
	}

	public static EquivalencyOptions<T> ExcludingSoftDeletableEntityProperties<T>(
		this EquivalencyOptions<T> options) where T : class
	{
		var softDeletableEntityType = typeof(ISoftDeletableEntity);
		return options
			.Excluding(info => info.DeclaringType.IsAssignableTo(softDeletableEntityType) && info.Name == nameof(ISoftDeletableEntity.DeletedAt));
	}

	public static EquivalencyOptions<T> ExcludingAuditableDtoProperties<T>(
		this EquivalencyOptions<T> options) where T : class
	{
		var auditableEntityType = typeof(IAuditableDto);
		return options
			.Excluding(info => info.DeclaringType.IsAssignableTo(auditableEntityType)
							   && (info.Name == nameof(IAuditableDto.CreatedAt)
								   || info.Name == nameof(IAuditableDto.ModifiedAt)));
	}

	public static EquivalencyOptions<T> ExcludingSoftDeletableDtoProperties<T>(
		this EquivalencyOptions<T> options) where T : class
	{
		var softDeletableEntityType = typeof(ISoftDeletableDto);
		return options
			.Excluding(info => info.DeclaringType.IsAssignableTo(softDeletableEntityType) && info.Name == nameof(ISoftDeletableDto.DeletedAt));
	}
}