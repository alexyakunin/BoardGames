using System;
using System.Linq;
using Stl.Time;
using Stl.Time.Internal;

namespace BoardGames.Abstractions.Games
{
    public record DiceState(CharBoard Board, int MoveIndex = 0, int FirstPlayerIndex = 0)
    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
        public int NextPlayerIndex => (PlayerIndex + 1) % 2;
        
        public DiceState() : this((CharBoard) null!) { }
    }

    public record DiceMove(int PlayerIndex, int Row, int Column, Moment Time = default) : GameMove(Time)
    {
        public DiceMove() : this(0, 0, 0) { }
    }

    public class DiceEngine : GameEngine<DiceState, DiceMove>
    {
        public static int BoardSize { get; } = 8;
        public override string Id => "dice";
        public override string Title => "Dice";
        public override string Icon => "fa-dice-five";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 2;
        public override bool AutoStart => true;

        public override Game Start(Game game)
        {
            var firstPlayerIndex = CoarseStopwatch.RandomInt32 % 2;
            var state = new DiceState(CharBoard.Empty(BoardSize), 0, firstPlayerIndex);
            return game with {
                StateJson = SerializeState(state),
                StateMessage = GameMessages.MoveTurn(state.PlayerIndex),
            };
        }

        public override Game Move(Game game, DiceMove move)
        {
            if (game.Stage == GameStage.Ended)
                throw new ApplicationException("Game is ended.");
            var state = DeserializeState(game.StateJson);
            if (move.PlayerIndex != state.PlayerIndex)
                throw new ApplicationException("It's another player's turn.");
            var board = state.Board;
            if (board[move.Row, move.Column] != ' ')
                throw new ApplicationException("The cell is already occupied.");

            var nextBoard = board.Set(move.Row, move.Column, GetPlayerMarker(move.PlayerIndex));
            var nextState = state with {
                Board = nextBoard,
                MoveIndex = state.MoveIndex + 1,
            };
            var nextGame = game with {StateJson = SerializeState(nextState)};
            if (CheckGameEnded(nextBoard, move))
                nextGame = IncrementPlayerScore(nextGame, move.PlayerIndex, 1) with {
                    StateMessage = GameMessages.Win(move.PlayerIndex),
                    Stage = GameStage.Ended,
                };
            else {
                nextGame = nextGame with {
                    StateMessage = GameMessages.MoveTurn(nextState.PlayerIndex),
                };
            }
            return nextGame;
        }

        public char GetPlayerMarker(int playerIndex)
            => playerIndex == 0 ? 'X' : 'O';

        private bool CheckGameEnded(CharBoard board, DiceMove lastMove)
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

            bool IsGameEnded(int dr, int dc) =>
                SymmetricCount(dr, dc) >= 3;

            return IsGameEnded(0, 1) || IsGameEnded(1, 0) || IsGameEnded(1, 1) || IsGameEnded(-1, 1);
        }
    }
    
}