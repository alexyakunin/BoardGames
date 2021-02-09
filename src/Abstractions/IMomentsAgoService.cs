using System;
using System.Threading.Tasks;

namespace BoardGames.Abstractions
{
    public interface IMomentsAgoService
    {
        Task<string> GetMomentsAgoAsync(DateTime time);
    }
}
