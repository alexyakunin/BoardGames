using System;

namespace BoardGames.Abstractions
{
    [Flags]
    public enum ChatPermission
    {
        Read = 0x1,
        Post = 0x2 | Read,
        Write = 0x4 | Post,
        Owner = 0x100 | Write
    }
}
