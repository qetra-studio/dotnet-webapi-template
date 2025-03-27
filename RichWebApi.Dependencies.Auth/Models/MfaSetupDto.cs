namespace RichWebApi.Models;

public sealed class MfaSetupDto : IOutbound
{
	public required string Key { get; init; }
	
	public required string Uri { get; init; }
}