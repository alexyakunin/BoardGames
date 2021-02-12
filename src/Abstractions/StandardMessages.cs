using System;
using System.Collections.Generic;
using System.Linq;

namespace BoardGames.Abstractions
{
    public static class StandardMessages
    {
        public static string Draw()
            => "It's a draw!";
        public static string Win(AppUser winner)
            => new GameMessage(
                new UserMention(winner),
                new PlainText(" won!")
            ).Format();
        public static string WinWithScore(AppUser winner, Game game, long score)
            => new GameMessage(
                new UserMention(winner),
                new PlainText(" won with "),
                new GameScoreMention(game, score)
            ).Format();
        public static string MoveTurn(AppUser player)
            => new GameMessage
            (new UserMention(player),
                new PlainText(", your turn!")
            ).Format();

        public static string MakeYourChoice()
            => "Players, make your choice!";
        public static string MakeYourChoice(IEnumerable<AppUser> users)
            => CallForAction(users, "make your choice!");
        public static string CallForAction(IEnumerable<AppUser> users, string suffix)
        {
            var lUsers = users.ToList();
            if (lUsers.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(users));
            List<MessageFragment> fragments = new();
            for (var i = 0; i < lUsers.Count; i++) {
                var isLast = i == lUsers.Count - 1;
                if (!isLast)
                    fragments.Add(new PlainText(", "));
                else {
                    var delimiter = lUsers.Count switch {
                        1 => "",
                        2 => " and ",
                        _ => ", and ",
                    };
                    if (!string.IsNullOrEmpty(delimiter))
                        fragments.Add(new PlainText(delimiter));
                }
                fragments.Add(new UserMention(lUsers[i]));
            }
            fragments.Add(new PlainText(", " + suffix));
            return new GameMessage(fragments.ToArray()).Format();
        }

        public static string CurrentStandings(Game game)
            => Standings(game, "Current standings: ");
        public static string FinalStandings(Game game)
            => Standings(game, "Final standings: ");
        public static string Standings(Game game, string prefix)
        {
            List<MessageFragment> fragments = new() { new PlainText(prefix) };
            var isFirst = true;
            foreach (var player in game.Players.OrderByDescending(p => p.Score)) {
                if (!isFirst)
                    fragments.Add(new PlainText(", "));
                fragments.Add(new UserMention(new AppUser(player.UserId)));
                fragments.Add(new PlainText(": "));
                fragments.Add(new GameScoreMention(game, player.Score));
                isFirst = false;
            }
            return new GameMessage(fragments.ToArray()).Format();
        }
    }
}
