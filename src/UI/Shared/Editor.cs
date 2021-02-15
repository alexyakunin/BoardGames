using System;
using System.Collections.Generic;

namespace BoardGames.UI.Shared
{
    public class Editor<TValue>
    {
        private TValue _originalValue = default!;
        private TValue _value = default!;
        private Func<Editor<TValue>, string> _validator = _ => "";

        public TValue OriginalValue {
            get => _originalValue;
            set {
                var isChanged = IsChanged;
                _originalValue = value;
                if (!isChanged)
                    Value = value;
                else
                    ValidationMessage = Validator.Invoke(this);
            }
        }

        public TValue Value {
            get => _value;
            set {
                _value = value;
                ValidationMessage = Validator.Invoke(this);
            }
        }

        public Func<Editor<TValue>, string> Validator {
            get => _validator;
            set {
                _validator = value;
                ValidationMessage = Validator.Invoke(this);
            }
        }

        public string ValidationMessage { get; private set; } = "";
        public bool IsChanged => !EqualityComparer<TValue>.Default.Equals(OriginalValue, Value);
        public bool IsValid => string.IsNullOrEmpty(ValidationMessage);

        public void Reset()
            => Value = OriginalValue;

        public void Reset(TValue original)
        {
            OriginalValue = original;
            Value = OriginalValue;
        }
    }
}
