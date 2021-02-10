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
            Text = fragments.ToDelimitedString("");
        }

        public GameMessage(params MessageFragment[] fragments)
        {
            Fragments = ImmutableList<MessageFragment>.Empty.AddRange(fragments);
            Text = fragments.ToDelimitedString("");
        }

        public override string ToString() => Format();
        public virtual string Format(bool editable = false)
            => editable
                ? Fragments.Select(f => f.Format(true)).ToDelimitedString("")
                : Fragments.ToDelimitedString("");
    }

    public abstract record MessageFragment
    {
        public override string ToString() => Format();
        public abstract string Format(bool editable = false);
    }

    public record PlainText(string Text) : MessageFragment
    {
        public PlainText() : this("") { }
        public override string ToString() => Format();
        public override string Format(bool editable = false)
            => Text.Replace("@", "@@");
    }

    public record UserMention(AppUser User) : MessageFragment
    {
        public UserMention() : this(AppUser.None) { }
        public override string ToString() => Format();
        public override string Format(bool editable = false)
            => editable ? "@" + User.Name : $"@user[{User.Id}]";
    }

    public record GameScoreMention(Game Game, long Score) : MessageFragment
    {
        public GameScoreMention() : this(null!, 0) { }
        public override string ToString() => Format();
        public override string Format(bool editable = false)
            => $"@score[{Game.Id},{Score}]";
    }
}
