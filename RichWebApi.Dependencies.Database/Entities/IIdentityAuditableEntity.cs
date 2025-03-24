using System.ComponentModel.DataAnnotations.Schema;
using RichWebApi.Entities.Identity;

namespace RichWebApi.Entities;

public interface IIdentityAuditableEntity : IEntity
{
	[ForeignKey(nameof(CreatedBy))]
	public Guid? CreatedById { get; set; }
	public RichWebApiUser? CreatedBy { get; set; }
	
	[ForeignKey(nameof(ModifiedBy))]
	public Guid? ModifiedById { get; set; }
	public RichWebApiUser? ModifiedBy { get; set; }
}