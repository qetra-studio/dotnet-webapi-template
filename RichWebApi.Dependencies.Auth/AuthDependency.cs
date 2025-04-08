using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using RichWebApi.Authorization.Requirements;
using RichWebApi.Config;
using RichWebApi.Dependencies;
using RichWebApi.Entities.Identity;
using RichWebApi.Entities.OpenIddict;
using RichWebApi.Parts;
using RichWebApi.Services;
using RichWebApi.Services.Jwt;
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
		services.AddOptionsWithValidator<BearerConfig, BearerConfig.Validator>("Dependencies:Auth:Bearer");
		services.AddOptionsWithValidator<MfaConfig, MfaConfig.Validator>("Dependencies:Auth:Mfa");
		var sp = services.BuildServiceProvider();
		var authConfig = sp.GetRequiredService<IOptionsMonitor<AuthConfig>>();
		var bearerConfig = sp.GetRequiredService<IOptionsMonitor<BearerConfig>>();
		var keys = UpdateKeys(authConfig.CurrentValue);
		authConfig.OnChange(x => keys = UpdateKeys(x));

		services.AddIdentityCore<RichWebApiUser>(options =>
			{
				options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
				options.ClaimsIdentity.EmailClaimType = Claims.Email;
				options.ClaimsIdentity.RoleClaimType = Claims.Role;
				options.ClaimsIdentity.UserNameClaimType = Claims.Name;
				options.User.RequireUniqueEmail = true;
			})
			.AddRoles<RichWebApiRole>()
			.AddSignInManager()
			.AddEntityFrameworkStores<RichWebApiDbContext>()
			.AddDefaultTokenProviders();
		services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
		services.TryAddScoped<IRichWebApiUserContextAccessor, RichWebApiUserContextAccessor>();
		services.TryAddScoped<IIdentityProvider>(serviceProvider
			=> new AuthIdentityProvider(
				new Lazy<IRichWebApiUserContextAccessor>(serviceProvider
					.GetRequiredService<IRichWebApiUserContextAccessor>)));


		IssuerSigningKeyResolver issuerSigningKeyResolver
			= (_, _, kid, _) => keys
				.Where(x => x.Key == kid)
				.Select(x => x.Value);
		services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ClockSkew = TimeSpan.Zero,
					IssuerSigningKeyResolver =
						issuerSigningKeyResolver,
					ValidIssuer = bearerConfig.CurrentValue.Issuer,
					ValidAudience = bearerConfig.CurrentValue.Audience,
				};
				options.MapInboundClaims = false;
			});
		services.AddAuthorization();
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

			options.AddPolicy("global", builder => builder
				.WithOrigins("https://local.richwebapi.com", "https://local.richwebapi.com:7262",
					"https://localhost:7262")
				.AllowAnyHeader()
				.AllowCredentials()
				.AllowAnyMethod()
				.WithExposedHeaders());
		});

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
					.RequireProofKeyForCodeExchange();

				options.RegisterScopes(Scopes.OpenId, Scopes.Profile, Scopes.Email);

				options.UseAspNetCore()
					.EnableTokenEndpointPassthrough()
					.EnableAuthorizationEndpointPassthrough();

				options.AddSigningKeys(keys.Values);
				options.AddEncryptionKeys(keys.Values);

				options
					.SetJsonWebKeySetEndpointUris(".well-known/jwks.json")
					.SetTokenEndpointUris("auth/connect/token")
					.SetAuthorizationEndpointUris("auth/connect/authorize");
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

		var builder = services.AddAuthorizationBuilder();

		builder.AddDefaultPolicy("default", x =>
		{
			x.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
			x.RequireAuthenticatedUser();
		});

		services.AddStartupAction<OpenIddictInit>();

		return;

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