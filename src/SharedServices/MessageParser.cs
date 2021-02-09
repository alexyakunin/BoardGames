using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using BoardGames.Abstractions;
using Stl.Fusion;

namespace BoardGames.ClientServices
{
    [ComputeService(typeof(IMessageParser))]
    public class MessageParser : IMessageParser
    {
        protected IGameService GameService { get; }
        protected IGameUserService GameUserService { get; }
        protected IUserNameService UserNameService { get; }

        public MessageParser(
            IGameService gameService,
            IGameUserService gameUserService,
            IUserNameService userNameService)
        {
            GameService = gameService;
            GameUserService = gameUserService;
            UserNameService = userNameService;
        }

        public virtual async ValueTask<GameMessage> ParseAsync(string text, CancellationToken cancellationToken = default)
        {
            List<MessageFragment> fragments = new();
            var startIndex = 0;

            void AddPlainText(int length) {
                if (length < 0)
                    startIndex -= length;
                fragments.Add(new PlainText(text.Substring(startIndex, length)));
                startIndex += length;
            }

            bool StartsWith(string start) => text.AsSpan(startIndex).StartsWith(start);

            bool TryParseDirective(string directive, out string value) {
                value = "";
                if (!StartsWith(directive))
                    return false;
                if (startIndex + directive.Length + 2 >= text.Length || text[startIndex + directive.Length] != '[') {
                    AddPlainText(directive.Length);
                    return false;
                }
                var valueStartIndex = startIndex + directive.Length + 1;
                var rightBracketIndex = text.IndexOf(']', valueStartIndex);
                if (rightBracketIndex < 0) {
                    AddPlainText(directive.Length);
                    return false;
                }
                value = text.Substring(valueStartIndex, rightBracketIndex - valueStartIndex);
                startIndex = rightBracketIndex + 1;
                return true;
            }

            while (startIndex < text.Length) {
                if (text[startIndex] != '@') {
                    var endIndex = text.IndexOf('@', startIndex);
                    if (endIndex < 0)
                        endIndex = text.Length;
                    AddPlainText(endIndex - startIndex);
                    continue;
                }
                if (StartsWith("@@")) {
                    fragments.Add(new PlainText("@"));
                    startIndex += 2;
                    continue;
                }

                var directiveStartIndex = startIndex;
                if (TryParseDirective("@user", out var userIdText)) {
                    if (!long.TryParse(userIdText, out var userId)) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    var user = await GameUserService.FindAsync(userId, cancellationToken);
                    if (user == null) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    fragments.Add(new GameUserMention(user));
                    continue;
                }
                if (TryParseDirective("@score", out var scoreText)) {
                    var scoreParts = scoreText.Split(",");
                    if (scoreParts.Length != 2) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    var gameId = scoreParts[0].Trim();
                    if (string.IsNullOrEmpty(gameId)) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    if (!long.TryParse(scoreParts[1].Trim(), out var score)) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    var game = await GameService.FindAsync(gameId, cancellationToken);
                    if (game == null) {
                        AddPlainText(directiveStartIndex - startIndex);
                        continue;
                    }
                    fragments.Add(new GameScoreMention(game, score));
                    continue;
                }
                if (StartsWith("@")) {
                    var name = UserNameService.ParseName(text, startIndex + 1);
                    if (string.IsNullOrEmpty(name)) {
                        AddPlainText(1);
                        continue;
                    }
                    var user = await GameUserService.FindByNameAsync(name, cancellationToken);
                    if (user == null) {
                        AddPlainText(name.Length + 1);
                        continue;
                    }
                    fragments.Add(new GameUserMention(user));
                    startIndex += name.Length + 1;
                    continue;
                }
            }

            List<MessageFragment> optimizedFragments = new();
            MessageFragment? lastFragment = null;
            foreach (var fragment in fragments) {
                if (fragment is PlainText pt && lastFragment is PlainText lpt) {
                    lastFragment = new PlainText(lpt.Text + pt.Text);
                    optimizedFragments.RemoveAt(optimizedFragments.Count - 1);
                }
                else
                    lastFragment = fragment;
                optimizedFragments.Add(lastFragment);
            }

            return new GameMessage() {
                Text = text,
                Fragments = optimizedFragments.ToImmutableList()
            };
        }
    }
}
