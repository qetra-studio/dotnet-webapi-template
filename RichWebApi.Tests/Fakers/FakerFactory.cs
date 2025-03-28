namespace RichWebApi.Tests.Fakers;

internal class FakerFactory(IServiceProvider serviceProvider) : IFakerFactory
{
	public IServiceProvider ServiceProvider { get; } = serviceProvider;
};