using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using RichWebApi.Tests.Fakers;

namespace RichWebApi.Tests.DependencyInjection;

[UsedImplicitly]
public abstract class DependencyContainerFixture : IDisposable
{
	private readonly IServiceCollection _services = new ServiceCollection();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public DependencyContainerFixture ConfigureServices(Action<IServiceCollection> configure)
	{
		configure.Invoke(_services);
		return this;
	}

	public IServiceProvider BuildServiceProvider()
		=> ConfigureSharedServices(_services)
			.AddSingleton<IFakerFactory, FakerFactory>()
			.BuildServiceProvider();

	public void Dispose() => _services.Clear();

	protected virtual IServiceCollection ConfigureSharedServices(IServiceCollection services)
		=> services;
}