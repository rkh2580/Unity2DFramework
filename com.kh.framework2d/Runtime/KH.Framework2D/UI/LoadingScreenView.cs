using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// Loading screen UI component.
    /// Can be used standalone or with SceneLoader.
    /// </summary>
    public class LoadingScreenView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _tipText;
        [SerializeField] private GameObject _loadingIndicator;
        
        [Header("Animation")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _minimumDisplayTime = 1f;
        
        [Header("Tips (Optional)")]
        [SerializeField] private string[] _loadingTips;
        [SerializeField] private float _tipChangeInterval = 3f;
        
        private float _showTime;
        private bool _isVisible;
        
        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            
            // Start hidden
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Show the loading screen.
        /// </summary>
        public async UniTask ShowAsync()
        {
            if (_isVisible) return;
            
            _isVisible = true;
            _showTime = Time.unscaledTime;
            
            gameObject.SetActive(true);
            SetProgress(0f);
            ShowRandomTip();
            
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(true);
            
            // Fade in
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                await _canvasGroup.DOFade(1f, _fadeInDuration)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
            }
            
            // Start tip rotation
            if (_loadingTips != null && _loadingTips.Length > 1)
            {
                RotateTips().Forget();
            }
        }
        
        /// <summary>
        /// Hide the loading screen.
        /// </summary>
        public async UniTask HideAsync()
        {
            if (!_isVisible) return;
            
            // Ensure minimum display time
            float elapsed = Time.unscaledTime - _showTime;
            if (elapsed < _minimumDisplayTime)
            {
                await UniTask.Delay(
                    (int)((_minimumDisplayTime - elapsed) * 1000),
                    ignoreTimeScale: true
                );
            }
            
            SetProgress(1f);
            await UniTask.Delay(200, ignoreTimeScale: true);
            
            // Fade out
            if (_canvasGroup != null)
            {
                await _canvasGroup.DOFade(0f, _fadeOutDuration)
                    .SetUpdate(true)
                    .AsyncWaitForCompletion();
                
                _canvasGroup.blocksRaycasts = false;
            }
            
            _isVisible = false;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Show instantly (no animation).
        /// </summary>
        public void ShowInstant()
        {
            _isVisible = true;
            _showTime = Time.unscaledTime;
            gameObject.SetActive(true);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
            
            SetProgress(0f);
            ShowRandomTip();
        }
        
        /// <summary>
        /// Hide instantly (no animation).
        /// </summary>
        public void HideInstant()
        {
            _isVisible = false;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Set loading progress (0-1).
        /// </summary>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            if (_progressFill != null)
            {
                _progressFill.fillAmount = progress;
            }
            
            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }
        
        /// <summary>
        /// Set the tip text.
        /// </summary>
        public void SetTip(string tip)
        {
            if (_tipText != null)
            {
                _tipText.text = tip;
            }
        }
        
        /// <summary>
        /// Show a random tip from the list.
        /// </summary>
        public void ShowRandomTip()
        {
            if (_loadingTips != null && _loadingTips.Length > 0 && _tipText != null)
            {
                _tipText.text = _loadingTips[Random.Range(0, _loadingTips.Length)];
            }
        }
        
        private async UniTaskVoid RotateTips()
        {
            while (_isVisible)
            {
                await UniTask.Delay(
                    (int)(_tipChangeInterval * 1000),
                    ignoreTimeScale: true
                );
                
                if (_isVisible && _tipText != null)
                {
                    // Fade out
                    await _tipText.DOFade(0f, 0.2f).SetUpdate(true).AsyncWaitForCompletion();
                    
                    ShowRandomTip();
                    
                    // Fade in
                    await _tipText.DOFade(1f, 0.2f).SetUpdate(true).AsyncWaitForCompletion();
                }
            }
        }
    }
}
