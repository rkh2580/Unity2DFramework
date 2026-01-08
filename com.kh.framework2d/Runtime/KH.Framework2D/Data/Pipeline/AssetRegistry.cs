using System;
using System.Collections.Generic;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Binds Unity assets (sprites, prefabs, audio) to data IDs.
    /// Separates numeric data (XML) from Unity-specific assets (ScriptableObject).
    /// 
    /// Usage:
    /// 1. Create AssetRegistry ScriptableObject via Create menu
    /// 2. Add entries mapping data IDs to assets
    /// 3. Access via AssetRegistry.GetSprite("card_001")
    /// </summary>
    [CreateAssetMenu(fileName = "AssetRegistry", menuName = "Data/Asset Registry")]
    public class AssetRegistry : ScriptableObject
    {
        [Header("Sprites")]
        [SerializeField] private List<SpriteEntry> _sprites = new();
        
        [Header("Prefabs")]
        [SerializeField] private List<PrefabEntry> _prefabs = new();
        
        [Header("Audio Clips")]
        [SerializeField] private List<AudioEntry> _audioClips = new();
        
        [Header("Animator Controllers")]
        [SerializeField] private List<AnimatorEntry> _animators = new();
        
        // Runtime lookup dictionaries (built on first access)
        private Dictionary<string, Sprite> _spriteDict;
        private Dictionary<string, GameObject> _prefabDict;
        private Dictionary<string, AudioClip> _audioDict;
        private Dictionary<string, RuntimeAnimatorController> _animatorDict;
        
        // Singleton instance (optional - can also use DI)
        private static AssetRegistry _instance;
        public static AssetRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AssetRegistry>("AssetRegistry");
                    if (_instance == null)
                    {
                        Debug.LogError("[AssetRegistry] No AssetRegistry found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        #region Public API
        
        public Sprite GetSprite(string id)
        {
            EnsureSpriteDict();
            return _spriteDict.TryGetValue(id, out var sprite) ? sprite : null;
        }
        
        public Sprite GetSprite(string id, Sprite fallback)
        {
            EnsureSpriteDict();
            return _spriteDict.TryGetValue(id, out var sprite) ? sprite : fallback;
        }
        
        public GameObject GetPrefab(string id)
        {
            EnsurePrefabDict();
            return _prefabDict.TryGetValue(id, out var prefab) ? prefab : null;
        }
        
        public AudioClip GetAudioClip(string id)
        {
            EnsureAudioDict();
            return _audioDict.TryGetValue(id, out var clip) ? clip : null;
        }
        
        public RuntimeAnimatorController GetAnimator(string id)
        {
            EnsureAnimatorDict();
            return _animatorDict.TryGetValue(id, out var animator) ? animator : null;
        }
        
        public bool HasSprite(string id)
        {
            EnsureSpriteDict();
            return _spriteDict.ContainsKey(id);
        }
        
        public bool HasPrefab(string id)
        {
            EnsurePrefabDict();
            return _prefabDict.ContainsKey(id);
        }
        
        #endregion
        
        #region Dictionary Building
        
        private void EnsureSpriteDict()
        {
            if (_spriteDict != null) return;
            
            _spriteDict = new Dictionary<string, Sprite>();
            foreach (var entry in _sprites)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Asset != null)
                {
                    _spriteDict[entry.Id] = entry.Asset;
                }
            }
        }
        
        private void EnsurePrefabDict()
        {
            if (_prefabDict != null) return;
            
            _prefabDict = new Dictionary<string, GameObject>();
            foreach (var entry in _prefabs)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Asset != null)
                {
                    _prefabDict[entry.Id] = entry.Asset;
                }
            }
        }
        
        private void EnsureAudioDict()
        {
            if (_audioDict != null) return;
            
            _audioDict = new Dictionary<string, AudioClip>();
            foreach (var entry in _audioClips)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Asset != null)
                {
                    _audioDict[entry.Id] = entry.Asset;
                }
            }
        }
        
        private void EnsureAnimatorDict()
        {
            if (_animatorDict != null) return;
            
            _animatorDict = new Dictionary<string, RuntimeAnimatorController>();
            foreach (var entry in _animators)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Asset != null)
                {
                    _animatorDict[entry.Id] = entry.Asset;
                }
            }
        }
        
        #endregion
        
        #region Editor Support
        
        /// <summary>
        /// Clear cached dictionaries (call after modifying entries in editor).
        /// </summary>
        public void ClearCache()
        {
            _spriteDict = null;
            _prefabDict = null;
            _audioDict = null;
            _animatorDict = null;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            ClearCache();
        }
#endif
        
        #endregion
        
        #region Entry Types
        
        [Serializable]
        public class SpriteEntry
        {
            public string Id;
            public Sprite Asset;
        }
        
        [Serializable]
        public class PrefabEntry
        {
            public string Id;
            public GameObject Asset;
        }
        
        [Serializable]
        public class AudioEntry
        {
            public string Id;
            public AudioClip Asset;
        }
        
        [Serializable]
        public class AnimatorEntry
        {
            public string Id;
            public RuntimeAnimatorController Asset;
        }
        
        #endregion
    }
}
