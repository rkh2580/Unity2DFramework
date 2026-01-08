using System;
using System.Collections.Generic;

namespace KH.Framework2D.Utils
{
    /// <summary>
    /// Reactive property wrapper that notifies subscribers when value changes.
    /// Thread-safe and supports value/reference types.
    /// </summary>
    public class ObservableProperty<T>
    {
        private T _value;
        private event Action<T> _onValueChanged;
        
        public T Value
        {
            get => _value;
            set
            {
                // Use EqualityComparer for null-safe comparison (works for both value and reference types)
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                
                _value = value;
                _onValueChanged?.Invoke(_value);
            }
        }
        
        public ObservableProperty(T initialValue = default)
        {
            _value = initialValue;
        }
        
        /// <summary>
        /// Subscribe to value changes.
        /// </summary>
        /// <param name="action">Callback when value changes</param>
        /// <param name="invokeImmediately">If true, callback is invoked with current value immediately</param>
        public void Subscribe(Action<T> action, bool invokeImmediately = true)
        {
            if (action == null) return;
            
            if (invokeImmediately)
            {
                action(_value);
            }
            _onValueChanged += action;
        }
        
        /// <summary>
        /// Unsubscribe from value changes.
        /// </summary>
        public void Unsubscribe(Action<T> action)
        {
            if (action == null) return;
            _onValueChanged -= action;
        }
        
        /// <summary>
        /// Remove all subscriptions. Useful for cleanup.
        /// </summary>
        public void ClearSubscriptions()
        {
            _onValueChanged = null;
        }
        
        /// <summary>
        /// Force notify all subscribers without changing value.
        /// Useful for initial UI sync.
        /// </summary>
        public void NotifySubscribers()
        {
            _onValueChanged?.Invoke(_value);
        }
        
        /// <summary>
        /// Set value without triggering notifications.
        /// Use sparingly - mainly for initialization.
        /// </summary>
        public void SetSilently(T value)
        {
            _value = value;
        }
        
        // Implicit conversion for convenience (e.g., if (myProp) or int x = myProp)
        public static implicit operator T(ObservableProperty<T> property) => property.Value;
        
        public override string ToString() => _value?.ToString() ?? "null";
    }
}
