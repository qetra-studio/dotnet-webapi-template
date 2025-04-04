using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using OpenIddict.Server;
using RichWebApi.Authorization;
using RichWebApi.Config;
using RichWebApi.Dependencies;
using RichWebApi.Entities.Identity;
using RichWebApi.Entities.OpenIddict;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Parts;
using RichWebApi.Services;
using RichWebApi.Services.Jwt;
using RichWebApi.Startup;
using RichWebApi.Validation;
using Riok.Mapperly.Abstractions;

[assembly: InternalsVisibleTo("RichWebApi.Dependencies.Auth.Tests.Unit")]
[assembly: MapperDefaults(ThrowOnMappingNullMismatch = true, ThrowOnPropertyMappingNullMismatch = true)]

namespace RichWebApi;

internal class AuthDependency : IAppDependency
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

		services.AddIdentity<RichWebApiUser, RichWebApiRole>(options => { options.User.RequireUniqueEmail = true; })
			.AddEntityFrameworkStores<RichWebApiDbContext>()
			.AddDefaultTokenProviders();
		services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
		services.TryAddScoped<IRichWebApiUserContextAccessor, RichWebApiUserContextAccessor>();
		services.TryAddScoped<IIdentityProvider>(serviceProvider
			=> new AuthIdentityProvider(
				new Lazy<IRichWebApiUserContextAccessor>(serviceProvider
					.GetRequiredService<IRichWebApiUserContextAccessor>)));

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

		services.AddAuthentication(x =>
			{
				x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			})
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
						(_, _, kid, _) => keys.Where(x => x.Key == kid).Select(x => x.Value.Public),
					ValidIssuer = bearerConfig.CurrentValue.Issuer,
					ValidAudience = bearerConfig.CurrentValue.Audience,
				};
			});
		services.AddAuthorization();

		services.AddOpenIddict()
			.AddCore(options =>
			{
				options.UseEntityFrameworkCore().UseDbContext<RichWebApiDbContext>()
					.ReplaceDefaultEntities<RichWebApiOpenApplication, RichWebApiOpenAuthorization, RichWebApiOpenScope,
						RichWebApiOpenToken, Guid>();
			})
			.AddServer(options =>
			{
				options.UseAspNetCore()
					.EnableTokenEndpointPassthrough();

				options.AddSigningKeys(keys.Values.Select(x => x.Public));
				options.AddEncryptionKeys(keys.Values.Select(x => x.Private));

				options.SetJsonWebKeySetEndpointUris(".well-known/jwks.json");
				options.SetTokenEndpointUris("auth/connect/token");
				options.AllowClientCredentialsFlow();
			});



		services.AddScoped<IAuthorizationHandler, SecurityStampRequirement.Handler>();
		var builder = services.AddAuthorizationBuilder();

		builder.AddDefaultPolicy("default", x => x
			.RequireAuthenticatedUser()
			.RequireSecurityStamp());

		services.AddStartupAction<OpenIddictInit>();

		return;

		IReadOnlyDictionary<string, RsaKeyPair> UpdateKeys(AuthConfig cfg)
			=> cfg.RsaKeys.ToDictionary(x => x.Key, x =>
			{
				var publicRsa = RSA.Create();
				publicRsa.ImportRSAPrivateKey(Convert.FromBase64String(x.Value.Private), out _);
				var privateRsa = RSA.Create();
				privateRsa.ImportRSAPrivateKey(Convert.FromBase64String(x.Value.Private), out _);
				return new RsaKeyPair(new RsaSecurityKey(publicRsa), new RsaSecurityKey(privateRsa));
			});
	}

	private record RsaKeyPair(RsaSecurityKey Public, RsaSecurityKey Private);

	public void ConfigureApplication(IApplicationBuilder builder)
	{
		builder.UseAuthentication();
		builder.UseAuthorization();
	}
}