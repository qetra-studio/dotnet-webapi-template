using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RichWebApi.Authorization;
using RichWebApi.Config;
using RichWebApi.Entities.Identity;
using RichWebApi.Enums;
using RichWebApi.Extensions;
using RichWebApi.Services;
using RichWebApi.Services.Jwt;
using RichWebApi.Validation;

[assembly: InternalsVisibleTo("RichWebApi.Dependencies.Auth.Tests.Unit")]

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
		var signingKeys = UpdateSigningKeys(authConfig.CurrentValue);
		authConfig.OnChange(x => signingKeys = UpdateSigningKeys(x));

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
						(_, _, kid, _) => signingKeys.Where(x => x.Key == kid).Select(x => x.Value),
					ValidIssuer = bearerConfig.CurrentValue.Issuer,
					ValidAudience = bearerConfig.CurrentValue.Audience,
				};
			});

		services.AddScoped<IAuthorizationHandler, SecurityStampRequirement.Handler>();
		var builder = services.AddAuthorizationBuilder();

		builder.AddDefaultPolicy("default", x => x
			.RequireAuthenticatedUser()
			.RequireSecurityStamp());

		return;

		IReadOnlyDictionary<string, RsaSecurityKey> UpdateSigningKeys(AuthConfig cfg)
			=> cfg.RsaKeys.ToDictionary(x => x.Key, x =>
			{
				var rsa = RSA.Create();
				rsa.ImportRSAPublicKey(Convert.FromBase64String(x.Value.Public), out _);
				return new RsaSecurityKey(rsa);
			});
	}

	public void ConfigureApplication(IApplicationBuilder builder)
	{
		builder.UseAuthorization();
	}
}