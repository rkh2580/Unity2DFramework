using KH.Framework2D.Services.Localization;
using TMPro;
using UnityEngine;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// Automatically localizes TextMeshPro text components.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private string _key;
        [SerializeField] private bool _updateOnLanguageChange = true;
        
        private TextMeshProUGUI _text;
        private object[] _formatArgs;
        
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                UpdateText();
            }
        }
        
        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }
        
        private void OnEnable()
        {
            if (_updateOnLanguageChange)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
            
            UpdateText();
        }
        
        private void OnDisable()
        {
            if (_updateOnLanguageChange)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }
        
        /// <summary>
        /// Set the localization key and update text.
        /// </summary>
        public void SetKey(string key)
        {
            _key = key;
            _formatArgs = null;
            UpdateText();
        }
        
        /// <summary>
        /// Set key with format arguments.
        /// </summary>
        public void SetKey(string key, params object[] args)
        {
            _key = key;
            _formatArgs = args;
            UpdateText();
        }
        
        /// <summary>
        /// Update format arguments and refresh text.
        /// </summary>
        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            UpdateText();
        }
        
        private void UpdateText()
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;
            
            if (_formatArgs != null && _formatArgs.Length > 0)
            {
                _text.text = LocalizationManager.Instance.Get(_key, _formatArgs);
            }
            else
            {
                _text.text = LocalizationManager.Instance.Get(_key);
            }
        }
        
        private void OnLanguageChanged(string language)
        {
            UpdateText();
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Preview in editor
            if (!Application.isPlaying && !string.IsNullOrEmpty(_key))
            {
                var text = GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"[{_key}]";
                }
            }
        }
#endif
    }
}
