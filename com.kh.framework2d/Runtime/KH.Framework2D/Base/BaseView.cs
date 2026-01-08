using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace KH.Framework2D.Base
{
    /// <summary>
    /// Base class for all UI views.
    /// Provides show/hide functionality with optional animations.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseView : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        
        private CanvasGroup _canvasGroup;
        
        protected CanvasGroup CanvasGroup => _canvasGroup;
        protected float FadeDuration => _fadeDuration;
        
        public bool IsVisible => gameObject.activeSelf && _canvasGroup.alpha > 0;
        
        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        #region Sync Methods (Immediate)
        
        /// <summary>
        /// Show immediately without animation.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            SetInteractable(true);
            OnShow();
        }
        
        /// <summary>
        /// Hide immediately without animation.
        /// </summary>
        public virtual void Hide()
        {
            OnHide();
            SetInteractable(false);
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Async Methods (Animated)
        
        /// <summary>
        /// Show with fade-in animation.
        /// </summary>
        public virtual async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            SetInteractable(false); // Block input during animation
            
            await _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(_fadeEase)
                .AsyncWaitForCompletion();
            
            SetInteractable(true);
            OnShow();
        }
        
        /// <summary>
        /// Hide with fade-out animation.
        /// </summary>
        public virtual async UniTask HideAsync()
        {
            OnHide();
            SetInteractable(false);
            
            await _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(_fadeEase)
                .AsyncWaitForCompletion();
            
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Protected Helpers
        
        protected void SetInteractable(bool value)
        {
            _canvasGroup.interactable = value;
            _canvasGroup.blocksRaycasts = value;
        }
        
        /// <summary>
        /// Called after view becomes visible. Override for initialization logic.
        /// </summary>
        protected virtual void OnShow() { }
        
        /// <summary>
        /// Called before view starts hiding. Override for cleanup logic.
        /// </summary>
        protected virtual void OnHide() { }
        
        #endregion
        
        protected virtual void OnDestroy()
        {
            // Kill any running tweens to prevent errors
            _canvasGroup?.DOKill();
        }
    }
}
