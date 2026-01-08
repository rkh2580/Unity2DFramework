using System;
using KH.Framework2D.StateMachine;
using UnityEngine;

namespace KH.Framework2D.Services.Game
{
    /// <summary>
    /// Base game manager with state machine.
    /// Inherit this for your specific game implementation.
    /// </summary>
    public abstract class GameManager<TGameManager> : MonoBehaviour 
        where TGameManager : GameManager<TGameManager>
    {
        public static TGameManager Instance { get; private set; }
        
        [Header("Game Settings")]
        [SerializeField] protected bool _persistAcrossScenes = true;
        
        // Game state machine
        protected StateMachine<TGameManager> _stateMachine;
        
        public GameState CurrentState { get; protected set; } = GameState.None;
        
        public event Action<GameState, GameState> OnStateChanged; // old, new
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnGameEnded; // isWin
        
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = (TGameManager)this;
            
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            InitializeStateMachine();
        }
        
        protected virtual void Update()
        {
            _stateMachine?.Update(Time.deltaTime);
        }
        
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// Override to set up your game's state machine.
        /// </summary>
        protected abstract void InitializeStateMachine();
        
        #region State Changes
        
        /// <summary>
        /// Change to a new game state.
        /// </summary>
        protected void ChangeState(GameState newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }
        
        /// <summary>
        /// Start the game.
        /// </summary>
        public virtual void StartGame()
        {
            ChangeState(GameState.Playing);
            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// Pause the game.
        /// </summary>
        public virtual void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
        
        /// <summary>
        /// Resume the game.
        /// </summary>
        public virtual void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
        
        /// <summary>
        /// Toggle pause.
        /// </summary>
        public virtual void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
        
        /// <summary>
        /// End the game.
        /// </summary>
        public virtual void EndGame(bool isWin)
        {
            ChangeState(isWin ? GameState.Win : GameState.Lose);
            Time.timeScale = 1f;
            OnGameEnded?.Invoke(isWin);
        }
        
        /// <summary>
        /// Restart the game.
        /// </summary>
        public abstract void RestartGame();
        
        /// <summary>
        /// Return to main menu.
        /// </summary>
        public abstract void ReturnToMenu();
        
        #endregion
    }
    
    /// <summary>
    /// Common game states.
    /// </summary>
    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        Playing,
        Paused,
        Win,
        Lose,
        GameOver
    }
    
    /// <summary>
    /// Simple game manager without generics for quick prototyping.
    /// </summary>
    public class SimpleGameManager : MonoBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private bool _persistAcrossScenes = true;
        
        public GameState CurrentState { get; private set; } = GameState.None;
        
        public event Action<GameState, GameState> OnStateChanged;
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<bool> OnGameEnded;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        
        public void ChangeState(GameState newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
        }
        
        public void StartGame()
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            OnGameStarted?.Invoke();
        }
        
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            
            ChangeState(GameState.Paused);
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
        
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            
            ChangeState(GameState.Playing);
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
        
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
        
        public void EndGame(bool isWin)
        {
            ChangeState(isWin ? GameState.Win : GameState.Lose);
            Time.timeScale = 1f;
            OnGameEnded?.Invoke(isWin);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
