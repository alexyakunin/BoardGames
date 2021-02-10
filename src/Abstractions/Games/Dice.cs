using System;
using System.Drawing;
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

    public record DiceMove(int PlayerIndex, int PlayerScore, Moment Time = default) : GameMove(Time)
    {
        public DiceMove() : this(0, 0) {}
    }

    // public class DiceEngine : GameEngine<DiceState, DiceMove>
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
            var firstPlayerIndex = CoarseStopwatch.RandomInt32 & 1;
            var state = new DiceState(CharBoard.Empty(BoardSize), 0, firstPlayerIndex);
            var player = game.Players[state.PlayerIndex];
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MoveTurn(new GameUser(player.UserId)),
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
            var player = game.Players[state.PlayerIndex];
            var playerScore = move.PlayerScore;
            board = RemovePreviousMoves(board, move.PlayerIndex);
            if (playerScore >= BoardSize * BoardSize) {
                var newState = state with {Board = board, MoveIndex = state.MoveIndex + 1};
                var newGame = game with {StateJson = SerializeState(newState)};
                newGame = IncrementPlayerScore(newGame, move.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.Win(new GameUser(player.UserId)),
                    Stage = GameStage.Ended,
                };
                return newGame;
            }
            
            var rowAndCol = GetRowAndColValues(playerScore);

            var nextBoard = board.Set(rowAndCol.Item1, rowAndCol.Item2, GetPlayerMarker(move.PlayerIndex));
            var nextState = state with {
                Board = nextBoard,
                MoveIndex = state.MoveIndex + 1,
            };
            var nextPlayer = game.Players[nextState.PlayerIndex];
            var nextGame = game with {StateJson = SerializeState(nextState)};
            nextGame = nextGame with {
                StateMessage = StandardMessages.MoveTurn(new GameUser(nextPlayer.UserId)),
            };
            return nextGame;
        }

        private CharBoard RemovePreviousMoves(CharBoard board, int playerIndex)
        {
            var playerSign = GetPlayerMarker(playerIndex);
            var cells = board.Cells;
            var newCells = cells.Replace(playerSign, ' ');
            return new CharBoard(BoardSize, newCells);
        }

        private (int, int) GetRowAndColValues(long value)
        {
            var row = value / BoardSize;
            var col = value % BoardSize;
            return ((int)row, (int)col);
        }

        public char GetPlayerMarker(int playerIndex)
            => playerIndex == 0 ? 'X' : 'O';
    }
    
}