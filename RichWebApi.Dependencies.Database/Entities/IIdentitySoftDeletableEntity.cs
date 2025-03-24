using System.ComponentModel.DataAnnotations.Schema;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Entities;

public interface IIdentitySoftDeletableEntity : IEntity
{
	[ForeignKey(nameof(DeletedBy))] Guid? DeletedById { get; set; }
	RichWebApiUser? DeletedBy { get; set; }
}