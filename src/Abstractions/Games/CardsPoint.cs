using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Stl.DependencyInjection;

namespace BoardGames.Abstractions.Games
{
    public enum CardSuit
    {
        Diamonds,
        Hearts,
        Spades,
        Clubs
    }

    public enum CardRank
    {
        Jack = 3,
        Queen = 4,
        King = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Ace = 11
    }

    public enum Status
    {
        Active,
        Finished
    }

    public record Card
    {
        public int Id { get; set; }
        public CardSuit Suit { get; set; }
        public CardRank Rank { get; set; }

        public Card(int id, CardSuit suit, CardRank rank)
        {
            Id = id;
            Suit = suit;
            Rank = rank;
        }
    }

    public record PointGameState(ImmutableList<Card> Cards,
        ImmutableList<int> TotalScores,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0,
        int PlayersCount = 0)
    {
        public ImmutableList<ImmutableList<Card>> PlayersCards { get; set; } = ImmutableList<ImmutableList<Card>>.Empty;
        public ImmutableList<Status> Statuses { get; set; } = ImmutableList<Status>.Empty;
        public ImmutableList<int> Scores { get; set; } = ImmutableList<int>.Empty;
        public ImmutableList<int> TotalScores { get; set; } = ImmutableList<int>.Empty;
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % PlayersCount;

        public PointGameState() : this((ImmutableList<Card>)null!, (ImmutableList<int>)null!) { }

        public PointGameState(int playersCount) : this(ImmutableList<Card>.Empty, ImmutableList<int>.Empty)
        {
            var cards = new List<Card>();
            var cardId = 1;
            foreach (var suit in (CardSuit[])Enum.GetValues(typeof(CardSuit))) {
                foreach (var rank in (CardRank[])Enum.GetValues(typeof(CardRank))) {
                    var card = new Card(cardId, suit, rank);
                    cards.Add(card);
                    cardId++;
                }
            }

            Cards = cards.ToImmutableList();
            var statuses = new List<Status>();
            var playersCards = new List<ImmutableList<Card>>();
            var scores = new List<int>();
            for (var i = 0; i < playersCount; i++) {
                statuses.Add(Status.Active);
                playersCards.Add(ImmutableList<Card>.Empty);
                scores.Add(0);
            }

            TotalScores = Scores = scores.ToImmutableList();
            Statuses = statuses.ToImmutableList();
            PlayersCards = playersCards.ToImmutableList();
            MoveIndex = 0;
            FirstPlayerIndex = 0;
            PlayersCount = playersCount;
        }
    }

    public record PointGameMove(bool IsSkip) : GameMove
    { }

    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class PointGameEngine : GameEngine<PointGameState, PointGameMove>
    {
        public override string Id => "point";
        public override string Title => "Point (Twenty-One)";
        public override string Icon => "fa-map-marker-alt";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 9;
        public override bool AutoStart => false;

        public override Game Create(Game game)
            => game with {RoundCount = 5};

        public override Game Start(Game game)
        {
            var state = new PointGameState(game.Players.Count);
            var player = game.Players[state.PlayerIndex];
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId))
            };
        }

        public override Game Move(Game game, PointGameMove move)
        {
            if (game.Stage == GameStage.Ended)
                throw new ApplicationException("Game is ended.");
            var state = DeserializeState(game.StateJson);
            
            var newState = state;
            var newGame = game;
            var newTotalScoresList = newState.TotalScores.ToList();
            var newPlayer = newGame.Players[newState.PlayerIndex];
            var newStatusesList = newState.Statuses.ToList();
            var newCardsList = newState.Cards.ToList();
            var newMoveIndex = newState.MoveIndex;
            var newPlayersCardsList = newState.PlayersCards.ToList();
            var newScoresList = newState.Scores.ToList();

            if (move.PlayerIndex != newState.PlayerIndex)
                throw new ApplicationException("It's another player's turn.");
            
            if (move.IsSkip) {
                newStatusesList[newState.PlayerIndex] = Status.Finished;
                if (!IsRoundEnded(newStatusesList.ToImmutableList()))
                    newMoveIndex = GetNextMoveIndex(newStatusesList.ToImmutableList(), newState.MoveIndex);
            }
            else {
                var card = GetRandomCard(newCardsList.ToImmutableList());
                newCardsList.Remove(card);
                var currentPlayerCards = newPlayersCardsList[newState.PlayerIndex].ToList();
                currentPlayerCards.Add(card);
                newPlayersCardsList[state.PlayerIndex] = currentPlayerCards.ToImmutableList();

                var currentPlayerScores = GetPlayerScores(card, newScoresList[state.PlayerIndex], newPlayersCardsList[newState.PlayerIndex].Count);
                newScoresList[state.PlayerIndex] = currentPlayerScores;
                newMoveIndex = GetNextMoveIndex(newStatusesList.ToImmutableList(), newState.MoveIndex);
                
                var currentPlayerStatus = CheckOrChangePlayerStatus(currentPlayerScores);
                if (currentPlayerStatus != newStatusesList[newState.PlayerIndex])
                    newStatusesList[newState.PlayerIndex] = currentPlayerStatus;
            }

            newState = newState with {
                Cards = newCardsList.ToImmutableList(),
                PlayersCards = newPlayersCardsList.ToImmutableList(),
                Scores = newScoresList.ToImmutableList(),
                Statuses = newStatusesList.ToImmutableList(),
                MoveIndex = newMoveIndex,
                TotalScores = newTotalScoresList.ToImmutableList(),
            };
            newGame = newGame with {StateJson = SerializeState(newState)};
            newPlayer = newGame.Players[newState.PlayerIndex];
            newGame = newGame with {
                StateJson = SerializeState(newState),
                StateMessage = StandardMessages.MoveTurn(new AppUser(newPlayer.UserId))
            };

            if (IsRoundEnded(newState.Statuses)) {
                for (var i = 0; i < newState.Scores.Count; i++) {
                    newTotalScoresList[i] += newScoresList[i];
                }

                var (pCards, statuses, scores)
                    = SetNewStateData(newState.PlayersCount);
                
                newState = newState with {
                    Cards = InitializeCardsDeck(),
                    TotalScores = newTotalScoresList.ToImmutableList(),
                    MoveIndex = 0,
                    FirstPlayerIndex = 0,
                    PlayersCount = newGame.Players.Count,
                    PlayersCards = pCards,
                    Statuses = statuses,
                    Scores = scores
                };
                
                var newPlayers = new List<GamePlayer>();
                for (int i = 0; i < newGame.Players.Count; i++) {
                    var player = newGame.Players[i];
                    player = player with {
                        Score = newState.TotalScores[i]
                    };
                    newPlayers.Add(player);
                }
                
                newGame = newGame with {
                    RoundIndex = game.RoundIndex + 1,
                    StateJson = SerializeState(newState),
                    Players = newPlayers.ToImmutableList(),
                };
                var isGameEnded = newGame.RoundIndex >= newGame.RoundCount!.Value;
                if (isGameEnded) {
                    newGame = newGame with {
                        StateMessage = StandardMessages.FinalStandings(newGame),
                        Stage = GameStage.Ended,
                    };
                }
            }
            return newGame;
        }

        private Card GetRandomCard(ImmutableList<Card> cards)
        {
            return cards[new Random().Next(0, cards.Count)];
        }

        private int GetPlayerScores(Card card, int playerScores, int cardsCount)
        {
            var currentScore = (int)card.Rank + playerScores;
            if (cardsCount == 2) {
                if (currentScore == 22) currentScore += 10;
                else if (currentScore == 21) currentScore += 5;
            }
            else if (cardsCount > 2) {
                if (currentScore == 21) currentScore += 5;
                else if (currentScore > 21) currentScore = 2;
            }
            return currentScore;
        }

        private Status CheckOrChangePlayerStatus(int s)
        {
            if (s == 32 || s == 26 || s == 2 || s == 19 || s == 20) return Status.Finished;
            return Status.Active;
        }

        private int GetNextMoveIndex(ImmutableList<Status> statuses, int moveIndex)
        {
            moveIndex += 1;
            moveIndex %= statuses.Count;
            var isActive = statuses[moveIndex] != Status.Finished;
            return isActive ? moveIndex : GetNextMoveIndex(statuses, moveIndex);
        }

        private bool IsRoundEnded(ImmutableList<Status> statuses)
            =>  statuses.All(s => s == Status.Finished);

        private ImmutableList<Card> InitializeCardsDeck()
        {
            var cards = new List<Card>();
            var cardId = 1;
            foreach (var suit in (CardSuit[])Enum.GetValues(typeof(CardSuit))) {
                foreach (var rank in (CardRank[])Enum.GetValues(typeof(CardRank))) {
                    var card = new Card(cardId, suit, rank);
                    cards.Add(card);
                    cardId++;
                }
            }
            return cards.ToImmutableList();
        }

        private (ImmutableList<ImmutableList<Card>> pCards,
            ImmutableList<Status> statuses,
            ImmutableList<int> scores)
            SetNewStateData(int playersCount)
        {
            var statuses = new List<Status>();
            var playersCards = new List<ImmutableList<Card>>();
            var scores = new List<int>();
            for (var i = 0; i < playersCount; i++) {
                statuses.Add(Status.Active);
                playersCards.Add(ImmutableList<Card>.Empty);
                scores.Add(0);
            }
            return (playersCards.ToImmutableList(), statuses.ToImmutableList(), scores.ToImmutableList());
        }
    }
}