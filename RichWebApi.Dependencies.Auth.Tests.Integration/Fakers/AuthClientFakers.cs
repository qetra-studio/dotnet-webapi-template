using Bogus;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Fakers;

public static class AuthClientFakers
{
	public static Faker<RegisterDto> RegisterDto(this IFakerFactory factory)
		=> new Faker<RegisterDto>()
			.RuleSet("random", ruleSet => ruleSet
				.RuleFor(x => x.Email, x => x.Internet.Email())
				.RuleFor(x => x.UserName, x => x.Internet.UserName())
				.RuleFor(x => x.Password, x => x.Internet.Password(length: 32, prefix: "!Va1id.")));
}