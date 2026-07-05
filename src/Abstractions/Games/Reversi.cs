using MessagePack;

namespace BoardGames.Abstractions.Games;

public record ReversiState(CharBoard Board, int PlayerIndex = 0, int FirstPlayerIndex = 0);

[MessagePackObject(true)]
public sealed partial record ReversiMove(int Row, int Column) : GameMove;

public class ReversiEngine : GameEngine<ReversiState, ReversiMove>
{
    public const int BoardSize = 8;

    private static readonly (int Dr, int Dc)[] Directions = [
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1), (0, 1),
        (1, -1), (1, 0), (1, 1),
    ];

    public override string Id => "reversi";
    public override string Title => "Reversi (Othello)";
    public override string Icon => "fa-adjust";
    public override int MinPlayerCount => 2;
    public override int MaxPlayerCount => 2;
    public override bool AutoStart => true;

    public override Game Start(Game game)
    {
        var firstPlayerIndex = Random.Shared.Next() & 1;
        var firstMarker = GetPlayerMarker(firstPlayerIndex);
        var secondMarker = GetPlayerMarker(1 - firstPlayerIndex);
        var board = CharBoard.Empty(BoardSize)
            .Set(3, 3, secondMarker).Set(4, 4, secondMarker)
            .Set(3, 4, firstMarker).Set(4, 3, firstMarker);
        var state = new ReversiState(board, firstPlayerIndex, firstPlayerIndex);
        var player = game.Players[state.PlayerIndex];
        game = SetDiscCountScores(game, board);
        return game with {
            StateJson = SerializeState(state),
            StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId)),
        };
    }

    public override Game Move(Game game, ReversiMove move)
    {
        if (game.Stage == GameStage.Ended)
            throw new ApplicationException("Game is ended.");
        var state = DeserializeState(game.StateJson);
        if (move.PlayerIndex != state.PlayerIndex)
            throw new ApplicationException("It's another player's turn.");
        var board = state.Board;
        var marker = GetPlayerMarker(state.PlayerIndex);

        var flips = GetFlips(board, move.Row, move.Column, marker);
        if (flips.Count == 0)
            throw new ApplicationException("This move doesn't flip any of your opponent's discs.");

        var nextBoard = board.Set(move.Row, move.Column, marker);
        foreach (var (r, c) in flips)
            nextBoard = nextBoard.Set(r, c, marker);
        var nextGame = SetDiscCountScores(game, nextBoard);

        // The turn passes to the opponent unless they have no legal moves;
        // in that case the same player moves again; if neither can move, the game ends.
        var opponentMarker = GetPlayerMarker(1 - state.PlayerIndex);
        int nextPlayerIndex;
        if (HasAnyMove(nextBoard, opponentMarker))
            nextPlayerIndex = 1 - state.PlayerIndex;
        else if (HasAnyMove(nextBoard, marker))
            nextPlayerIndex = state.PlayerIndex;
        else {
            var nextState1 = state with { Board = nextBoard };
            return nextGame with {
                StateJson = SerializeState(nextState1),
                StateMessage = GetEndingMessage(nextGame, nextBoard),
                Stage = GameStage.Ended,
            };
        }

        var nextState = state with { Board = nextBoard, PlayerIndex = nextPlayerIndex };
        var nextPlayer = game.Players[nextPlayerIndex];
        return nextGame with {
            StateJson = SerializeState(nextState),
            StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
        };
    }

    public char GetPlayerMarker(int playerIndex)
        => playerIndex == 0 ? 'X' : 'O';

    // Opponent's discs flipped by placing the marker at (row, column), if any
    public static List<(int Row, int Column)> GetFlips(CharBoard board, int row, int column, char marker)
    {
        var flips = new List<(int, int)>();
        if (board[row, column] != ' ' || row < 0 || row >= BoardSize || column < 0 || column >= BoardSize)
            return flips;
        foreach (var (dr, dc) in Directions) {
            var lineFlips = new List<(int, int)>();
            var (r, c) = (row + dr, column + dc);
            while (board[r, c] != ' ' && board[r, c] != marker) {
                lineFlips.Add((r, c));
                (r, c) = (r + dr, c + dc);
            }
            if (board[r, c] == marker)
                flips.AddRange(lineFlips);
        }
        return flips;
    }

    public static bool IsLegalMove(CharBoard board, int row, int column, char marker)
        => GetFlips(board, row, column, marker).Count != 0;

    public static bool HasAnyMove(CharBoard board, char marker)
    {
        for (var r = 0; r < BoardSize; r++)
        for (var c = 0; c < BoardSize; c++)
            if (IsLegalMove(board, r, c, marker))
                return true;
        return false;
    }

    private Game SetDiscCountScores(Game game, CharBoard board)
    {
        game = SetPlayerScore(game, 0, board.Count(GetPlayerMarker(0)));
        game = SetPlayerScore(game, 1, board.Count(GetPlayerMarker(1)));
        return game;
    }

    private string GetEndingMessage(Game game, CharBoard board)
    {
        var count0 = board.Count(GetPlayerMarker(0));
        var count1 = board.Count(GetPlayerMarker(1));
        if (count0 == count1)
            return StandardMessages.Draw();
        var winner = game.Players[count0 > count1 ? 0 : 1];
        return StandardMessages.Win(new AppUser(winner.UserId));
    }
}
