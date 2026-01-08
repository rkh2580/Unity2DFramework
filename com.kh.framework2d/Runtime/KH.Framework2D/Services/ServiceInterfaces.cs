using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Services
{
    /// <summary>
    /// Audio service interface.
    /// </summary>
    public interface IAudioService
    {
        // SFX
        void PlaySFX(string key);
        void PlaySFX(AudioClip clip);
        void PlaySFX(string key, Vector3 position);
        void PlaySFX(AudioClip clip, Vector3 position);
        void PlaySFXWithPitch(string key, float minPitch = 0.9f, float maxPitch = 1.1f);
        
        // BGM
        void PlayBGM(string key, bool loop = true);
        void PlayBGM(AudioClip clip, bool loop = true);
        void StopBGM();
        void PauseBGM();
        void ResumeBGM();
        UniTask CrossfadeBGM(string key, float duration = -1);
        UniTask CrossfadeBGM(AudioClip newClip, float duration = -1);
        
        // Volume (0-1)
        void SetMasterVolume(float volume);
        void SetBGMVolume(float volume);
        void SetSFXVolume(float volume);
        
        float MasterVolume { get; }
        float BGMVolume { get; }
        float SFXVolume { get; }
        bool IsBGMPlaying { get; }
        
        // Clip management
        void RegisterClip(string key, AudioClip clip);
    }
    
    /// <summary>
    /// Scene loading service interface.
    /// </summary>
    public interface ISceneService
    {
        UniTask LoadSceneAsync(string sceneName, bool showLoadingScreen = true);
        UniTask LoadSceneAdditiveAsync(string sceneName);
        UniTask UnloadSceneAsync(string sceneName);
        UniTask ReloadCurrentSceneAsync();
        
        string CurrentSceneName { get; }
        bool IsLoading { get; }
        bool IsSceneLoaded(string sceneName);
        void SetActiveScene(string sceneName);
        
        event Action<float> OnLoadProgress;
        event Action<string> OnSceneLoaded;
        event Action<string> OnSceneUnloaded;
    }
    
    /// <summary>
    /// Save/Load service interface.
    /// </summary>
    public interface ISaveService
    {
        // PlayerPrefs (simple data)
        void Save<T>(string key, T data);
        T Load<T>(string key, T defaultValue = default);
        bool HasKey(string key);
        void Delete(string key);
        void DeleteAll();
        
        // File-based (complex data)
        void SaveToFile<T>(string fileName, T data);
        T LoadFromFile<T>(string fileName, T defaultValue = default);
        bool FileExists(string fileName);
        void DeleteFile(string fileName);
        string[] GetAllSaveFiles();
    }
    
    /// <summary>
    /// Input service interface.
    /// </summary>
    public interface IInputService
    {
        // Movement
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        
        // Actions (single frame)
        bool AttackPressed { get; }
        bool SkillPressed { get; }
        bool UltimatePressed { get; }
        bool InteractPressed { get; }
        bool JumpPressed { get; }
        bool DashPressed { get; }
        
        // Actions (held)
        bool AttackHeld { get; }
        
        // UI
        bool PausePressed { get; }
        bool InventoryPressed { get; }
        bool CancelPressed { get; }
        bool ConfirmPressed { get; }
        
        // Mouse
        Vector2 MousePosition { get; }
        bool MouseLeftPressed { get; }
        bool MouseRightPressed { get; }
        
        // State
        bool InputEnabled { get; }
        void EnableInput();
        void DisableInput();
        void SwitchToUI();
        void SwitchToGameplay();
        
        // Utility
        Vector2 GetMouseWorldPosition2D();
        Vector3 GetMouseWorldPosition3D(float zDepth = 10f);
        Vector2 GetDirectionToMouse(Vector2 fromPosition);
        
        // Events
        event Action OnAttack;
        event Action OnSkill;
        event Action OnUltimate;
        event Action OnInteract;
        event Action OnJump;
        event Action OnDash;
        event Action OnPause;
        event Action OnInventory;
    }
    
    /// <summary>
    /// Time service interface.
    /// </summary>
    public interface ITimeService
    {
        float DeltaTime { get; }
        float UnscaledDeltaTime { get; }
        float FixedDeltaTime { get; }
        float TotalTime { get; }
        float UnscaledTotalTime { get; }
        float TimeScale { get; set; }
        bool IsPaused { get; }
        
        void Pause();
        void Resume();
        void TogglePause();
        void SetTimeScale(float scale);
        void SlowMotion(float scale = 0.3f);
        void ResetTimeScale();
        
        event Action OnPaused;
        event Action OnResumed;
        event Action<float> OnTimeScaleChanged;
    }
}
