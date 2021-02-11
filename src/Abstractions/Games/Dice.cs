using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Stl.Fusion;
using Stl.Time;
using Stl.Time.Internal;

namespace BoardGames.Abstractions.Games
{
    public record DiceState(DiceBoard Board, Dictionary<int, int> Scores, int MoveIndex = 0, int FirstPlayerIndex = 0)

    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % 2;
        public int NextPlayerIndex => (PlayerIndex + 1) % 2;
        public DiceState() : this((DiceBoard) null!, (Dictionary<int, int>) null!) { }
    }

    public record DiceMove(int PlayerIndex, int Value, Moment Time = default) : GameMove(Time)
    {
        public DiceMove() : this(0, 0) {}
    }

    public class DiceEngine : GameEngine<DiceState, DiceMove>
    {
        public static int BoardSize { get; } = 8;
        public override string Id => "dice";
        public override string Title => "Dice";
        public override string Icon => "fa-dice-five";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 4;
        public override bool AutoStart => true;

        public override Game Start(Game game)
        {
            var scores = new Dictionary<int, int>() {
                {0, -1},
                {1, -1},
                {2, -1},
                {3, -1},
            };
            var firstPlayerIndex = CoarseStopwatch.RandomInt32 & 1;
            var state = new DiceState(DiceBoard.Empty(BoardSize), scores,0, firstPlayerIndex);
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
            state.Scores[state.PlayerIndex] += move.Value;
            var playerScore = state.Scores[state.PlayerIndex];
            var scores = state.Scores;
            board = RemovePreviousMoves(board, move.PlayerIndex);
            if (playerScore >= (BoardSize * BoardSize) - 1) {
                var newState = state with {Board = board, MoveIndex = state.MoveIndex + 1, Scores = scores};
                var newGame = game with {StateJson = SerializeState(newState)};
                newGame = IncrementPlayerScore(newGame, move.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.Win(new GameUser(player.UserId)),
                    Stage = GameStage.Ended,
                };
                return newGame;
            }
            
            var rowAndCol = GetRowAndColValues(playerScore);

            var nextBoard = board.Set(rowAndCol.Item1, rowAndCol.Item2, state.PlayerIndex, DicePiece[move.PlayerIndex]);
            var nextState = state with {
                Board = nextBoard,
                MoveIndex = state.MoveIndex + 1,
                Scores = scores,
            };
            var nextPlayer = game.Players[nextState.PlayerIndex];
            var nextGame = game with {StateJson = SerializeState(nextState)};
            nextGame = nextGame with {
                StateMessage = StandardMessages.MoveTurn(new GameUser(nextPlayer.UserId)),
            };
            return nextGame;
        }

        private DiceBoard RemovePreviousMoves(DiceBoard board, int playerIndex)
        {
            var playerColor = DicePiece[playerIndex];
            var cells = board.Cells;
            cells = cells.ToDictionary(kv => kv.Key,
                kv => kv.Value[playerIndex] != playerColor ? kv.Value : UpdatePlayerCellToDefault(kv.Value, playerIndex));
            return new DiceBoard(BoardSize, cells);
        }

        private string[] UpdatePlayerCellToDefault(string[] colors, int playerIndex)
        {
            colors[playerIndex] = DefaultCell;
            return colors;
        }

        private (int, int) GetRowAndColValues(long value)
        {
            var row = value / BoardSize;
            var col = value % BoardSize;
            return ((int)row, (int)col);
        }

        readonly string DefaultCell = "lightblue";

        readonly Dictionary<int, string> DicePiece = new Dictionary<int, string>() {
            {0, "blue"},
            {1, "green"},
            {2, "red"},
            {3, "yellow"},
        };
    }
    
}