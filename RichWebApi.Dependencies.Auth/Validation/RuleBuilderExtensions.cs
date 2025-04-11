using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using RichWebApi.Entities.Identity;
using static RichWebApi.Utilities.AuthRegex;

namespace RichWebApi.Validation;

public static class RuleBuilderExtensions
{
	public static IRuleBuilderOptionsConditions<T, T> HasOpenIddictServerRequest<T>(
		this IRuleBuilder<T, T> ruleBuilder, IHttpContextAccessor accessor, Func<OpenIddictRequest, bool>? predicate = null)
		where T : class
		=> ruleBuilder
			.HasHttpContext(accessor)
			.Custom((_, ctx) =>
			{
				if (accessor.HttpContext is null)
				{
					return;
				}
				
				var request = accessor.HttpContext.GetOpenIddictServerRequest();
				if (request is null)
				{
					ctx.AddFailure(ctx.PropertyPath, "OpenIddictServerRequest is missing");
					return;
				}

				if (predicate is not null && !predicate(request))
				{
					ctx.AddFailure(ctx.PropertyPath, "OpenIddictServerRequest didn't pass predicate validation");
				}
			});
	
	public static IRuleBuilderOptionsConditions<T, T> HasHttpContext<T>(
		this IRuleBuilder<T, T> ruleBuilder, IHttpContextAccessor accessor)
		where T : class
		=> ruleBuilder
			.Custom((_, ctx) =>
			{
				if (accessor.HttpContext is null)
				{
					ctx.AddFailure(ctx.PropertyPath, "HttpContext is missing");
				}
			});
	
	public static IRuleBuilderOptionsConditions<T, T> HasAuthenticatedUser<T>(
		this IRuleBuilder<T, T> ruleBuilder, IHttpContextAccessor accessor)
		where T : class
		=> ruleBuilder
			.HasHttpContext(accessor)
			.Custom((_, ctx) =>
			{
				if (accessor.HttpContext is null)
				{
					return;
				}

				var identity = accessor.HttpContext.User.Identity; 
				if (identity is null)
				{
					ctx.AddFailure(ctx.PropertyPath, "User identity is missing");
					return;
				}

				if (!identity.IsAuthenticated)
				{
					ctx.AddFailure(ctx.PropertyPath, "User identity is not authenticated");
				}
			});
	
	
	public static IRuleBuilderOptionsConditions<T, string> RichWebApiEmail<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		UserManager<RichWebApiUser> manager)
		=> ruleBuilder
			.MaximumLength(256)
			.EmailAddress()
			.CustomAsync(async (v, ctx, _) =>
			{
				if (await manager.FindByEmailAsync(v) is not null)
				{
					ctx.AddFailure(new ValidationFailure
					{
						Severity = Severity.Error,
						AttemptedValue = v,
						ErrorCode = "emailTaken",
						PropertyName = ctx.PropertyPath,
						ErrorMessage = "Email is taken."
					});
				}
			});

	public static IRuleBuilderOptionsConditions<T, string> RichWebApiUsername<T>(
		this IRuleBuilder<T, string> ruleBuilder)
		=> ruleBuilder
			.MaximumLength(256)
			.Custom((v, ctx) =>
			{
				if (!UserNameRegex.IsMatch(v))
				{
					ctx.AddFailure(new ValidationFailure
					{
						Severity = Severity.Error,
						AttemptedValue = v,
						ErrorCode = "invalidUserName",
						PropertyName = ctx.PropertyPath,
						ErrorMessage = "Username contains invalid characters."
					});
				}
			});
	public static IRuleBuilderOptionsConditions<T, string> RichWebApiUniqueUsername<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		UserManager<RichWebApiUser> manager)
		=> ruleBuilder
			.RichWebApiUsername()
			.CustomAsync(async (v, ctx, _) =>
			{
				if (await manager.FindByNameAsync(v) is not null)
				{
					ctx.AddFailure(new ValidationFailure
					{
						Severity = Severity.Error,
						AttemptedValue = v,
						ErrorCode = "userNameTaken",
						PropertyName = ctx.PropertyPath,
						ErrorMessage = "Username is taken."
					});
				}
			});

	public static IRuleBuilderOptionsConditions<T, string> RichWebApiPassword<T>(
		this IRuleBuilder<T, string> ruleBuilder,
		UserManager<RichWebApiUser> manager)
		=> ruleBuilder
			.MaximumLength(100)
			.CustomAsync(async (v, ctx, _) =>
			{
				var sampleUser = new RichWebApiUser();
				foreach (var pv in manager.PasswordValidators)
				{
					var result = await pv.ValidateAsync(manager, sampleUser, v);
					if (result.Succeeded)
					{
						continue;
					}

					foreach (var e in result.Errors)
					{
						ctx.AddFailure(new ValidationFailure
						{
							Severity = Severity.Error,
							PropertyName = ctx.PropertyPath,
							ErrorCode = e.Code,
							ErrorMessage = e.Description
						});
					}
				}
			});
}