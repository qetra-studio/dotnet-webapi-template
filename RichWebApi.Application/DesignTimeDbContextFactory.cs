using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.FileProviders;
using RichWebApi.Dependencies;
using RichWebApi.Parts;
using RichWebApi.Services;

namespace RichWebApi;

[UsedImplicitly]
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RichWebApiDbContext>
{
	private sealed class DummyEnvironment : IWebHostEnvironment
	{
		public string ApplicationName { get; set; } = null!;
		public IFileProvider ContentRootFileProvider { get; set; } = null!;
		public string ContentRootPath { get; set; } = null!;
		public string EnvironmentName { get; set; } = Environments.Development;
		public string WebRootPath { get; set; } = null!;
		public IFileProvider WebRootFileProvider { get; set; } = null!;
	}

	private sealed class DummyIdentityProvider : IIdentityProvider
	{
		public Guid? UserId { get; }
	}

	public RichWebApiDbContext CreateDbContext(string[] args)
	{
		var configurationRoot = new ConfigurationBuilder()
			.AddJsonFile("appsettings.Development.json")
			.AddUserSecrets<Program>()
			.AddEnvironmentVariables()
			.Build();
		var env = new DummyEnvironment();
		var parts = Program.EnrichWithApplicationParts(new AppPartsCollection());
		var dependencies = new AppDependenciesCollection()
			.AddDatabase(env);
		var provider = new ServiceCollection()
			.AddLogging(x =>
			{
				x.SetMinimumLevel(LogLevel.Information);
				x.AddConsole();
			})
			.AddCore()
			.AddSingleton<IConfiguration>(configurationRoot)
			.AddSingleton(configurationRoot)
			.AddSingleton<IWebHostEnvironment>(env)
			.AddSingleton<IIdentityProvider, DummyIdentityProvider>()
			.AddAppParts(parts)
			.AddDependencyServices(dependencies, parts)
			.BuildServiceProvider();
		return provider.GetRequiredService<RichWebApiDbContext>();
	}
}