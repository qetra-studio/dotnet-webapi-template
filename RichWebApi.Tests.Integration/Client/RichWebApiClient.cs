using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace RichWebApi.Tests.Client;

internal abstract class RichWebApiClient : IRichWebApiClient
{
	public AuthenticationHeaderValue? AuthorizationHeader { get; set; }
	protected virtual ValueTask<HttpRequestMessage> CreateHttpRequestMessageAsync(
		CancellationToken cancellationToken = default)
	{
		var message = new HttpRequestMessage();
		if (AuthorizationHeader is not null)
		{
			message.Headers.Authorization = AuthorizationHeader;
		}
		return new ValueTask<HttpRequestMessage>(message);
	}
}