using JetBrains.Annotations;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using RichWebApi.Entities.OpenIddict;
using static OpenIddict.Abstractions.OpenIddictConstants.Permissions;
using static OpenIddict.Abstractions.OpenIddictConstants.Requirements;

namespace RichWebApi.Startup;

[UsedImplicitly]
internal sealed class OpenIddictInit(OpenIddictApplicationManager<RichWebApiOpenApplication> manager, OpenIddictScopeManager<RichWebApiOpenScope> scopeManager) : IAsyncStartupAction
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
					GrantTypes.RefreshToken,
					OpenIddictConstants.Permissions.Prefixes.Scope + "weather"
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
					GrantTypes.RefreshToken,

					ResponseTypes.Code,

					Scopes.Email,
					Scopes.Profile,
					OpenIddictConstants.Permissions.Prefixes.Scope + "weather"
				},
				Requirements =
				{
					Features.ProofKeyForCodeExchange
				},
				ApplicationType = OpenIddictConstants.ApplicationTypes.Web
			},
			new()
			{
				ClientId = "test_app_client2",
				ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
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
					GrantTypes.RefreshToken,

					ResponseTypes.Code,

					Scopes.Email,
					Scopes.Profile,
					OpenIddictConstants.Permissions.Prefixes.Scope + "weather"
				},
				Requirements =
				{
					Features.ProofKeyForCodeExchange
				},
				ApplicationType = OpenIddictConstants.ApplicationTypes.Web
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

		var weatherScopeDescriptor = new OpenIddictScopeDescriptor
		{
			DisplayName = "Weather",
			Name = "weather",
			Resources = { "api-weather" }
		};
		if (await scopeManager.FindByNameAsync(weatherScopeDescriptor.Name, cancellationToken) is not {} scope)
		{
			await scopeManager.CreateAsync(weatherScopeDescriptor, cancellationToken);
		}
		else
		{
			await scopeManager.PopulateAsync(scope, weatherScopeDescriptor, cancellationToken);
		}
	}
}