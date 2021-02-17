using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Time;
using Stl.Time.Internal;

namespace BoardGames.Abstractions.Games
{
    public record DiceState(DiceBoard Board,
        Dictionary<int, int> Scores,
        Dictionary<int, int> Steps,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0,
        int PlayersCount = 0)

    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % PlayersCount;
        public DiceState() : this((DiceBoard) null!, (Dictionary<int, int>) null!, (Dictionary<int, int>) null!) { }
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
            var steps = new Dictionary<int, int>() {
                {0, 0},
                {1, 0},
                {2, 0},
                {3, 0},
            };
            var rnd = new Random();
            var playersCount = game.Players.Count;
            var firstPlayerIndex = rnd.Next(0, playersCount);
            var state = new DiceState(DiceBoard.Empty(BoardSize), scores, steps, 0, firstPlayerIndex, playersCount);
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
            var playerScore = oldPlayerScore += move.Value;
            state.Steps[state.PlayerIndex] += 1;
            var playerSteps = state.Steps[state.PlayerIndex];
            if (playerScore == 10 || playerScore == 27 || playerScore == 44) {
                playerScore += 3;
            }
            if (playerScore == 20 || playerScore == 35 || playerScore == 54) {
                playerScore -= 3;
            }
            state.Scores[state.PlayerIndex] = playerScore;
            var scores = state.Scores;

            if (playerScore >= (BoardSize * BoardSize) - 1)
                playerScore = (BoardSize * BoardSize) - 1;

            board = ChangePlayerCells(board, move.PlayerIndex, playerScore, oldPlayerScore);
            
            if (playerScore >= (BoardSize * BoardSize) - 1) {
                var newState = state with {Board = board, MoveIndex = state.MoveIndex + 1, Scores = scores};
                var newGame = game with {StateJson = SerializeState(newState)};
                newGame = IncrementPlayerScore(newGame, move.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.WinWithScore(new AppUser(player.UserId), game, playerSteps),
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
                cell[playerIndex] = GetOpacity(Opacity.Past);
            }

            for (int i = newScore + 1; i < BoardSize * BoardSize; i++) {
                var cell = cells.ElementAt(i).Value;
                cell[playerIndex] = GetOpacity(Opacity.Invisible);
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