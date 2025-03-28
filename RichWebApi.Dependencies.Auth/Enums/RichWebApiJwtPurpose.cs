namespace RichWebApi.Enums;

public enum RichWebApiJwtPurpose : byte
{
	Unknown = 0,
	Mfa,
	Access,
	Refresh
}