
using System.Net.Http.Headers;

namespace RichWebApi.Tests.Client;

public interface IRichWebApiClient
{
	AuthenticationHeaderValue? AuthorizationHeader { get; set; }
}