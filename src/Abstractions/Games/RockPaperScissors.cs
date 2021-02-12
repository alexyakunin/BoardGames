using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Stl.DependencyInjection;

namespace BoardGames.Abstractions.Games
{
    public enum RpsVote
    {
        None = 0,
        Rock = 1,
        Paper = 2,
        Scissors = 3,
    }

    public record RpsState
    {
        public ImmutableArray<RpsVote> Votes { get; init; } = ImmutableArray<RpsVote>.Empty;
        public ImmutableArray<RpsVote> LastVotes { get; init; } = ImmutableArray<RpsVote>.Empty;
        public ImmutableArray<int> LastVoteCounts { get; init; } = ImmutableArray<int>.Empty;
        public ImmutableArray<int> TotalVoteCounts { get; init; } = ImmutableArray<int>.Empty;

        public RpsState() { }
        public RpsState(int playerCount)
        {
            Votes = LastVotes = Enumerable.Range(0, playerCount).Select(_ => RpsVote.None).ToImmutableArray();
            TotalVoteCounts = LastVoteCounts = RpsEngine.GetVoteCounts(Votes);
        }
    }

    public record RpsMove(RpsVote Vote) : GameMove
    {
        public RpsMove() : this(RpsVote.None) { }
    }

    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class RpsEngine : GameEngine<RpsState, RpsMove>
    {
        public override string Id => "rps";
        public override string Title => "Rock-Paper-Scissors";
        public override string Icon => "fa-hand-scissors";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 10;
        public override bool AutoStart => false;

        public override Game Create(Game game)
            => game with { RoundCount = 10 };

        public override Game Start(Game game)
        {
            var state = new RpsState(game.Players.Count);
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MakeYourChoice(),
            };
        }

        public override Game Move(Game game, RpsMove move)
        {
            if (game.Stage == GameStage.Ended)
                throw new ApplicationException("Game is ended.");
            if (move.Vote == RpsVote.None)
                throw new ApplicationException("Vote can't be undone.");

            var state = DeserializeState(game.StateJson);
            var nextVotes = state.Votes.SetItem(move.PlayerIndex, move.Vote);
            var nextState = state with { Votes = nextVotes };
            var isRoundEnded = nextVotes.All(v => v != RpsVote.None);
            if (isRoundEnded) {
                var nextPlayers = new List<GamePlayer>();
                for (var playerIndex = 0; playerIndex < game.Players.Count; playerIndex++) {
                    var player = game.Players[playerIndex];
                    var vote = nextVotes[playerIndex];
                    var losingVote = GetLosingVote(vote);
                    var winningVote = GetWinningVote(vote);
                    var scoreDelta = nextVotes.Sum(v => v == losingVote ? 2 : v == winningVote ? 0 : 1) - 1;
                    var nextPlayer = player with {
                        Score = player.Score + scoreDelta
                    };
                    nextPlayers.Add(nextPlayer);
                }
                nextState = nextState with {
                    Votes = Enumerable.Range(0, nextPlayers.Count).Select(_ => RpsVote.None).ToImmutableArray(),
                    LastVotes = nextVotes,
                    TotalVoteCounts = SumVoteCounts(state.TotalVoteCounts, state.LastVoteCounts),
                    LastVoteCounts = GetVoteCounts(nextVotes),
                };
                var nextGame = game with {
                    RoundIndex = game.RoundIndex + 1,
                    StateJson = SerializeState(nextState),
                    Players = nextPlayers.ToImmutableList(),
                };
                var isGameEnded = nextGame.RoundIndex >= nextGame.RoundCount!.Value;
                if (isGameEnded) {
                    return nextGame with {
                        StateJson = SerializeState(nextState),
                        StateMessage = StandardMessages.FinalStandings(nextGame),
                        Stage = GameStage.Ended,
                    };
                }
                return nextGame with {
                    StateMessage = StandardMessages.CurrentStandings(nextGame),
                };
            }

            var remainingPlayers = game.Players
                .Where((p, i) => nextVotes[i] == RpsVote.None)
                .Select(p => new AppUser(p.UserId));
            return game with {
                StateJson = SerializeState(nextState),
                StateMessage = StandardMessages.MakeYourChoice(remainingPlayers),
            };
        }

        public static ImmutableArray<int> GetVoteCounts(ImmutableArray<RpsVote> votes)
        {
            var result = new int[4];
            foreach (var vote in votes)
                result[(int) vote]++;
            return result.ToImmutableArray();
        }

        public static ImmutableArray<int> SumVoteCounts(ImmutableArray<int> voteCounts1, ImmutableArray<int> voteCounts2)
        {
            var result = new int[4];
            for (var i = 0; i < voteCounts1.Length; i++)
                result[i] = voteCounts1[i] + voteCounts2[i];
            return result.ToImmutableArray();
        }

        public static RpsVote GetWinningVote(RpsVote vote)
            => vote switch {
                RpsVote.Rock => RpsVote.Paper,
                RpsVote.Paper => RpsVote.Scissors,
                RpsVote.Scissors => RpsVote.Rock,
                _ => throw new ArgumentOutOfRangeException(nameof(vote), vote, null)
            };

        public static RpsVote GetLosingVote(RpsVote vote)
            => vote switch {
                RpsVote.Rock => RpsVote.Scissors,
                RpsVote.Paper => RpsVote.Rock,
                RpsVote.Scissors => RpsVote.Paper,
                _ => throw new ArgumentOutOfRangeException(nameof(vote), vote, null)
            };
    }
}
