
namespace RichWebApi.Models;

public sealed class ValidationResponseDto : IOutbound
{
	public required Dictionary<string, ValidationErrorDto[]> Errors { get; init; } = [];
}