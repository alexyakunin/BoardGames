using BoardGames.Abstractions;
using BoardGames.Abstractions.Games;
using Xunit;

namespace BoardGames.Tests;

// Pure unit tests for the game engines - no host required, since engines
// are deterministic functions over the Game model.
public class GameEngineTest
{
    private static Game NewGame(IGameEngine engine)
    {
        var game = new Game() {
            Id = "test",
            EngineId = engine.Id,
            UserId = 1,
            Stage = GameStage.Playing,
            Players = ImmutableList<GamePlayer>.Empty
                .Add(new GamePlayer(1))
                .Add(new GamePlayer(2)),
        };
        return engine.Start(engine.Create(game));
    }

    private static Game Move(IGameEngine engine, Game game, GameMove move, int playerIndex)
        => engine.Move(game, move with { PlayerIndex = playerIndex });

    // Tic-Tac-Toe (m,n,k-games)

    [Fact]
    public void TicTacToeWin()
    {
        var engine = new TicTacToeEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex; // Random first player
        var o = 1 - p;

        // p: (0,0) (0,1) (0,2) - the top row; o: (1,0) (1,1)
        game = Move(engine, game, new MnkGameMove(0, 0), p);
        game = Move(engine, game, new MnkGameMove(1, 0), o);
        game = Move(engine, game, new MnkGameMove(0, 1), p);
        game = Move(engine, game, new MnkGameMove(1, 1), o);
        game = Move(engine, game, new MnkGameMove(0, 2), p);

        Assert.Equal(GameStage.Ended, game.Stage);
        Assert.Equal(1, game.Players[p].Score);
        Assert.Equal(0, game.Players[o].Score);
    }

    [Fact]
    public void TicTacToeRejectsWrongTurnAndOccupiedCells()
    {
        var engine = new TicTacToeEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;
        var o = 1 - p;

        Assert.Throws<ApplicationException>(() => Move(engine, game, new MnkGameMove(0, 0), o));
        game = Move(engine, game, new MnkGameMove(0, 0), p);
        Assert.Throws<ApplicationException>(() => Move(engine, game, new MnkGameMove(0, 0), o));
    }

    // Connect Four

    [Fact]
    public void ConnectFourDiscsFallDown()
    {
        var engine = new ConnectFourEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;

        game = Move(engine, game, new ConnectFourMove(3), p);
        var board = engine.DeserializeState(game.StateJson).Board;
        Assert.Equal(engine.GetPlayerMarker(p), board[ConnectFourEngine.Height - 1, 3]);
        Assert.Equal(' ', board[ConnectFourEngine.Height - 2, 3]);
    }

    [Fact]
    public void ConnectFourVerticalWin()
    {
        var engine = new ConnectFourEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;
        var o = 1 - p;

        // p stacks column 0, o stacks column 6
        for (var i = 0; i < 3; i++) {
            game = Move(engine, game, new ConnectFourMove(0), p);
            game = Move(engine, game, new ConnectFourMove(6), o);
        }
        game = Move(engine, game, new ConnectFourMove(0), p);

        Assert.Equal(GameStage.Ended, game.Stage);
        Assert.Equal(1, game.Players[p].Score);
    }

    [Fact]
    public void ConnectFourRejectsFullColumn()
    {
        var engine = new ConnectFourEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;
        var o = 1 - p;

        // Fill column 0 (alternating, 6 discs)
        for (var i = 0; i < 3; i++) {
            game = Move(engine, game, new ConnectFourMove(0), p);
            game = Move(engine, game, new ConnectFourMove(0), o);
        }
        Assert.Throws<ApplicationException>(() => Move(engine, game, new ConnectFourMove(0), p));
    }

    // Reversi

    [Fact]
    public void ReversiFirstMoveFlipsADisc()
    {
        var engine = new ReversiEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;
        var marker = engine.GetPlayerMarker(p);

        // Both players start with 2 discs
        Assert.Equal(2, game.Players[0].Score);
        Assert.Equal(2, game.Players[1].Score);

        // Find any legal move & make it: it must flip exactly one disc on move 1,
        // so the mover ends up with 4 discs and the opponent with 1
        var board = state.Board;
        var legalMove = (
            from r in Enumerable.Range(0, ReversiEngine.BoardSize)
            from c in Enumerable.Range(0, ReversiEngine.BoardSize)
            where ReversiEngine.IsLegalMove(board, r, c, marker)
            select new ReversiMove(r, c)).First();
        game = Move(engine, game, legalMove, p);

        Assert.Equal(4, game.Players[p].Score);
        Assert.Equal(1, game.Players[1 - p].Score);

        // And the turn passes to the opponent
        var nextState = engine.DeserializeState(game.StateJson);
        Assert.Equal(1 - p, nextState.PlayerIndex);
    }

    [Fact]
    public void ReversiRejectsNonFlippingMoves()
    {
        var engine = new ReversiEngine();
        var game = NewGame(engine);
        var state = engine.DeserializeState(game.StateJson);
        var p = state.PlayerIndex;

        // A corner cell can't flip anything on move 1
        Assert.Throws<ApplicationException>(() => Move(engine, game, new ReversiMove(0, 0), p));
    }
}
