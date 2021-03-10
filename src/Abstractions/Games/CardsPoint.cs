using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Stl.DependencyInjection;

namespace BoardGames.Abstractions.Games
{
    public enum Suits
    {
        Diamonds,
        Hearts,
        Spades,
        Clubs
    }

    public enum Ranks
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

    public class Card
    {
        public int Id { get; set; }
        public Suits Suit { get; set; }
        public Ranks Rank { get; set; }
        public int BoardIndex { get; set; } = 100;
        public int PlayerIndex { get; set; } = 100;

        public Card(int id, Suits suit, Ranks rank)
        {
            Id = id;
            Suit = suit;
            Rank = rank;
        }
    }

    public record PointState(CharBoard Board,
        ImmutableList<Card> Cards,
        int MoveIndex = 0,
        int FirstPlayerIndex = 0,
        int PlayersCount = 0)
    {
        public int PlayerIndex => (MoveIndex + FirstPlayerIndex) % PlayersCount;
        public PointState() : this((CharBoard) null!, ImmutableList<Card>.Empty, 0) {}
    }

    public record PointMove(int Row, int Column) : GameMove
    {
        public PointMove() : this(0, 0) { }
    }
    
    [Service, ServiceAlias(typeof(IGameEngine), IsEnumerable = true)]
    public class PointEngine : GameEngine<PointState, PointMove>
    {
        public static int BoardSize { get; } = 6;

        public override string Id => "point";
        public override string Title => "Point (21)";
        public override string Icon => "fa-file-powerpoint";
        public override int MinPlayerCount => 2;
        public override int MaxPlayerCount => 9;
        public override bool AutoStart => false;

        public override Game Start(Game game)
        {
            var cards = new List<Card>();
            var cardId = 1;
            foreach (Suits suit in Enum.GetValues(typeof(Suits))) {
                foreach (Ranks rank in Enum.GetValues(typeof(Ranks))) {
                    var card = new Card(cardId, suit, rank);
                    cards.Add(card);
                    cardId++;
                }
            }
            var playerCount = game.Players.Count;
            var firstPlayerIndex = new Random().Next(0, playerCount);
            var state = new PointState(CharBoard.Empty(BoardSize),
                cards.ToImmutableList(),
                0,
                firstPlayerIndex,
                playerCount);
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
            if (move.PlayerIndex != state.PlayerIndex)
                throw new ApplicationException("It's another player's turn.");
            var board = state.Board;
            var player = game.Players[state.PlayerIndex];
            var cards = state.Cards;
            var card = GetRandomCard(cards);
            var boardIndex = board.GetCardIndex(move.Row, move.Column);
            card.BoardIndex = boardIndex;
            card.PlayerIndex = state.PlayerIndex;
            cards.SetItem(card.Id, card);
            
            var nextState = state with {
                Board = board,
                MoveIndex = state.MoveIndex + 1,
                Cards = cards,
            };
            var nextPlayer = game.Players[nextState.PlayerIndex];
            var nextGame = game with {StateJson = SerializeState(nextState)};
            nextGame = nextGame with {
                StateMessage = StandardMessages.MoveTurn(new AppUser(nextPlayer.UserId))
            };
            
            return nextGame;
        }

        private Card GetRandomCard(ImmutableList<Card> cards)
        {
            var rnd = new Random();
            var result = rnd.Next(0, 36);
            var card = cards[result];
            if (card.PlayerIndex != 100 && card.BoardIndex != 100)
                return GetRandomCard(cards);
            return card;
        }
    }
}