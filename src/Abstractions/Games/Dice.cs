using System;
using Stl.DependencyInjection;

namespace BoardGames.Abstractions.Games
{
    public record DiceState(DiceBoard Board,
        int[] PlayerPositions,
        int[] PlayerSteps,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0,
        int PlayersCount = 0
        )

    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % PlayersCount;
        public DiceState() : this((DiceBoard) null!, (int[]) null!, (int[]) null!) { }
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
            var positions = new int[] {-1, -1, -1, -1};
            var steps = new int[] {0, 0, 0, 0};
            var rnd = new Random();
            var playersCount = game.Players.Count;
            var firstPlayerIndex = rnd.Next(0, playersCount);
            var state = new DiceState(DiceBoard.Empty(BoardSize), positions, steps, 0, firstPlayerIndex, playersCount);
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
            
            state.PlayerPositions[state.PlayerIndex] += move.Value;
            var playerPosition = state.PlayerPositions[state.PlayerIndex];

            if (playerPosition == 10 || playerPosition == 27 || playerPosition == 44) {
                state.PlayerPositions[state.PlayerIndex] += 3;
                playerPosition += 3;
            }
            if (playerPosition == 20 || playerPosition == 35 || playerPosition == 54) {
                state.PlayerPositions[state.PlayerIndex] -= 3;
                playerPosition -= 3;
            }
            
            if (playerPosition >= (BoardSize * BoardSize) - 1)
                playerPosition = (BoardSize * BoardSize) - 1;
            
            state.PlayerSteps[state.PlayerIndex] += 1;
            var playerSteps = state.PlayerSteps[state.PlayerIndex];
            var scores = state.PlayerPositions;
            
            if (playerPosition >= (BoardSize * BoardSize) - 1) {
                var newState = state with {Board = board, MoveIndex = state.MoveIndex + 1, PlayerPositions = scores};
                var newGame = game with {StateJson = SerializeState(newState)};
                newGame = IncrementPlayerScore(newGame, move.PlayerIndex, 1) with {
                    StateMessage = StandardMessages.WinWithScore(new AppUser(player.UserId), game, playerSteps),
                    Stage = GameStage.Ended,
                };
                return newGame;
            }
            
            var rowAndCol = GetRowAndColValues(playerPosition);
            var nextBoard = board.Set(rowAndCol.Item1, rowAndCol.Item2, state.PlayerIndex, 'X');
            var nextState = state with {
                Board = nextBoard,
                MoveIndex = state.MoveIndex + 1,
                PlayerPositions = scores,
            };
            var nextPlayer = game.Players[nextState.PlayerIndex];
            var nextGame = game with {StateJson = SerializeState(nextState)};
            nextGame = nextGame with {
                StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
            };
            return nextGame;
        }

        private (int, int) GetRowAndColValues(long value)
        {
            var row = value / BoardSize;
            var col = value % BoardSize;
            return ((int)row, (int)col);
        }
    }
}