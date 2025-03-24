using AutoMapper;
using RichWebApi.Entities;
using RichWebApi.Models;

namespace RichWebApi.Mappers;

public static class MappingExpressionExtensions
{
	public static IMappingExpression<TSource, TDestination> IgnoreAuditableProperties<TSource, TDestination>(
		this IMappingExpression<TSource, TDestination> expr)
		where TSource : class, IAuditableDto
		where TDestination : class, IDateAuditableEntity
		=> expr.IgnoreMember(x => x.CreatedAt)
			.IgnoreMember(x => x.ModifiedAt);

	public static IMappingExpression<TSource, TDestination> IgnoreSoftDeletableProperties<TSource, TDestination>(
		this IMappingExpression<TSource, TDestination> expr)
		where TSource : class, ISoftDeletableDto
		where TDestination : class, IDateSoftDeletableEntity
		=> expr.IgnoreMember(x => x.DeletedAt);
}