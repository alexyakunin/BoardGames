using System.ComponentModel.DataAnnotations;

namespace BoardGames.Abstractions
{
    public interface IUserNameService
    {
        ValidationException? ValidateName(string name);
        string ParseName(string text, int startIndex = 0);
    }
}
