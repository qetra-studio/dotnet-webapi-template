using System.Net;
using FluentAssertions;
using RichWebApi.Tests.Client;

namespace RichWebApi.Tests.Extensions;

public static class ApiExceptionAssertionExtensions
{
	public static void ShouldHaveStatusCode(this ApiException exception, HttpStatusCode code) => exception.StatusCode.Should().Be((int)code);
}