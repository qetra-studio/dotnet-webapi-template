using System.Linq.Expressions;
using Destructurama.Attributed;
using FluentValidation;
using JetBrains.Annotations;
using RichWebApi.Validation;

namespace RichWebApi.Config;

internal class DatabaseConnectionConfig : IAppConfig
{
	[NotLogged]
	public string Password { get; set; } = null!;

	[NotLogged]
	public ushort Port { get; set; }

	[NotLogged]
	public string Host { get; set; } = null!;

	[NotLogged]
	public string Username { get; set; } = null!;

	[NotLogged]
	public string DbInstanceIdentifier { get; set; } = null!;

	public int Retries { get; set; }

	public int Timeout { get; set; }

	public string[] Additional { get; set; } = [];

	[UsedImplicitly]
	public class ProdEnvValidator : AbstractValidator<DatabaseConnectionConfig>
	{
		public ProdEnvValidator()
		{
			RuleFor(x => x.Retries).GreaterThan(0);
			RuleFor(x => x.Timeout).GreaterThan(0);
			RuleFor(x => x.Port).GreaterThan((ushort)0);
			RuleForConnectionStringPart(x => x.Host);
			RuleForConnectionStringPart(x => x.Password);
			RuleForConnectionStringPart(x => x.Username);
			RuleForConnectionStringPart(x => x.DbInstanceIdentifier);
			RuleFor(x => x.ToConnectionString()).SqlServerConnectionString();
			return;

			void RuleForConnectionStringPart(Expression<Func<DatabaseConnectionConfig, string>> expr)
				=> RuleFor(expr)
					.Must(x => !string.IsNullOrEmpty(x) && !x.Contains(';'))
					.WithMessage("Should be real and not contain restricted characters");
		}
	}
}