using MessagePack;

namespace BoardGames.Abstractions;

[MessagePackObject(true)]
public sealed partial record AppUser(long Id, string Name = "(unknown)")
{
    public static AppUser None { get; } = new(0, "(none)");
}
