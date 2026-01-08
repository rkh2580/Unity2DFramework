using System;
using UnityEngine;

namespace KH.Framework2D.Events
{
    /// <summary>
    /// Base class for all event channels.
    /// Event channels are ScriptableObjects that act as decoupled communication bridges.
    /// </summary>
    public abstract class BaseEventChannel : ScriptableObject
    {
        [TextArea]
        [SerializeField] private string _description;
        
#if UNITY_EDITOR
        // Editor-only: track listener count for debugging
        public abstract int ListenerCount { get; }
#endif
    }
    
    /// <summary>
    /// Event channel with no parameters.
    /// Use for: game start, pause, resume, quit, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Void Event Channel", fileName = "VoidEventChannel")]
    public class VoidEventChannel : BaseEventChannel
    {
        private event Action _onEventRaised;
        
        public void Raise()
        {
            _onEventRaised?.Invoke();
            
#if UNITY_EDITOR
            Debug.Log($"[Event] {name} raised");
#endif
        }
        
        public void Subscribe(Action listener)
        {
            if (listener != null)
                _onEventRaised += listener;
        }
        
        public void Unsubscribe(Action listener)
        {
            if (listener != null)
                _onEventRaised -= listener;
        }
        
#if UNITY_EDITOR
        public override int ListenerCount => _onEventRaised?.GetInvocationList().Length ?? 0;
#endif
    }
    
    /// <summary>
    /// Generic event channel with one parameter.
    /// </summary>
    public abstract class EventChannel<T> : BaseEventChannel
    {
        private event Action<T> _onEventRaised;
        
        public void Raise(T value)
        {
            _onEventRaised?.Invoke(value);
            
#if UNITY_EDITOR
            Debug.Log($"[Event] {name} raised with: {value}");
#endif
        }
        
        public void Subscribe(Action<T> listener)
        {
            if (listener != null)
                _onEventRaised += listener;
        }
        
        public void Unsubscribe(Action<T> listener)
        {
            if (listener != null)
                _onEventRaised -= listener;
        }
        
#if UNITY_EDITOR
        public override int ListenerCount => _onEventRaised?.GetInvocationList().Length ?? 0;
#endif
    }
    
    /// <summary>
    /// Generic event channel with two parameters.
    /// </summary>
    public abstract class EventChannel<T1, T2> : BaseEventChannel
    {
        private event Action<T1, T2> _onEventRaised;
        
        public void Raise(T1 value1, T2 value2)
        {
            _onEventRaised?.Invoke(value1, value2);
            
#if UNITY_EDITOR
            Debug.Log($"[Event] {name} raised with: ({value1}, {value2})");
#endif
        }
        
        public void Subscribe(Action<T1, T2> listener)
        {
            if (listener != null)
                _onEventRaised += listener;
        }
        
        public void Unsubscribe(Action<T1, T2> listener)
        {
            if (listener != null)
                _onEventRaised -= listener;
        }
        
#if UNITY_EDITOR
        public override int ListenerCount => _onEventRaised?.GetInvocationList().Length ?? 0;
#endif
    }
}
