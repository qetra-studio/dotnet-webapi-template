using JetBrains.Annotations;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants.Permissions;
using static OpenIddict.Abstractions.OpenIddictConstants.Requirements;

namespace RichWebApi.Startup;

[UsedImplicitly]
internal sealed class OpenIddictInit(IOpenIddictApplicationManager manager) : IAsyncStartupAction
{
	public uint Order => 99;

	public async Task PerformActionAsync(CancellationToken cancellationToken = default)
	{
		OpenIddictApplicationDescriptor[] descriptors =
		[
			new()
			{
				ClientId = "test_client",
				ClientSecret = "test_secret",
				ClientType = OpenIddictConstants.ClientTypes.Confidential,
				DisplayName = "Test API",
				RedirectUris =
				{
					new Uri("https://local.richwebapi.com/callback")
				},
				Permissions =
				{
					Endpoints.Token,
					GrantTypes.ClientCredentials,
				},
				ApplicationType = OpenIddictConstants.ApplicationTypes.Web
			},
			new()
			{
				ClientId = "test_app_client",
				ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
				DisplayName = "Test App",
				ClientType = OpenIddictConstants.ClientTypes.Public,
				PostLogoutRedirectUris =
				{
					new Uri("https://local.richwebapi.com:7262/signout-callback-oidc"),
					new Uri("https://localhost:7262/signout-callback-oidc")
				},
				RedirectUris =
				{
					new Uri("https://local.richwebapi.com:7262/swagger/oauth2-redirect.html"),
					new Uri("https://localhost:7262/swagger/oauth2-redirect.html")
				},
				Permissions =
				{
					Endpoints.Authorization,
					Endpoints.Token,

					GrantTypes.AuthorizationCode,

					ResponseTypes.Code,

					Scopes.Email,
					Scopes.Profile,
				},
				Requirements =
				{
					Features.ProofKeyForCodeExchange
				}
			}
		];
		foreach (var descriptor in descriptors)
		{
			var client = await manager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken);
			if (client == null)
			{
				await manager.CreateAsync(descriptor, cancellationToken);
			}
			else
			{
				await manager.UpdateAsync(client, descriptor, cancellationToken);
			}
		}
	}
}