namespace RichWebApi.Tests.Client;

public interface IHasJwt
{
	string AccessToken { get; }
	string TokenType { get; }
	int ExpiresIn { get; }
}

public partial record AuthSuccessDto : IHasJwt;
public partial record MfaSetupDto : IHasJwt;
public partial record AuthActionRequiredDto : IHasJwt;