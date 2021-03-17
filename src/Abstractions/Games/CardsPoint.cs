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

    public record PointState(ImmutableList<Card> Cards,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0,
        int PlayersCount = 0)
    {
        public ImmutableList<ImmutableList<Card>> PlayersCards { get; set; } = ImmutableList<ImmutableList<Card>>.Empty;
        public ImmutableList<Status> Statuses { get; set; } = ImmutableList<Status>.Empty;
        public ImmutableList<int> Scores { get; set; } = ImmutableList<int>.Empty;
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % PlayersCount;

        public PointState() : this((ImmutableList<Card>)null!) { }

        public PointState(int playersCount) : this((ImmutableList<Card>.Empty))
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

            // var playersCards = new Dictionary<int, List<Card>>();
            var playersCards = new List<ImmutableList<Card>>();
            // var scores = new Dictionary<int, int>();
            var scores = new List<int>();
            for (var i = 0; i < playersCount; i++) {
                statuses.Add(Status.Active);
                playersCards.Add(ImmutableList<Card>.Empty);
                scores.Add(0);
            }

            Scores = scores.ToImmutableList();
            Statuses = statuses.ToImmutableList();
            PlayersCards = playersCards.ToImmutableList();
            MoveIndex = 0;
            FirstPlayerIndex = new Random().Next(0, playersCount);
            PlayersCount = playersCount;
        }
    }

    public record PointMove(bool IsSkip) : GameMove
    { }

    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class PointEngine : GameEngine<PointState, PointMove>
    {
        public override string Id => "point";
        public override string Title => "Point (Twenty-One)";
        public override string Icon => "fa-map-marker-alt";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 9;
        public override bool AutoStart => false;

        public override Game Start(Game game)
        {
            var state = new PointState(game.Players.Count);
            var player = game.Players[state.PlayerIndex];
            return game with {
                StateJson = SerializeState(state),
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId))
            };
        }

        public override Game Move(Game game, PointMove move)
        {
            if (game.Stage == GameStage.Ended)
                throw new ApplicationException("Game is ended.");
            var state = DeserializeState(game.StateJson);
            var player = game.Players[state.PlayerIndex];
            Status currentPlayerStatus;
            ImmutableList<Status> newStatuses = ImmutableList<Status>.Empty;
            var newState = state;
            var newGame = game;
            var newPlayer = newGame.Players[newState.PlayerIndex];

            if (move.IsSkip) {
                currentPlayerStatus = Status.Finished;
                newStatuses = state.Statuses.SetItem(state.PlayerIndex, currentPlayerStatus);

                newState = state with {
                    Statuses = newStatuses
                };
            
                newGame = game with {
                    StateJson = SerializeState(newState)
                };
            
                if (newState.Statuses.All(s => s == Status.Finished))
                    return newGame with {
                        StateJson = SerializeState(newState),
                        StateMessage = StandardMessages.FinalStandings(newGame),
                        Stage = GameStage.Ended
                    };

                state = newState with {
                    MoveIndex = GetNextMoveIndex(newState.Statuses, newState.MoveIndex),
                };
                newPlayer = newGame.Players[state.PlayerIndex];
                game = newGame with {StateJson = SerializeState(state)};
                game = game with {
                    StateMessage = StandardMessages.MoveTurn(new AppUser(newPlayer.UserId))
                };
            
                return game;
            }
            
            if (move.PlayerIndex != state.PlayerIndex)
                throw new ApplicationException("It's another player's turn.");
            var cards = state.Cards;
            var card = GetRandomCard(cards);
            var newCards = cards.Remove(card);
            cards = newCards;
            var playersCards = state.PlayersCards.ToList();
            var currentPlayerCards = playersCards[state.PlayerIndex].ToList();
            currentPlayerCards.Add(card);
            playersCards[state.PlayerIndex] = currentPlayerCards.ToImmutableList();

            var scores = state.Scores.ToList();
            var currentPlayerScores = GetPlayerScores(card, scores[state.PlayerIndex], playersCards[state.PlayerIndex].Count);
            scores[state.PlayerIndex] = currentPlayerScores;
            
            var allStatuses = state.Statuses;
            currentPlayerStatus = CheckOrChangePlayerStatus(currentPlayerScores);
            if (currentPlayerStatus != state.Statuses[state.PlayerIndex])
                allStatuses.SetItem(state.PlayerIndex, currentPlayerStatus);
            newStatuses = currentPlayerStatus == allStatuses[state.PlayerIndex]
                ? allStatuses
                : allStatuses.SetItem(state.PlayerIndex, currentPlayerStatus);

            newState = state with {
                Cards = cards,
                PlayersCards = playersCards.ToImmutableList(),
                Scores = scores.ToImmutableList(),
                Statuses = newStatuses
            };
            
            newGame = game with {
                StateJson = SerializeState(newState)
            };
            
            if (newState.Statuses.All(s => s == Status.Finished))
                return newGame with {
                    StateJson = SerializeState(newState),
                    StateMessage = StandardMessages.FinalStandings(newGame),
                    Stage = GameStage.Ended
                };

            state = newState with {
                MoveIndex = GetNextMoveIndex(newState.Statuses, newState.MoveIndex),
            };
            player = newGame.Players[state.PlayerIndex];
            game = newGame with {StateJson = SerializeState(state)};
            game = game with {
                StateMessage = StandardMessages.MoveTurn(new AppUser(player.UserId))
            };
            return game;
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
            if (statuses[moveIndex] == Status.Finished)
                return GetNextMoveIndex(statuses, moveIndex);
            return moveIndex;
        }
    }
}