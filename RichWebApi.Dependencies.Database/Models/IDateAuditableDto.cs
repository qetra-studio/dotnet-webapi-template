namespace RichWebApi.Models;

public interface IDateAuditableDto
{
	DateTimeOffset CreatedAt { get; set; }
	DateTimeOffset? ModifiedAt { get; set; }
}