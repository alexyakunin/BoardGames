using MessagePack;

namespace BoardGames.Abstractions.Games;

public record ConnectFourState(CharBoard Board, int MoveIndex = 0, int FirstPlayerIndex = 0)
{
    public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
    public int NextPlayerIndex => (PlayerIndex + 1) % 2;
}

[MessagePackObject(true)]
public sealed partial record ConnectFourMove(int Column) : GameMove;

public class ConnectFourEngine : GameEngine<ConnectFourState, ConnectFourMove>
{
    public const int Width = 7;
    public const int Height = 6;
    public const int WinLength = 4;

    public override string Id => "connect4";
    public override string Title => "Connect Four";
    public override string Icon => "fa-arrow-circle-down";
    public override int MinPlayerCount => 2;
    public override int MaxPlayerCount => 2;
    public override bool AutoStart => true;

    public override Game Start(Game game)
    {
        var firstPlayerIndex = Random.Shared.Next() & 1;
        var state = new ConnectFourState(CharBoard.Empty(Width, Height), 0, firstPlayerIndex);
        var player = game.Players[state.PlayerIndex];
        return game with {
            StateJson = SerializeState(state),
            StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId)),
        };
    }

    public override Game Move(Game game, ConnectFourMove move)
    {
        if (game.Stage == GameStage.Ended)
            throw new ApplicationException("Game is ended.");
        var state = DeserializeState(game.StateJson);
        if (move.PlayerIndex != state.PlayerIndex)
            throw new ApplicationException("It's another player's turn.");
        var board = state.Board;
        if (move.Column < 0 || move.Column >= Width)
            throw new ApplicationException("There is no such column.");
        var row = GetDropRow(board, move.Column);
        if (row < 0)
            throw new ApplicationException("This column is full.");
        var player = game.Players[state.PlayerIndex];

        var nextBoard = board.Set(row, move.Column, GetPlayerMarker(move.PlayerIndex));
        var nextState = state with {
            Board = nextBoard,
            MoveIndex = state.MoveIndex + 1,
        };
        var nextPlayer = game.Players[nextState.PlayerIndex];
        var nextGame = game with { StateJson = SerializeState(nextState) };

        if (IsWin(nextBoard, row, move.Column))
            return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                StateMessage = StandardMessages.Win(new AppUser(player.UserId)),
                Stage = GameStage.Ended,
            };
        if (nextBoard.IsFull)
            return nextGame with {
                StateMessage = StandardMessages.Draw(),
                Stage = GameStage.Ended,
            };
        return nextGame with {
            StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
        };
    }

    public char GetPlayerMarker(int playerIndex)
        => playerIndex == 0 ? 'X' : 'O';

    // The row a disc dropped into this column lands on, or -1 if the column is full
    public static int GetDropRow(CharBoard board, int column)
    {
        for (var r = Height - 1; r >= 0; r--) {
            if (board[r, column] == ' ')
                return r;
        }
        return -1;
    }

    private static bool IsWin(CharBoard board, int row, int column)
    {
        var marker = board[row, column];
        int Count(int dr, int dc)
            => Enumerable.Range(0, WinLength)
                .Select(i => board[row + dr * i, column + dc * i])
                .TakeWhile(c => c == marker)
                .Take(WinLength)
                .Count();
        int SymmetricCount(int dr, int dc)
            => Count(dr, dc) + Count(-dr, -dc) - 1;
        bool IsWinLine(int dr, int dc)
            => SymmetricCount(dr, dc) >= WinLength;

        return IsWinLine(0, 1) || IsWinLine(1, 0) || IsWinLine(1, 1) || IsWinLine(-1, 1);
    }
}
