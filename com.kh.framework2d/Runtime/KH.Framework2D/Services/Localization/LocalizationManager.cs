using System;
using System.Collections.Generic;
using UnityEngine;

namespace KH.Framework2D.Services.Localization
{
    /// <summary>
    /// Simple localization system using ScriptableObject dictionaries.
    /// </summary>
    public class LocalizationManager
    {
        private static LocalizationManager _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();
        
        private readonly Dictionary<string, string> _localizedTexts = new();
        private string _currentLanguage = "en";
        private LocalizationData _currentData;
        
        public string CurrentLanguage => _currentLanguage;
        public event Action<string> OnLanguageChanged;
        
        /// <summary>
        /// Load localization data for a language.
        /// </summary>
        public void LoadLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            _localizedTexts.Clear();
            
            // Try to load from Resources
            var data = Resources.Load<LocalizationData>($"Localization/{languageCode}");
            
            if (data == null)
            {
                Debug.LogWarning($"[Localization] Language data not found: {languageCode}");
                return;
            }
            
            LoadData(data);
        }
        
        /// <summary>
        /// Load localization from a ScriptableObject.
        /// </summary>
        public void LoadData(LocalizationData data)
        {
            if (data == null) return;
            
            _currentData = data;
            _currentLanguage = data.LanguageCode;
            _localizedTexts.Clear();
            
            foreach (var entry in data.Entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    _localizedTexts[entry.key] = entry.value;
                }
            }
            
            OnLanguageChanged?.Invoke(_currentLanguage);
            Debug.Log($"[Localization] Loaded language: {_currentLanguage} ({_localizedTexts.Count} entries)");
        }
        
        /// <summary>
        /// Get localized text by key.
        /// </summary>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            
            if (_localizedTexts.TryGetValue(key, out var text))
                return text;
            
            // Return key as fallback
            Debug.LogWarning($"[Localization] Key not found: {key}");
            return $"[{key}]";
        }
        
        /// <summary>
        /// Get localized text with format arguments.
        /// </summary>
        public string Get(string key, params object[] args)
        {
            string text = Get(key);
            
            try
            {
                return string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }
        
        /// <summary>
        /// Check if key exists.
        /// </summary>
        public bool HasKey(string key)
        {
            return _localizedTexts.ContainsKey(key);
        }
        
        /// <summary>
        /// Add or update a localized text at runtime.
        /// </summary>
        public void Set(string key, string value)
        {
            _localizedTexts[key] = value;
        }
        
        /// <summary>
        /// Get all available languages from Resources.
        /// </summary>
        public string[] GetAvailableLanguages()
        {
            var languages = new List<string>();
            var allData = Resources.LoadAll<LocalizationData>("Localization");
            
            foreach (var data in allData)
            {
                languages.Add(data.LanguageCode);
            }
            
            return languages.ToArray();
        }
    }
    
    /// <summary>
    /// ScriptableObject containing localization entries.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Game Data/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [Header("Language")]
        [SerializeField] private string _languageCode = "en";
        [SerializeField] private string _languageName = "English";
        
        [Header("Entries")]
        [SerializeField] private List<LocalizationEntry> _entries = new();
        
        public string LanguageCode => _languageCode;
        public string LanguageName => _languageName;
        public IReadOnlyList<LocalizationEntry> Entries => _entries;
    }
    
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(1, 5)]
        public string value;
    }
    
    /// <summary>
    /// Static helper for quick access.
    /// </summary>
    public static class Localization
    {
        public static string Get(string key) => LocalizationManager.Instance.Get(key);
        public static string Get(string key, params object[] args) => LocalizationManager.Instance.Get(key, args);
        public static void Load(string languageCode) => LocalizationManager.Instance.LoadLanguage(languageCode);
    }
}
