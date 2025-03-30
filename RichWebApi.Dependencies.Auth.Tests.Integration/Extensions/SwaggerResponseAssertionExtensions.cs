using System.Net;
using FluentAssertions;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Extensions;

public static class SwaggerResponseAssertionExtensions
{
	public static void ShouldHaveStatusCode(this SwaggerResponse response, HttpStatusCode code)
		=> response.StatusCode.Should().Be((int)code);

	public static void ShouldHaveStatusCode<T>(this SwaggerResponse<T> response, HttpStatusCode code)
		=> response.StatusCode.Should().Be((int)code);
}