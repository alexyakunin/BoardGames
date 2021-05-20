using System;
using System.Linq;
using Stl.DependencyInjection;
using Stl.Time.Internal;

namespace BoardGames.Abstractions.Games
{
    public enum GomokuGameEndingKind
    {
        None = 0,
        Win,
        Draw,
    }

    public record GomokuState(CharBoard Board, int MoveIndex = 0, int FirstPlayerIndex = 0)
    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
        public int NextPlayerIndex => (PlayerIndex + 1) % 2;

        public GomokuState() : this((CharBoard) null!) { }
    }

    public record GomokuMove(int Row, int Column) : GameMove
    {
        public GomokuMove() : this(0, 0) { }
    }

    [RegisterService]
    [RegisterAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class GomokuEngine : GameEngine<GomokuState, GomokuMove>
    {
        public static int BoardSize { get; } = 19;

        public override string Id => "gomoku";
        public override string Title => "Gomoku (Five in a Row)";
        public override string Icon => "fa-border-all";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 2;
        public override bool AutoStart => true;

        public override Game Start(Game game)
        {
            var firstPlayerIndex = CoarseStopwatch.RandomInt32 & 1;
            var state = new GomokuState(CharBoard.Empty(BoardSize), 0, firstPlayerIndex);
            var player = game.Players[state.PlayerIndex];
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId)),
            };
        }

        public override Game Move(Game game, GomokuMove move)
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
            case GomokuGameEndingKind.None:
                return nextGame with {
                    StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
                };
            case GomokuGameEndingKind.Win:
                return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.Win(new AppUser(player.UserId)),
                    Stage = GameStage.Ended,
                };
            case GomokuGameEndingKind.Draw:
                return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.Draw(),
                    Stage = GameStage.Ended,
                };
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public char GetPlayerMarker(int playerIndex)
            => playerIndex == 0 ? 'X' : 'O';

        private GomokuGameEndingKind GetGameEnding(CharBoard board, GomokuMove lastMove)
        {
            var marker = GetPlayerMarker(lastMove.PlayerIndex);
            int Count(int dr, int dc)
                => Enumerable.Range(0, 5)
                    .Select(i => board[lastMove.Row + dr * i, lastMove.Column + dc * i])
                    .TakeWhile(c => c == marker)
                    .Take(5)
                    .Count();
            int SymmetricCount(int dr, int dc)
                => Count(dr, dc) + Count(-dr, -dc) - 1;
            bool IsWin(int dr, int dc)
                => SymmetricCount(dr, dc) >= 5;

            if (IsWin(0, 1) || IsWin(1, 0) || IsWin(1, 1) || IsWin(-1, 1))
                return GomokuGameEndingKind.Win;
            if (board.Cells.All(c => c != ' '))
                return GomokuGameEndingKind.Draw;
            return GomokuGameEndingKind.None;
        }
    }
}
