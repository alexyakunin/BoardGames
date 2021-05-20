using System;
using System.ComponentModel.DataAnnotations;
using BoardGames.Abstractions;
using Stl.DependencyInjection;

namespace BoardGames.ClientServices
{
    [RegisterService(typeof(IUserNameService))]
    public class UserNameService : IUserNameService
    {
        public ValidationException? ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new ValidationException("Name is empty.");
            if (name.Length < 4)
                return  new ValidationException("Name is too short.");
            if (!char.IsLetter(name[0]))
                return  new ValidationException("Name must start with a letter.");
            foreach (var c in name.AsSpan(1)) {
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                    return new ValidationException("Name may contain only letters, digits, '-' and '_'.");
            }
            return null;
        }

        public string ParseName(string text, int startIndex = 0)
        {
            string name;
            for (var i = startIndex; i < text.Length; i++) {
                var c = text[i];
                if (i == startIndex) {
                    if (char.IsLetter(c))
                        continue;
                    return "";
                }
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    continue;
                name = text.Substring(startIndex, i - startIndex);
                return ValidateName(name) == null ? name : "";
            }
            name = text.Substring(startIndex);
            return ValidateName(name) == null ? name : "";
        }
    }
}
