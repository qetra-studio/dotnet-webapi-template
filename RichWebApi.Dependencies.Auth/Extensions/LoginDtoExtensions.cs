using RichWebApi.Enums;
using RichWebApi.Models;

namespace RichWebApi.Extensions;

public static class LoginDtoExtensions
{
	public static LoginValueKind DefineLoginKind(this LoginDto l)
	{
		if (l.Login.Contains('@'))
		{
			return LoginValueKind.Email;
		}

		if (l.Login.StartsWith("+"))
		{
			return LoginValueKind.PhoneNumber;
		}

		return LoginValueKind.UserName;
	}
}