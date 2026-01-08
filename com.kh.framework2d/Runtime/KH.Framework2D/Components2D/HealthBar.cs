using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KH.Framework2D.Components2D
{
    /// <summary>
    /// Flexible health/progress bar component.
    /// Supports both UI Image and SpriteRenderer.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Bar Type")]
        [SerializeField] private BarType _barType = BarType.UIImage;
        
        [Header("UI Image Mode")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _damageImage; // Optional: shows damage animation
        
        [Header("Sprite Mode")]
        [SerializeField] private Transform _fillTransform;
        [SerializeField] private SpriteRenderer _fillSprite;
        
        [Header("Animation")]
        [SerializeField] private float _animationDuration = 0.3f;
        [SerializeField] private float _damageDelay = 0.5f;
        [SerializeField] private Ease _easeType = Ease.OutQuad;
        
        [Header("Colors")]
        [SerializeField] private Gradient _healthGradient;
        [SerializeField] private bool _useGradient = true;
        
        [Header("Billboard (3D)")]
        [SerializeField] private bool _billboardToCamera = false;
        
        private float _currentFill = 1f;
        private float _maxValue = 100f;
        private Tweener _fillTween;
        private Tweener _damageTween;
        
        private Camera _mainCamera;
        
        public float CurrentFill => _currentFill;
        public float MaxValue => _maxValue;
        
        private void Awake()
        {
            if (_billboardToCamera)
            {
                _mainCamera = Camera.main;
            }
            
            // Initialize gradient if not set
            if (_healthGradient == null || _healthGradient.colorKeys.Length == 0)
            {
                _healthGradient = new Gradient();
                _healthGradient.SetKeys(
                    new GradientColorKey[] 
                    {
                        new GradientColorKey(Color.red, 0f),
                        new GradientColorKey(Color.yellow, 0.5f),
                        new GradientColorKey(Color.green, 1f)
                    },
                    new GradientAlphaKey[] 
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                );
            }
        }
        
        private void LateUpdate()
        {
            if (_billboardToCamera && _mainCamera != null)
            {
                transform.forward = _mainCamera.transform.forward;
            }
        }
        
        /// <summary>
        /// Initialize the bar with max value.
        /// </summary>
        public void Initialize(float maxValue)
        {
            _maxValue = maxValue;
            SetFill(1f, true);
        }
        
        /// <summary>
        /// Set health with current and max values.
        /// </summary>
        public void SetHealth(float current, float max)
        {
            _maxValue = max;
            float fill = max > 0 ? Mathf.Clamp01(current / max) : 0f;
            SetFill(fill, false);
        }
        
        /// <summary>
        /// Set fill amount directly (0-1).
        /// </summary>
        public void SetFill(float fill, bool instant = false)
        {
            fill = Mathf.Clamp01(fill);
            
            if (instant)
            {
                _currentFill = fill;
                ApplyFill(fill);
                ApplyDamageFill(fill);
            }
            else
            {
                AnimateFill(fill);
            }
        }
        
        /// <summary>
        /// Add or subtract from current fill.
        /// </summary>
        public void ModifyFill(float delta)
        {
            SetFill(_currentFill + delta);
        }
        
        private void AnimateFill(float targetFill)
        {
            _fillTween?.Kill();
            _damageTween?.Kill();
            
            bool isDamage = targetFill < _currentFill;
            
            // Main fill bar animates immediately
            _fillTween = DOTween.To(
                () => _currentFill,
                x => {
                    _currentFill = x;
                    ApplyFill(x);
                },
                targetFill,
                _animationDuration
            ).SetEase(_easeType).SetUpdate(true);
            
            // Damage bar animates with delay (shows red "damage" area)
            if (isDamage && _damageImage != null)
            {
                _damageTween = DOTween.To(
                    () => _damageImage.fillAmount,
                    x => _damageImage.fillAmount = x,
                    targetFill,
                    _animationDuration
                ).SetDelay(_damageDelay).SetEase(_easeType).SetUpdate(true);
            }
        }
        
        private void ApplyFill(float fill)
        {
            switch (_barType)
            {
                case BarType.UIImage:
                    if (_fillImage != null)
                    {
                        _fillImage.fillAmount = fill;
                        
                        if (_useGradient)
                        {
                            _fillImage.color = _healthGradient.Evaluate(fill);
                        }
                    }
                    break;
                    
                case BarType.SpriteScale:
                    if (_fillTransform != null)
                    {
                        var scale = _fillTransform.localScale;
                        scale.x = fill;
                        _fillTransform.localScale = scale;
                        
                        if (_useGradient && _fillSprite != null)
                        {
                            _fillSprite.color = _healthGradient.Evaluate(fill);
                        }
                    }
                    break;
            }
        }
        
        private void ApplyDamageFill(float fill)
        {
            if (_damageImage != null)
            {
                _damageImage.fillAmount = fill;
            }
        }
        
        /// <summary>
        /// Show the health bar.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Hide the health bar.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            _fillTween?.Kill();
            _damageTween?.Kill();
        }
        
        public enum BarType
        {
            UIImage,      // Uses Image.fillAmount
            SpriteScale   // Uses Transform.localScale
        }
    }
}
