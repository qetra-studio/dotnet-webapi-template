using System.Text.RegularExpressions;

namespace RichWebApi.Utilities;

public static partial class AuthRegex
{
	[GeneratedRegex(@"^\w(?:\w|[.-](?=\w)){2,255}$")]
	public static partial Regex UserNameRegex { get; }
}