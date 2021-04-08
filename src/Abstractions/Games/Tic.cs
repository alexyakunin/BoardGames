using System;
using System.Collections.Immutable;
using System.Linq;
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

    public record TicState(CharBoard Board,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0)
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
            var state = new TicState(CharBoard.Empty(BoardSize),0, firstPlayerIndex);
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

            var roundIndex = game.RoundIndex;
            var players = game.Players;

            var roundEnd = GetRoundEnded(nextBoard, move);
            switch (roundEnd) {
                case TicEnd.None:
                    return nextGame with {
                        StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId)),
                    };
                case TicEnd.Win:
                    roundIndex++;
                    var nextPlayers = players.ToList();
                    nextPlayers[state.PlayerIndex] = player with {
                        Score = player.Score + 1,
                    };
                    players = nextPlayers.ToImmutableList();
                    break;
                case TicEnd.Draw:
                    roundIndex++;
                    break;
                default:
                    throw new ArgumentException();
            }

            nextGame = nextGame with {
                RoundIndex = roundIndex,
                StateJson = SerializeState(nextState),
                Players = players,
            };

            var isGameEnded = roundIndex >= game.RoundCount!.Value;
            if (isGameEnded) {
                nextGame = nextGame with {
                    StateMessage = StandardMessages.FinalStandings(nextGame),
                    Stage = GameStage.Ended,
                };
            }
            else {
                nextState = nextState with {
                    Board = CharBoard.Empty(BoardSize),
                    FirstPlayerIndex = CoarseStopwatch.RandomInt32 & 1,
                    MoveIndex = 0,
                };
                player = nextGame.Players[nextState.PlayerIndex];
                nextGame = nextGame with {
                    StateJson = SerializeState(nextState),
                    StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId))
                };
            }
            return nextGame;
        }

        public char GetPlayerMarker(int playerIndex)
            => playerIndex == 0 ? 'X' : 'O';

        private TicEnd GetRoundEnded(CharBoard board, TicMove lastMove)
        {
            var marker = GetPlayerMarker(lastMove.PlayerIndex);
            var winningCombos = new int[8, 3]
            {
                {0,1,2 }, {3,4,5 }, {6,7,8 }, {0,3,6 },
                {1,4,7 }, {2,5,8 }, {0,4,8 }, {2,4,6 },
            };

            for (var i = 0; i < 8; i++)
                if (board.Cells[winningCombos[i, 0]] == marker
                    && board.Cells[winningCombos[i, 0]] == board.Cells[winningCombos[i, 1]]
                    && board.Cells[winningCombos[i, 0]] == board.Cells[winningCombos[i, 2]])
                    return TicEnd.Win;

            bool isBoardFull = true;
            foreach (var cell in board.Cells)
                if (cell == ' ') {
                    isBoardFull = false;
                    break;
                }
            return isBoardFull ? TicEnd.Draw : TicEnd.None;
        }
    }
}