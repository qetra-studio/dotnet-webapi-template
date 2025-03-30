using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.DependencyInjection;
using RichWebApi.Tests.Extensions;
using RichWebApi.Tests.Fakers;
using RichWebApi.Tests.Logging;
using RichWebApi.Tests.Services;
using Xunit.Abstractions;

namespace RichWebApi.Tests;

public class RegisterTests : IntegrationTest
{
	private readonly IServiceProvider _serviceProvider;

	public RegisterTests(ITestOutputHelper testOutputHelper, IntegrationDependencyContainerFixture container) : base(
		testOutputHelper)
		=> _serviceProvider = container
			.WithXunitLogging(TestOutputHelper)
			.WithAuthServices()
			.BuildServiceProvider();

	[Theory]
	[InlineData("", "", "", "")]
	[InlineData("validUsername", "", "asd@mail.com", "")]
	[InlineData("validUsername", "aaaaaaaaaaaaaaaaaaa", "asd@mail.com", "")]
	[InlineData("", "ASd123.!", "asd@mail.com", "")]
	[InlineData("validUsername", "ASd123.!", "", "")]
	[InlineData("valid.Username", "ASd123.!", "", "")]
	[InlineData("valid.Username123123", "ASd123.!", "", "")]
	[InlineData("123213valid.Username", "ASd123.!", "", "+2754")]
	public async Task RegisterBadCredentialsCause400(string username, string password, string email,
													 string? phoneNumber)
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var action = () => client.RegisterAsync(new RegisterDto
		{
			UserName = username,
			Email = email,
			Password = password,
			PhoneNumber = phoneNumber
		});
		var assert = await action.Should().ThrowAsync<ApiException<ValidationResponseDto>>();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreatesRandomUser()
	{
		var client = _serviceProvider.GetRequiredService<IAuthClient>();
		var user = _serviceProvider
			.GetRequiredService<IFakerFactory>()
			.RegisterDto()
			.Generate("random");
		var action = () => client.RegisterAsync(user);
		var assert = await action.Should().NotThrowAsync();
		assert.Which.ShouldHaveStatusCode(HttpStatusCode.OK);
	}
}