namespace RichWebApi.Models;

public interface IAuditableDto
{
	DateTimeOffset CreatedAt { get; set; }
	DateTimeOffset? ModifiedAt { get; set; }
}