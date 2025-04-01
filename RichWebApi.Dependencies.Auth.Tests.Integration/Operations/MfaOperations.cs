using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using RichWebApi.Tests.Client;
using RichWebApi.Tests.Extensions;

namespace RichWebApi.Tests.Operations;

public static class MfaOperations
{
	public static async Task<Totp> SetupMfaAsync(this IServiceProvider serviceProvider, AuthSuccessDto auth)
	{
		var mfaClient = serviceProvider.GetRequiredService<IMfaClient>();
		mfaClient.SetAccessToken(auth);
		var result = await mfaClient.SetupAsync();
		result.ShouldHaveStatusCode(HttpStatusCode.OK);
		var setup = result.Result;
		var totp = new Totp(Base32Encoding.ToBytes(setup.Key));
		var action = () => mfaClient.VerifyAsync(new VerifyMfaDto
		{
			Token = totp.ComputeTotp()
		});

		var assert = await action.Should().NotThrowAsync();
		var response = assert.Subject;
		
		response.ShouldHaveStatusCode(HttpStatusCode.OK);

		return totp;
	}
}