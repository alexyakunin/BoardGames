using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Stl.DependencyInjection;
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

    public record DiceMove(int PlayerIndex, int Value) : GameMove
    {
        public DiceMove() : this(0, 0) {}
    }

    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
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
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId)),
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
            var oldPlayerScore = state.Scores[state.PlayerIndex];
            state.Scores[state.PlayerIndex] += move.Value;
            var playerScore = state.Scores[state.PlayerIndex];
            var scores = state.Scores;

            if (playerScore >= (BoardSize * BoardSize) - 1)
                playerScore = (BoardSize * BoardSize) - 1;

            board = ChangePlayerCells(board, move.PlayerIndex, playerScore, oldPlayerScore);
            
            if (playerScore >= (BoardSize * BoardSize) - 1) {
                var newState = state with {Board = board, MoveIndex = state.MoveIndex + 1, Scores = scores};
                var newGame = game with {StateJson = SerializeState(newState)};
                newGame = IncrementPlayerScore(newGame, move.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.Win(new AppUser(player.UserId)),
                    Stage = GameStage.Ended,
                };
                return newGame;
            }
            
            var rowAndCol = GetRowAndColValues(playerScore);

            var nextBoard = board.Set(rowAndCol.Item1, rowAndCol.Item2, state.PlayerIndex, GetOpacity(Opacity.Visible));
            var nextState = state with {
                Board = nextBoard,
                MoveIndex = state.MoveIndex + 1,
                Scores = scores,
            };
            var nextPlayer = game.Players[nextState.PlayerIndex];
            var nextGame = game with {StateJson = SerializeState(nextState)};
            nextGame = nextGame with {
                StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
            };
            return nextGame;
        }

        private DiceBoard ChangePlayerCells(DiceBoard board, int playerIndex, int newScore, int oldScore)
        {
            var cells = board.Cells;
            for (int i = 0; i < newScore + 1; i++) {
                var cell = cells.ElementAt(i).Value;
                Task.Delay(1000);
                cell[playerIndex] = GetOpacity(Opacity.Past);
            }
            return board;
        }

        private (int, int) GetRowAndColValues(long value)
        {
            var row = value / BoardSize;
            var col = value % BoardSize;
            return ((int)row, (int)col);
        }

        enum Opacity
        {
            Invisible = 0,
            Past = 1,
            Visible = 10,
        }

        private double GetOpacity(Opacity opacity)
        {
            return (double)opacity / 10;
        }
    }
}