using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using Xunit;

namespace BoardGames.Tests;

public class GameServiceTest : IClassFixture<TestAppHostFixture>
{
    private readonly TestAppHost _host;

    public GameServiceTest(TestAppHostFixture fixture)
        => _host = fixture.Host;

    private IServiceProvider ClientServices => _host.ClientServices;
    private ICommander ClientCommander => ClientServices.Commander();

    [Fact]
    public async Task CreateGame()
    {
        var session = await _host.SignIn("GameCreator");
        var games = ClientServices.GetRequiredService<IGameService>();

        var game = await ClientCommander.Call(new Game_Create(session, "gomoku"));
        Assert.NotNull(game);
        Assert.Equal("gomoku", game.EngineId);
        Assert.Equal(GameStage.New, game.Stage);
        Assert.Single(game.Players);

        // The game must be visible via the RPC client
        var readGame = await games.TryGet(game.Id);
        Assert.NotNull(readGame);
        Assert.Equal(game.Id, readGame.Id);

        // And it must be listed among the creator's own games
        var ownGames = await games.ListOwn(session, null, null, 10);
        Assert.Contains(ownGames, g => g.Id == game.Id);
    }

    [Fact]
    public async Task PlayGomoku()
    {
        var session1 = await _host.SignIn("GomokuPlayer1");
        var session2 = await _host.SignIn("GomokuPlayer2");
        var games = ClientServices.GetRequiredService<IGameService>();

        var game = await ClientCommander.Call(new Game_Create(session1, "gomoku"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var computed = await Computed.Capture(() => games.TryGet(game.Id));

        // Gomoku auto-starts once the 2nd player joins;
        // the client learns about it once the invalidation propagates over RPC.
        await ClientCommander.Call(new Game_Join(session2, game.Id));
        computed = await computed.When(g => g?.Stage == GameStage.Playing, cts.Token);
        var startedGame = computed.Value!;
        Assert.Equal(2, startedGame.Players.Count);

        // The player who moves first is random; player 0 is always the creator
        var engine = new GomokuEngine();
        var state = engine.DeserializeState(startedGame.StateJson);
        var currentPlayerSession = state.PlayerIndex == 0 ? session1 : session2;

        await ClientCommander.Call(new Game_Move(currentPlayerSession, game.Id, new MnkGameMove(9, 9)));
        computed = await computed.When(
            g => g != null && engine.DeserializeState(g.StateJson).MoveIndex == 1,
            cts.Token);
        var movedState = engine.DeserializeState(computed.Value!.StateJson);
        Assert.NotEqual(' ', movedState.Board[9, 9]);
    }

    [Fact]
    public async Task ListInvalidation()
    {
        var session = await _host.SignIn("ListWatcher");
        var games = ClientServices.GetRequiredService<IGameService>();

        // Capture the client-side computed for the own games list
        var computed = await Computed.Capture(() => games.ListOwn(session, null, null, 10));
        Assert.DoesNotContain(computed.Value, g => g.UserId == 0);
        var oldCount = computed.Value.Count;

        // Create a game & check the captured computed gets invalidated over RPC
        var game = await ClientCommander.Call(new Game_Create(session, "rps"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await computed.WhenInvalidated(cts.Token);

        computed = await computed.Update();
        Assert.Equal(oldCount + 1, computed.Value.Count);
        Assert.Contains(computed.Value, g => g.Id == game.Id);
    }

    [Fact]
    public async Task AnonymousUsersCannotCreateGames()
    {
        var session = Session.New(); // Not signed in
        await Assert.ThrowsAnyAsync<Exception>(
            () => ClientCommander.Call(new Game_Create(session, "gomoku")));
    }
}
