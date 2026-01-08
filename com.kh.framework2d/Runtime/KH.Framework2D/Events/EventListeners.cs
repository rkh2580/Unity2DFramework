using UnityEngine;
using UnityEngine.Events;

namespace KH.Framework2D.Events
{
    /// <summary>
    /// Listens to a VoidEventChannel and invokes UnityEvent response.
    /// Attach to any GameObject to wire up events in the Inspector.
    /// </summary>
    public class VoidEventListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _eventChannel;
        [SerializeField] private UnityEvent _response;
        
        private void OnEnable()
        {
            if (_eventChannel != null)
                _eventChannel.Subscribe(OnEventRaised);
        }
        
        private void OnDisable()
        {
            if (_eventChannel != null)
                _eventChannel.Unsubscribe(OnEventRaised);
        }
        
        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
    
    /// <summary>
    /// Listens to an IntEventChannel and invokes UnityEvent response with int parameter.
    /// </summary>
    public class IntEventListener : MonoBehaviour
    {
        [SerializeField] private IntEventChannel _eventChannel;
        [SerializeField] private UnityEvent<int> _response;
        
        private void OnEnable()
        {
            if (_eventChannel != null)
                _eventChannel.Subscribe(OnEventRaised);
        }
        
        private void OnDisable()
        {
            if (_eventChannel != null)
                _eventChannel.Unsubscribe(OnEventRaised);
        }
        
        private void OnEventRaised(int value)
        {
            _response?.Invoke(value);
        }
    }
    
    /// <summary>
    /// Listens to a FloatEventChannel and invokes UnityEvent response with float parameter.
    /// </summary>
    public class FloatEventListener : MonoBehaviour
    {
        [SerializeField] private FloatEventChannel _eventChannel;
        [SerializeField] private UnityEvent<float> _response;
        
        private void OnEnable()
        {
            if (_eventChannel != null)
                _eventChannel.Subscribe(OnEventRaised);
        }
        
        private void OnDisable()
        {
            if (_eventChannel != null)
                _eventChannel.Unsubscribe(OnEventRaised);
        }
        
        private void OnEventRaised(float value)
        {
            _response?.Invoke(value);
        }
    }
    
    /// <summary>
    /// Listens to a StringEventChannel and invokes UnityEvent response with string parameter.
    /// </summary>
    public class StringEventListener : MonoBehaviour
    {
        [SerializeField] private StringEventChannel _eventChannel;
        [SerializeField] private UnityEvent<string> _response;
        
        private void OnEnable()
        {
            if (_eventChannel != null)
                _eventChannel.Subscribe(OnEventRaised);
        }
        
        private void OnDisable()
        {
            if (_eventChannel != null)
                _eventChannel.Unsubscribe(OnEventRaised);
        }
        
        private void OnEventRaised(string value)
        {
            _response?.Invoke(value);
        }
    }
    
    /// <summary>
    /// Listens to a BoolEventChannel and invokes UnityEvent response with bool parameter.
    /// </summary>
    public class BoolEventListener : MonoBehaviour
    {
        [SerializeField] private BoolEventChannel _eventChannel;
        [SerializeField] private UnityEvent<bool> _response;
        
        private void OnEnable()
        {
            if (_eventChannel != null)
                _eventChannel.Subscribe(OnEventRaised);
        }
        
        private void OnDisable()
        {
            if (_eventChannel != null)
                _eventChannel.Unsubscribe(OnEventRaised);
        }
        
        private void OnEventRaised(bool value)
        {
            _response?.Invoke(value);
        }
    }
}
