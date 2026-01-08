using System;
using System.Collections.Generic;
using UnityEngine;

namespace KH.Framework2D.StateMachine
{
    /// <summary>
    /// Base interface for all states.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }
    
    /// <summary>
    /// Generic state with owner reference.
    /// Owner is the MonoBehaviour or data class that owns this state.
    /// </summary>
    public abstract class State<TOwner> : IState
    {
        protected TOwner Owner { get; private set; }
        protected StateMachine<TOwner> StateMachine { get; private set; }
        
        public void Initialize(TOwner owner, StateMachine<TOwner> stateMachine)
        {
            Owner = owner;
            StateMachine = stateMachine;
            OnInitialize();
        }
        
        protected virtual void OnInitialize() { }
        
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
    
    /// <summary>
    /// Generic state machine implementation.
    /// Manages state transitions and updates.
    /// </summary>
    public class StateMachine<TOwner>
    {
        private readonly TOwner _owner;
        private readonly Dictionary<Type, IState> _states = new();
        
        private IState _currentState;
        private IState _previousState;
        
        public IState CurrentState => _currentState;
        public IState PreviousState => _previousState;
        public Type CurrentStateType => _currentState?.GetType();
        
        // Events
        public event Action<IState, IState> OnStateChanged; // (from, to)
        
        public StateMachine(TOwner owner)
        {
            _owner = owner;
        }
        
        /// <summary>
        /// Register a state type. Will instantiate and initialize it.
        /// </summary>
        public void AddState<TState>() where TState : State<TOwner>, new()
        {
            var state = new TState();
            state.Initialize(_owner, this);
            _states[typeof(TState)] = state;
        }
        
        /// <summary>
        /// Register multiple states at once.
        /// </summary>
        public void AddStates(params Type[] stateTypes)
        {
            foreach (var type in stateTypes)
            {
                if (!typeof(State<TOwner>).IsAssignableFrom(type))
                {
                    Debug.LogError($"[StateMachine] {type.Name} is not a valid State<{typeof(TOwner).Name}>");
                    continue;
                }
                
                var state = (State<TOwner>)Activator.CreateInstance(type);
                state.Initialize(_owner, this);
                _states[type] = state;
            }
        }
        
        /// <summary>
        /// Transition to a new state.
        /// </summary>
        public void ChangeState<TState>() where TState : State<TOwner>
        {
            ChangeState(typeof(TState));
        }
        
        /// <summary>
        /// Transition to a new state by type.
        /// </summary>
        public void ChangeState(Type stateType)
        {
            if (!_states.TryGetValue(stateType, out var newState))
            {
                Debug.LogError($"[StateMachine] State {stateType.Name} not found!");
                return;
            }
            
            if (_currentState == newState)
                return;
            
            _previousState = _currentState;
            _currentState?.Exit();
            
            _currentState = newState;
            _currentState.Enter();
            
            OnStateChanged?.Invoke(_previousState, _currentState);
        }
        
        /// <summary>
        /// Return to the previous state.
        /// </summary>
        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                ChangeState(_previousState.GetType());
            }
        }
        
        /// <summary>
        /// Check if currently in a specific state.
        /// </summary>
        public bool IsInState<TState>() where TState : IState
        {
            return _currentState is TState;
        }
        
        /// <summary>
        /// Get a registered state instance.
        /// </summary>
        public TState GetState<TState>() where TState : State<TOwner>
        {
            return _states.TryGetValue(typeof(TState), out var state) ? (TState)state : null;
        }
        
        /// <summary>
        /// Call from MonoBehaviour.Update()
        /// </summary>
        public void Update()
        {
            _currentState?.Update();
        }
        
        /// <summary>
        /// Call from MonoBehaviour.FixedUpdate()
        /// </summary>
        public void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }
    }
}
