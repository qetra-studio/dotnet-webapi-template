using FluentValidation;

namespace RichWebApi.Config;

public sealed class StartupConfig : IAppConfig
{
	public bool IgnoreStartupActions { get; set; }

	public sealed class Validator : AbstractValidator<StartupConfig>;
}