using System.Collections.Immutable;
using System.Linq;
using Stl.Collections;

namespace BoardGames.Abstractions
{
    public record GameMessage
    {
        public ImmutableList<MessageFragment> Fragments { get; init; } = ImmutableList<MessageFragment>.Empty;
        public string Text { get; init; } = "";

        public GameMessage() { }

        public GameMessage(ImmutableList<MessageFragment> fragments)
        {
            Fragments = fragments;
            Text = fragments.Select(f => f.ToString()).ToDelimitedString("");
        }

        public GameMessage(params MessageFragment[] fragments)
        {
            Fragments = ImmutableList<MessageFragment>.Empty.AddRange(fragments);
            Text = fragments.Select(f => f.ToString()).ToDelimitedString("");
        }

        public override string ToString() => Fragments.ToDelimitedString("");
    }

    public abstract record MessageFragment
    { }

    public record PlainText(string Text) : MessageFragment
    {
        public PlainText() : this("") { }
        public override string ToString() => Text.Replace("@", "@@");
    }

    public record GameUserMention(GameUser User) : MessageFragment
    {
        public GameUserMention() : this(GameUser.None) { }
        public override string ToString() => $"@user[{User.Id}]";
    }

    public record GameScoreMention(Game Game, long Score) : MessageFragment
    {
        public GameScoreMention() : this(null!, 0) { }
        public override string ToString() => $"@score[{Game.Id},{Score}]";
    }
}
