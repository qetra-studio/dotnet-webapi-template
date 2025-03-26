namespace RichWebApi.Models;

public sealed class AuthErrorDto : IOutbound
{
	public string ErrorCode { get; set; }
	
	public string Message { get; set; }
}