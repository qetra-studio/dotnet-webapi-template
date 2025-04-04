using JetBrains.Annotations;
using OpenIddict.Abstractions;
using RichWebApi.Entities.OpenIddict;
using static OpenIddict.Abstractions.OpenIddictConstants.Permissions;

namespace RichWebApi.Startup;

[UsedImplicitly]
internal sealed class OpenIddictInit(IOpenIddictApplicationManager manager) : IAsyncStartupAction
{
	public uint Order => 99;

	public async Task PerformActionAsync(CancellationToken cancellationToken = default)
	{
		OpenIddictApplicationDescriptor appDescriptor = new()
		{
			ClientId = "test_client",
			ClientSecret = "test_secret",
			ClientType = OpenIddictConstants.ClientTypes.Confidential,
			DisplayName = "App Test",
			RedirectUris = { new Uri("https://localhost:4001/callback") },
			Permissions =
			{
				Endpoints.Token,
				Endpoints.Authorization,

				GrantTypes.ClientCredentials,
				GrantTypes.AuthorizationCode,

				ResponseTypes.Code,

				Prefixes.Scope + "test_scope"
			}
		};

		var client = await manager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);
		if (client == null)
		{
			await manager.CreateAsync(appDescriptor, cancellationToken);
		}
		else
		{
			await manager.UpdateAsync(client, appDescriptor, cancellationToken);
		}
	}
}