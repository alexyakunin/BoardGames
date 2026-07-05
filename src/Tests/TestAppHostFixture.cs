using Xunit;

namespace BoardGames.Tests;

public class TestAppHostFixture : IAsyncLifetime
{
    public TestAppHost Host { get; private set; } = null!;

    public async Task InitializeAsync()
        => Host = await TestAppHost.Start();

    public async Task DisposeAsync()
        => await Host.DisposeAsync();
}
