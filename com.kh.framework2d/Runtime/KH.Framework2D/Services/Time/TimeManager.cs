using System;
using UnityEngine;

namespace KH.Framework2D.Services.Time
{
    /// <summary>
    /// Time management service for pause, slow-motion, and time scale control.
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeService
    {
        [SerializeField] private float _defaultTimeScale = 1f;
        [SerializeField] private float _pausedTimeScale = 0f;
        
        private float _previousTimeScale;
        
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
        public float FixedDeltaTime => UnityEngine.Time.fixedDeltaTime;
        public float TotalTime => UnityEngine.Time.time;
        public float UnscaledTotalTime => UnityEngine.Time.unscaledTime;
        
        public float TimeScale
        {
            get => UnityEngine.Time.timeScale;
            set => UnityEngine.Time.timeScale = Mathf.Max(0f, value);
        }
        
        public bool IsPaused { get; private set; }
        
        public event Action OnPaused;
        public event Action OnResumed;
        public event Action<float> OnTimeScaleChanged;
        
        private void Awake()
        {
            TimeScale = _defaultTimeScale;
        }
        
        /// <summary>
        /// Pause the game (sets timeScale to 0).
        /// </summary>
        public void Pause()
        {
            if (IsPaused) return;
            
            _previousTimeScale = TimeScale;
            TimeScale = _pausedTimeScale;
            IsPaused = true;
            
            OnPaused?.Invoke();
        }
        
        /// <summary>
        /// Resume the game (restores previous timeScale).
        /// </summary>
        public void Resume()
        {
            if (!IsPaused) return;
            
            TimeScale = _previousTimeScale;
            IsPaused = false;
            
            OnResumed?.Invoke();
        }
        
        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }
        
        /// <summary>
        /// Set time scale with event notification.
        /// </summary>
        public void SetTimeScale(float scale)
        {
            TimeScale = scale;
            OnTimeScaleChanged?.Invoke(scale);
        }
        
        /// <summary>
        /// Slow motion effect.
        /// </summary>
        public void SlowMotion(float scale = 0.3f)
        {
            SetTimeScale(scale);
        }
        
        /// <summary>
        /// Reset to default time scale.
        /// </summary>
        public void ResetTimeScale()
        {
            SetTimeScale(_defaultTimeScale);
            IsPaused = false;
        }
        
        private void OnDestroy()
        {
            // Ensure time scale is reset when manager is destroyed
            UnityEngine.Time.timeScale = 1f;
        }
    }
}
