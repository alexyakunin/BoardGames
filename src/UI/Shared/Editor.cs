using System.Collections.Generic;

namespace BoardGames.UI.Shared
{
    public class Editor<TValue>
    {
        private TValue _original = default!;

        public TValue Original {
            get => _original;
            set {
                var isChanged = IsChanged;
                _original = value;
                if (!isChanged)
                    Current = value;
            }
        }

        public TValue Current { get; set; } = default!;
        public bool IsChanged => !EqualityComparer<TValue>.Default.Equals(Original, Current);

        public void Reset()
            => Current = Original;
    }
}
