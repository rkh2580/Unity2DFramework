using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Utils
{
    /// <summary>
    /// Flexible timer utility supporting countdown, stopwatch, and repeating timers.
    /// </summary>
    public class Timer
    {
        private float _duration;
        private float _remaining;
        private float _elapsed;
        private bool _isRunning;
        private bool _isPaused;
        private bool _useUnscaledTime;
        
        public float Duration => _duration;
        public float Remaining => _remaining;
        public float Elapsed => _elapsed;
        public float Progress => _duration > 0 ? Mathf.Clamp01(_elapsed / _duration) : 0f;
        public float RemainingProgress => 1f - Progress;
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public bool IsComplete => _remaining <= 0 && !_isRunning;
        
        public event Action OnStart;
        public event Action OnComplete;
        public event Action OnPause;
        public event Action OnResume;
        public event Action<float> OnTick; // Passes remaining time
        
        /// <summary>
        /// Create a timer with specified duration.
        /// </summary>
        public Timer(float duration, bool useUnscaledTime = false)
        {
            _duration = duration;
            _remaining = duration;
            _useUnscaledTime = useUnscaledTime;
        }
        
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _isPaused = false;
            _remaining = _duration;
            _elapsed = 0f;
            
            OnStart?.Invoke();
            RunAsync().Forget();
        }
        
        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _isPaused = false;
        }
        
        /// <summary>
        /// Pause the timer.
        /// </summary>
        public void Pause()
        {
            if (!_isRunning || _isPaused) return;
            
            _isPaused = true;
            OnPause?.Invoke();
        }
        
        /// <summary>
        /// Resume the timer.
        /// </summary>
        public void Resume()
        {
            if (!_isRunning || !_isPaused) return;
            
            _isPaused = false;
            OnResume?.Invoke();
        }
        
        /// <summary>
        /// Reset the timer to initial duration.
        /// </summary>
        public void Reset()
        {
            _remaining = _duration;
            _elapsed = 0f;
        }
        
        /// <summary>
        /// Reset and start immediately.
        /// </summary>
        public void Restart()
        {
            Stop();
            Reset();
            Start();
        }
        
        /// <summary>
        /// Set a new duration and reset.
        /// </summary>
        public void SetDuration(float duration)
        {
            _duration = duration;
            Reset();
        }
        
        /// <summary>
        /// Add time to the timer.
        /// </summary>
        public void AddTime(float seconds)
        {
            _remaining += seconds;
            _duration += seconds;
        }
        
        private async UniTaskVoid RunAsync()
        {
            while (_isRunning && _remaining > 0)
            {
                if (!_isPaused)
                {
                    float delta = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    _remaining -= delta;
                    _elapsed += delta;
                    
                    OnTick?.Invoke(_remaining);
                }
                
                await UniTask.Yield();
            }
            
            if (_isRunning)
            {
                _remaining = 0;
                _isRunning = false;
                OnComplete?.Invoke();
            }
        }
        
        /// <summary>
        /// Create and start a timer, returning a UniTask that completes when timer finishes.
        /// </summary>
        public static async UniTask DelayAsync(float seconds, bool useUnscaledTime = false)
        {
            var timer = new Timer(seconds, useUnscaledTime);
            var tcs = new UniTaskCompletionSource();
            
            timer.OnComplete += () => tcs.TrySetResult();
            timer.Start();
            
            await tcs.Task;
        }
    }
    
    /// <summary>
    /// Cooldown timer - useful for skills and abilities.
    /// </summary>
    public class Cooldown
    {
        private float _cooldownTime;
        private float _remainingTime;
        
        public float CooldownTime => _cooldownTime;
        public float RemainingTime => _remainingTime;
        public float Progress => _cooldownTime > 0 ? 1f - (_remainingTime / _cooldownTime) : 1f;
        public bool IsReady => _remainingTime <= 0;
        public bool IsOnCooldown => _remainingTime > 0;
        
        public event Action OnReady;
        public event Action OnStart;
        
        public Cooldown(float cooldownTime)
        {
            _cooldownTime = cooldownTime;
            _remainingTime = 0;
        }
        
        /// <summary>
        /// Try to use the cooldown. Returns true if ready, false if on cooldown.
        /// </summary>
        public bool TryUse()
        {
            if (!IsReady) return false;
            
            Use();
            return true;
        }
        
        /// <summary>
        /// Force use (starts cooldown regardless of ready state).
        /// </summary>
        public void Use()
        {
            _remainingTime = _cooldownTime;
            OnStart?.Invoke();
            UpdateAsync().Forget();
        }
        
        /// <summary>
        /// Update cooldown each frame (call in Update or use auto-update).
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_remainingTime > 0)
            {
                _remainingTime -= deltaTime;
                
                if (_remainingTime <= 0)
                {
                    _remainingTime = 0;
                    OnReady?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Reset cooldown to ready state.
        /// </summary>
        public void Reset()
        {
            _remainingTime = 0;
        }
        
        /// <summary>
        /// Set new cooldown time.
        /// </summary>
        public void SetCooldown(float time)
        {
            _cooldownTime = time;
        }
        
        /// <summary>
        /// Reduce remaining cooldown by amount.
        /// </summary>
        public void ReduceCooldown(float amount)
        {
            _remainingTime = Mathf.Max(0, _remainingTime - amount);
        }
        
        private async UniTaskVoid UpdateAsync()
        {
            while (_remainingTime > 0)
            {
                await UniTask.Yield();
                _remainingTime -= Time.deltaTime;
            }
            
            _remainingTime = 0;
            OnReady?.Invoke();
        }
    }
    
    /// <summary>
    /// Repeating timer that fires at intervals.
    /// </summary>
    public class RepeatingTimer
    {
        private float _interval;
        private float _elapsed;
        private bool _isRunning;
        private bool _useUnscaledTime;
        
        public float Interval => _interval;
        public bool IsRunning => _isRunning;
        
        public event Action OnTick;
        
        public RepeatingTimer(float interval, bool useUnscaledTime = false)
        {
            _interval = interval;
            _useUnscaledTime = useUnscaledTime;
        }
        
        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _elapsed = 0f;
            RunAsync().Forget();
        }
        
        public void Stop()
        {
            _isRunning = false;
        }
        
        public void SetInterval(float interval)
        {
            _interval = interval;
        }
        
        private async UniTaskVoid RunAsync()
        {
            while (_isRunning)
            {
                await UniTask.Yield();
                
                float delta = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                _elapsed += delta;
                
                if (_elapsed >= _interval)
                {
                    _elapsed -= _interval;
                    OnTick?.Invoke();
                }
            }
        }
    }
}
