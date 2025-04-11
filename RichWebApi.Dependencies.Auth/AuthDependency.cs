using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using RichWebApi.Authorization.Schemes;
using RichWebApi.Authorization.Schemes.Challenge;
using RichWebApi.Config;
using RichWebApi.Dependencies;
using RichWebApi.Entities.Identity;
using RichWebApi.Entities.OpenIddict;
using RichWebApi.Parts;
using RichWebApi.Services;
using RichWebApi.Services.OpenIddict;
using RichWebApi.Startup;
using RichWebApi.Validation;
using Riok.Mapperly.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

[assembly: InternalsVisibleTo("RichWebApi.Dependencies.Auth.Tests.Unit")]
[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]

namespace RichWebApi;

internal class AuthDependency(IWebHostEnvironment env) : IAppDependency
{
	public void ConfigureServices(IServiceCollection services, IAppPartsCollection parts)
	{
		services.AddOptionsWithValidator<AuthConfig, AuthConfig.Validator>("Dependencies:Auth");
		services.AddOptionsWithValidator<MfaConfig, MfaConfig.Validator>("Dependencies:Auth:Mfa");
		var sp = services.BuildServiceProvider();
		var authConfig = sp.GetRequiredService<IOptionsMonitor<AuthConfig>>();
		var keys = UpdateKeys(authConfig.CurrentValue);
		authConfig.OnChange(x => keys = UpdateKeys(x));

		IssuerSigningKeyResolver issuerSigningKeyResolver
			= (_, _, kid, _) => keys
				.Where(x => x.Key == kid)
				.Select(x => x.Value);

		
		AddRichWebApiAuthServices();
		AddIdentityServices();
		AddOpenIddictServices();
		
		services.AddAuthentication(b =>
			{
				b.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
				b.DefaultChallengeScheme = RichWebApiAuthConstants.ChallengeScheme;
			})
			.AddScheme<AuthenticationSchemeOptions, RichWebApiChallengeHandler>(RichWebApiAuthConstants.ChallengeScheme, RichWebApiAuthConstants.ChallengeScheme, _ => {});

		services.ConfigureApplicationCookie(options =>
		{
			options.Cookie.HttpOnly = true;
			options.ExpireTimeSpan = TimeSpan.FromDays(30);
			options.SlidingExpiration = true;
			options.Cookie.IsEssential = true;
			options.Cookie.SameSite = SameSiteMode.Lax;
			options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

			options.Cookie.MaxAge = TimeSpan.FromDays(30);
			options.Events.OnRedirectToLogin = context =>
			{
				context.Response.Redirect($"https://local.richwebapi.com/login?returnUrl={context.RedirectUri}");
				return Task.CompletedTask;
			};
			options.Events.OnRedirectToAccessDenied = context =>
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				return Task.CompletedTask;
			};
		});
		
		services.AddAuthorization();
		var builder = services.AddAuthorizationBuilder();

		builder.AddDefaultPolicy("default", x =>
		{
			x.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
			x.RequireAuthenticatedUser();
		});

		return;

		void AddOpenIddictServices()
		{
			services.AddOpenIddict()
				.AddCore(options =>
				{
					options.UseEntityFrameworkCore().UseDbContext<RichWebApiDbContext>()
						.ReplaceDefaultEntities<RichWebApiOpenApplication, RichWebApiOpenAuthorization, RichWebApiOpenScope,
							RichWebApiOpenToken, Guid>();
				})
				.AddServer(options =>
				{
					options
						.AllowClientCredentialsFlow()
						.AllowAuthorizationCodeFlow()
						.AllowRefreshTokenFlow()
						.RequireProofKeyForCodeExchange();

					options.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email);

					options.UseAspNetCore()
						.EnableTokenEndpointPassthrough()
						.EnableAuthorizationEndpointPassthrough();
					
					options.AddSigningKeys(keys.Values);
					options.AddEncryptionKeys(keys.Values);

					options.SetIssuer("https://localhost:7262");
					options
						.SetJsonWebKeySetEndpointUris("https://localhost:7262/.well-known/jwks.json")
						.SetTokenEndpointUris("https://localhost:7262/auth/connect/token")
						.SetAuthorizationEndpointUris("https://localhost:7262/auth/connect/authorize")
						.SetUserInfoEndpointUris("https://localhost:7262/auth/connect/userinfo");
					if (env.IsDevelopment())
					{
						options.DisableAccessTokenEncryption();
					}
				})
				.AddValidation(options =>
				{
					options.Configure(o =>
					{
						o.TokenValidationParameters.ValidateLifetime = true;
						o.TokenValidationParameters.ValidateIssuerSigningKey = true;
						o.TokenValidationParameters.IssuerSigningKeyResolver = issuerSigningKeyResolver;
					});
					options.UseLocalServer();
					options.UseAspNetCore();
				});
			services.AddSingleton<IClaimDestinationsProvider, ClaimDestinationsProvider>();
			services.AddStartupAction<OpenIddictInit>();
		}

		void AddIdentityServices()
		{
			services.AddIdentity<RichWebApiUser, RichWebApiRole>(options =>
				{
					options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
					options.ClaimsIdentity.EmailClaimType = Claims.Email;
					options.ClaimsIdentity.RoleClaimType = Claims.Role;
					options.ClaimsIdentity.UserNameClaimType = Claims.Name;
					options.User.RequireUniqueEmail = true;
				})
				.AddEntityFrameworkStores<RichWebApiDbContext>()
				.AddDefaultTokenProviders();
			services.ConfigureApplicationCookie(options =>
			{
				options.Events.OnRedirectToLogin = context =>
				{
					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				};
				options.Events.OnRedirectToAccessDenied = context =>
				{
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				};
			});
		}

		void AddRichWebApiAuthServices()
		{
			services.TryAddScoped<IRichWebApiUserContextAccessor, RichWebApiUserContextAccessor>();
			services.TryAddScoped<IIdentityProvider>(serviceProvider
				=> new AuthIdentityProvider(
					new Lazy<IRichWebApiUserContextAccessor>(serviceProvider
						.GetRequiredService<IRichWebApiUserContextAccessor>)));
			
			services.AddCors(options =>
			{
				options.AddPolicy("auth", builder =>
				{
					if (env.IsDevelopment())
					{
						builder.WithOrigins("https://local.richwebapi.com");
					}

					builder.WithOrigins("https://identity.richwebapi.com")
						.WithMethods("POST");
				});
			});
		}

		IReadOnlyDictionary<string, SecurityKey> UpdateKeys(AuthConfig cfg)
			=> cfg.RsaKeys.ToDictionary(x => x.Key, SecurityKey (x) =>
			{
				var rsa = RSA.Create();
				rsa.ImportFromPem(x.Value.Public);
				rsa.ImportFromPem(x.Value.Private);
				return new RsaSecurityKey(rsa)
				{
					KeyId = x.Key
				};
			});
	}

	public void ConfigureApplication(IApplicationBuilder builder)
	{
		builder.UseAuthentication();
		builder.UseAuthorization();
	}
}