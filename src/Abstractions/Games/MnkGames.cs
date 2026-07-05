using MessagePack;

namespace BoardGames.Abstractions.Games;

public enum MnkGameEndingKind
{
    None = 0,
    Win,
    Draw,
}

public record MnkGameState(CharBoard Board, int MoveIndex = 0, int FirstPlayerIndex = 0)
{
    public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
    public int NextPlayerIndex => (PlayerIndex + 1) % 2;
}

[MessagePackObject(true)]
public sealed partial record MnkGameMove(int Row, int Column) : GameMove;

/// <summary>
/// The base engine for m,n,k-games (https://en.wikipedia.org/wiki/M,n,k-game):
/// two players alternately place markers on a NxN board, the first one to get
/// K markers in a row (horizontally, vertically, or diagonally) wins.
/// </summary>
public abstract class MnkGameEngine : GameEngine<MnkGameState, MnkGameMove>
{
    public abstract int BoardSize { get; }
    public abstract int WinLength { get; }
    public override int MinPlayerCount => 2;
    public override int MaxPlayerCount => 2;
    public override bool AutoStart => true;

    public override Game Start(Game game)
    {
        var firstPlayerIndex = Random.Shared.Next() & 1;
        var state = new MnkGameState(CharBoard.Empty(BoardSize), 0, firstPlayerIndex);
        var player = game.Players[state.PlayerIndex];
        return game with {
            StateJson = SerializeState(state),
            StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId)),
        };
    }

    public override Game Move(Game game, MnkGameMove move)
    {
        if (game.Stage == GameStage.Ended)
            throw new ApplicationException("Game is ended.");
        var state = DeserializeState(game.StateJson);
        if (move.PlayerIndex != state.PlayerIndex)
            throw new ApplicationException("It's another player's turn.");
        var board = state.Board;
        if (board[move.Row, move.Column] != ' ')
            throw new ApplicationException("The cell is already occupied.");
        var player = game.Players[state.PlayerIndex];

        var nextBoard = board.Set(move.Row, move.Column, GetPlayerMarker(move.PlayerIndex));
        var nextState = state with {
            Board = nextBoard,
            MoveIndex = state.MoveIndex + 1,
        };
        var nextPlayer = game.Players[nextState.PlayerIndex];
        var nextGame = game with { StateJson = SerializeState(nextState) };

        var gameEndingKind = GetGameEnding(nextBoard, move);
        switch (gameEndingKind) {
        case MnkGameEndingKind.None:
            return nextGame with {
                StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
            };
        case MnkGameEndingKind.Win:
            return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                StateMessage = StandardMessages.Win(new AppUser(player.UserId)),
                Stage = GameStage.Ended,
            };
        case MnkGameEndingKind.Draw:
            return nextGame with {
                StateMessage = StandardMessages.Draw(),
                Stage = GameStage.Ended,
            };
        default:
            throw new ArgumentOutOfRangeException();
        }
    }

    public char GetPlayerMarker(int playerIndex)
        => playerIndex == 0 ? 'X' : 'O';

    private MnkGameEndingKind GetGameEnding(CharBoard board, MnkGameMove lastMove)
    {
        var marker = GetPlayerMarker(lastMove.PlayerIndex);
        int Count(int dr, int dc)
            => Enumerable.Range(0, WinLength)
                .Select(i => board[lastMove.Row + dr * i, lastMove.Column + dc * i])
                .TakeWhile(c => c == marker)
                .Take(WinLength)
                .Count();
        int SymmetricCount(int dr, int dc)
            => Count(dr, dc) + Count(-dr, -dc) - 1;
        bool IsWin(int dr, int dc)
            => SymmetricCount(dr, dc) >= WinLength;

        if (IsWin(0, 1) || IsWin(1, 0) || IsWin(1, 1) || IsWin(-1, 1))
            return MnkGameEndingKind.Win;
        if (board.IsFull)
            return MnkGameEndingKind.Draw;
        return MnkGameEndingKind.None;
    }
}

public class GomokuEngine : MnkGameEngine
{
    public override string Id => "gomoku";
    public override string Title => "Gomoku (Five in a Row)";
    public override string Icon => "fa-border-all";
    public override int BoardSize => 19;
    public override int WinLength => 5;
}

public class TicTacToeEngine : MnkGameEngine
{
    public override string Id => "tictactoe";
    public override string Title => "Tic-Tac-Toe";
    public override string Icon => "fa-hashtag";
    public override int BoardSize => 3;
    public override int WinLength => 3;
}
