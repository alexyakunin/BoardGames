using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Stl.DependencyInjection;
using Stl.Time.Internal;

namespace BoardGames.Abstractions.Games
{
    public enum TicEnd
    {
        None = 0,
        Win,
        Draw
    }

    public record TicState(CharBoard Board, int MoveIndex = 0, int FirstPlayerIndex = 0)
    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
        public TicState() : this((CharBoard) null!) { }
    }

    public record TicMove(int Row, int Column) : GameMove
    {
        public TicMove() : this(0, 0) {}
    }

    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class TicEngine : GameEngine<TicState, TicMove>
    {
        public static int BoardSize { get; } = 3;
        public override string Id => "tictactoe";
        public override string Title => "Tic Tac Toe";
        public override string Icon => "fa-ellipsis-h";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 2;
        public override bool AutoStart => true;

        public override Game Create(Game game)
            => game with {RoundCount = 7};

        public override Game Start(Game game)
        {
            var firstPlayerIndex = CoarseStopwatch.RandomInt32 & 1;
            var state = new TicState(CharBoard.Empty(BoardSize), 0, firstPlayerIndex);
            var player = game.Players[state.PlayerIndex];
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId))
            };
        }

        public override Game Move(Game game, TicMove move)
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

            var gameEnd = GetGameEnd(nextBoard, move);
            switch (gameEnd) {
                case TicEnd.None:
                    return nextGame with {
                        StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
                    };
                case TicEnd.Win:
                    return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                        StateMessage = StandardMessages.Win(new AppUser(player.UserId)),
                        Stage = GameStage.Ended,
                    };
                case TicEnd.Draw:
                    return IncrementPlayerScore(nextGame, state.PlayerIndex, 1) with {
                        StateMessage = StandardMessages.Draw(),
                        Stage = GameStage.Ended,
                    };
                default:
                    throw new ArgumentException();
            }
        }

        public char GetPlayerMarker(int playerIndex)
            => playerIndex == 0 ? 'X' : 'O';

        private TicEnd GetGameEnd(CharBoard board, TicMove lastMove)
        {
            var marker = GetPlayerMarker(lastMove.PlayerIndex);

            int Count(int dr, int dc)
                => Enumerable.Range(0, 3)
                    .Select(i => board[lastMove.Row + dr * i, lastMove.Column + dc * i])
                    .TakeWhile(c => c == marker)
                    .Take(3)
                    .Count();

            int SymmetricCount(int dr, int dc)
                => Count(dr, dc) + Count(-dr, -dc) - 1;

            bool IsWin(int dr, int dc)
                => SymmetricCount(dr, dc) >= 3;

            if (IsWin(0, 1) || IsWin(1, 0) || IsWin(1, 1) || IsWin(-1, 1))
                return TicEnd.Win;
            if (board.Cells.All(c => c != ' '))
                return TicEnd.Draw;
            return TicEnd.None;
        }
    }
}