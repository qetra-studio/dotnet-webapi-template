using System.Text;

namespace RichWebApi.Config;

internal static class DatabaseConnectionConfigExtensions
{
	public static string ToConnectionString(this DatabaseConnectionConfig x)
		=> new StringBuilder(
				$"Server=tcp:{x.Host},{x.Port};Initial Catalog={x.DbInstanceIdentifier};User ID={x.Username};Password={x.Password};")
			.Append(x.Additional.Length != 0
				? $"{string.Join(";", x.Additional)};"
				: string.Empty)
			.ToString();
}