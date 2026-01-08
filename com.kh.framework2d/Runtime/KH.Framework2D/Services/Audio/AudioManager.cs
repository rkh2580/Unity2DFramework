using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace KH.Framework2D.Services.Audio
{
    /// <summary>
    /// Complete audio management system.
    /// Supports BGM, SFX, pooled audio sources, and volume control.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer (Optional)")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _bgmVolumeParam = "BGMVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;
        
        [Header("Settings")]
        [SerializeField] private int _sfxPoolSize = 10;
        [SerializeField] private float _defaultFadeDuration = 0.5f;
        
        [Header("Audio Clips (Optional - can use Resources)")]
        [SerializeField] private List<AudioClipEntry> _audioClips = new();
        
        // Clip dictionary for quick lookup
        private readonly Dictionary<string, AudioClip> _clipCache = new();
        
        // SFX pool for overlapping sounds
        private readonly List<AudioSource> _sfxPool = new();
        private int _sfxPoolIndex;
        
        // Volume (0-1)
        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        
        public float MasterVolume => _masterVolume;
        public float BGMVolume => _bgmVolume;
        public float SFXVolume => _sfxVolume;
        public bool IsBGMPlaying => _bgmSource != null && _bgmSource.isPlaying;
        
        private void Awake()
        {
            InitializeSFXPool();
            CacheAudioClips();
            LoadVolumeSettings();
        }
        
        #region Initialization
        
        private void InitializeSFXPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxPool.Add(source);
            }
        }
        
        private void CacheAudioClips()
        {
            foreach (var entry in _audioClips)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.clip != null)
                {
                    _clipCache[entry.key] = entry.clip;
                }
            }
        }
        
        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("Audio_Master", 1f);
            _bgmVolume = PlayerPrefs.GetFloat("Audio_BGM", 1f);
            _sfxVolume = PlayerPrefs.GetFloat("Audio_SFX", 1f);
            
            ApplyVolumeToMixer();
        }
        
        #endregion
        
        #region SFX
        
        public void PlaySFX(string key)
        {
            var clip = GetClip(key);
            if (clip != null)
            {
                PlaySFX(clip);
            }
        }
        
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            
            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume;
            source.Play();
        }
        
        public void PlaySFX(string key, Vector3 position)
        {
            var clip = GetClip(key);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, _sfxVolume * _masterVolume);
            }
        }
        
        public void PlaySFX(AudioClip clip, Vector3 position)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, _sfxVolume * _masterVolume);
            }
        }
        
        /// <summary>
        /// Play SFX with pitch variation for variety.
        /// </summary>
        public void PlaySFXWithPitch(string key, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            var clip = GetClip(key);
            if (clip == null) return;
            
            var source = GetNextSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume;
            source.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            source.Play();
        }
        
        private AudioSource GetNextSFXSource()
        {
            var source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            source.pitch = 1f; // Reset pitch
            return source;
        }
        
        #endregion
        
        #region BGM
        
        public void PlayBGM(string key, bool loop = true)
        {
            var clip = GetClip(key);
            if (clip != null)
            {
                PlayBGM(clip, loop);
            }
        }
        
        public void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (_bgmSource == null || clip == null) return;
            
            _bgmSource.clip = clip;
            _bgmSource.loop = loop;
            _bgmSource.volume = _bgmVolume * _masterVolume;
            _bgmSource.Play();
        }
        
        public void StopBGM()
        {
            if (_bgmSource != null)
            {
                _bgmSource.Stop();
            }
        }
        
        public void PauseBGM()
        {
            if (_bgmSource != null)
            {
                _bgmSource.Pause();
            }
        }
        
        public void ResumeBGM()
        {
            if (_bgmSource != null)
            {
                _bgmSource.UnPause();
            }
        }
        
        /// <summary>
        /// Crossfade to a new BGM track.
        /// </summary>
        public async UniTask CrossfadeBGM(string key, float duration = -1)
        {
            var clip = GetClip(key);
            if (clip != null)
            {
                await CrossfadeBGM(clip, duration);
            }
        }
        
        public async UniTask CrossfadeBGM(AudioClip newClip, float duration = -1)
        {
            if (_bgmSource == null || newClip == null) return;
            
            duration = duration < 0 ? _defaultFadeDuration : duration;
            float targetVolume = _bgmVolume * _masterVolume;
            
            // Fade out current
            float elapsed = 0f;
            float startVolume = _bgmSource.volume;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                await UniTask.Yield();
            }
            
            // Switch clip
            _bgmSource.clip = newClip;
            _bgmSource.Play();
            
            // Fade in new
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
                await UniTask.Yield();
            }
            
            _bgmSource.volume = targetVolume;
        }
        
        #endregion
        
        #region Volume Control
        
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer();
            PlayerPrefs.SetFloat("Audio_Master", _masterVolume);
        }
        
        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer();
            
            if (_bgmSource != null)
            {
                _bgmSource.volume = _bgmVolume * _masterVolume;
            }
            
            PlayerPrefs.SetFloat("Audio_BGM", _bgmVolume);
        }
        
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeToMixer();
            PlayerPrefs.SetFloat("Audio_SFX", _sfxVolume);
        }
        
        private void ApplyVolumeToMixer()
        {
            if (_audioMixer == null) return;
            
            // Convert linear to decibels (0 = -80dB, 1 = 0dB)
            _audioMixer.SetFloat(_masterVolumeParam, LinearToDecibel(_masterVolume));
            _audioMixer.SetFloat(_bgmVolumeParam, LinearToDecibel(_bgmVolume));
            _audioMixer.SetFloat(_sfxVolumeParam, LinearToDecibel(_sfxVolume));
        }
        
        private float LinearToDecibel(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
        }
        
        #endregion
        
        #region Clip Management
        
        private AudioClip GetClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            
            // Try cache first
            if (_clipCache.TryGetValue(key, out var clip))
            {
                return clip;
            }
            
            // Try Resources
            clip = Resources.Load<AudioClip>($"Audio/{key}");
            if (clip != null)
            {
                _clipCache[key] = clip;
                return clip;
            }
            
            Debug.LogWarning($"[AudioManager] Clip not found: {key}");
            return null;
        }
        
        /// <summary>
        /// Register a clip at runtime.
        /// </summary>
        public void RegisterClip(string key, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(key) && clip != null)
            {
                _clipCache[key] = clip;
            }
        }
        
        #endregion
    }
    
    [Serializable]
    public class AudioClipEntry
    {
        public string key;
        public AudioClip clip;
    }
}
