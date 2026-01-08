using KH.Framework2D.Pool;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace KH.Framework2D.Components2D
{
    /// <summary>
    /// Floating damage/heal number popup.
    /// Works with object pooling.
    /// </summary>
    public class DamagePopup : MonoBehaviour, IPoolable
    {
        [Header("Components")]
        [SerializeField] private TextMeshPro _text; // World space
        [SerializeField] private TextMeshProUGUI _textUI; // Screen space (optional)
        
        [Header("Animation")]
        [SerializeField] private float _floatHeight = 1f;
        [SerializeField] private float _floatDuration = 0.8f;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _scaleAmount = 1.2f;
        [SerializeField] private float _scaleDuration = 0.15f;
        [SerializeField] private AnimationCurve _floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Random Offset")]
        [SerializeField] private float _randomOffsetX = 0.3f;
        [SerializeField] private float _randomOffsetY = 0.2f;
        
        [Header("Colors")]
        [SerializeField] private Color _damageColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _healColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color _criticalColor = new Color(1f, 1f, 0f);
        [SerializeField] private Color _missColor = new Color(0.7f, 0.7f, 0.7f);
        
        private Vector3 _startPosition;
        private Sequence _sequence;
        
        /// <summary>
        /// Show damage number.
        /// </summary>
        public void ShowDamage(int amount, bool isCritical = false)
        {
            SetText(amount.ToString(), isCritical ? _criticalColor : _damageColor);
            if (isCritical)
            {
                _scaleAmount = 1.5f;
            }
            Animate();
        }
        
        /// <summary>
        /// Show heal number.
        /// </summary>
        public void ShowHeal(int amount)
        {
            SetText($"+{amount}", _healColor);
            Animate();
        }
        
        /// <summary>
        /// Show miss text.
        /// </summary>
        public void ShowMiss()
        {
            SetText("MISS", _missColor);
            Animate();
        }
        
        /// <summary>
        /// Show custom text.
        /// </summary>
        public void ShowText(string text, Color color)
        {
            SetText(text, color);
            Animate();
        }
        
        /// <summary>
        /// Initialize at world position.
        /// </summary>
        public void SetPosition(Vector3 worldPosition)
        {
            // Add random offset
            worldPosition.x += Random.Range(-_randomOffsetX, _randomOffsetX);
            worldPosition.y += Random.Range(-_randomOffsetY, _randomOffsetY);
            
            transform.position = worldPosition;
            _startPosition = worldPosition;
        }
        
        private void SetText(string text, Color color)
        {
            if (_text != null)
            {
                _text.text = text;
                _text.color = color;
            }
            
            if (_textUI != null)
            {
                _textUI.text = text;
                _textUI.color = color;
            }
        }
        
        private void Animate()
        {
            _sequence?.Kill();
            
            // Reset
            transform.position = _startPosition;
            transform.localScale = Vector3.one;
            SetAlpha(1f);
            
            _sequence = DOTween.Sequence();
            
            // Pop in
            _sequence.Append(transform.DOScale(_scaleAmount, _scaleDuration).SetEase(Ease.OutBack));
            _sequence.Append(transform.DOScale(1f, _scaleDuration).SetEase(Ease.InOutQuad));
            
            // Float up
            Vector3 endPos = _startPosition + Vector3.up * _floatHeight;
            _sequence.Join(transform.DOMove(endPos, _floatDuration).SetEase(_floatCurve));
            
            // Fade out at the end
            _sequence.Insert(_floatDuration - _fadeDuration, 
                DOTween.To(() => 1f, SetAlpha, 0f, _fadeDuration));
            
            _sequence.SetUpdate(true); // Ignore timeScale
            _sequence.OnComplete(OnAnimationComplete);
        }
        
        private void SetAlpha(float alpha)
        {
            if (_text != null)
            {
                var color = _text.color;
                color.a = alpha;
                _text.color = color;
            }
            
            if (_textUI != null)
            {
                var color = _textUI.color;
                color.a = alpha;
                _textUI.color = color;
            }
        }
        
        private void OnAnimationComplete()
        {
            // Prefer returning to pool when available.
            if (TryGetComponent<PooledHandle>(out var handle) && handle.TryReturnToPool())
                return;

            // Fallback for non-pooled usage.
            gameObject.SetActive(false);
        }
        
        #region IPoolable
        
        public void OnSpawn()
        {
            transform.localScale = Vector3.one;
            SetAlpha(1f);
        }
        
        public void OnDespawn()
        {
            _sequence?.Kill();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
    
    /// <summary>
    /// Static helper to spawn damage popups.
    /// </summary>
    public static class DamagePopupSpawner
    {
        private static ObjectPool<DamagePopup> _pool;
        private static DamagePopup _prefab;
        
        /// <summary>
        /// Initialize the spawner with a prefab.
        /// </summary>
        public static void Initialize(DamagePopup prefab, int poolSize = 20)
        {
            _prefab = prefab;
            _pool = new ObjectPool<DamagePopup>(prefab, null, poolSize, 50);
            _pool.WarmUp();
        }
        
        /// <summary>
        /// Spawn a damage popup.
        /// </summary>
        public static void SpawnDamage(Vector3 position, int amount, bool isCritical = false)
        {
            var popup = _pool.Spawn();
            if (popup != null)
            {
                popup.SetPosition(position);
                popup.ShowDamage(amount, isCritical);
            }
        }
        
        /// <summary>
        /// Spawn a heal popup.
        /// </summary>
        public static void SpawnHeal(Vector3 position, int amount)
        {
            var popup = _pool.Spawn();
            if (popup != null)
            {
                popup.SetPosition(position);
                popup.ShowHeal(amount);
            }
        }
        
        /// <summary>
        /// Spawn a miss popup.
        /// </summary>
        public static void SpawnMiss(Vector3 position)
        {
            var popup = _pool.Spawn();
            if (popup != null)
            {
                popup.SetPosition(position);
                popup.ShowMiss();
            }
        }
        
        /// <summary>
        /// Return a popup to the pool.
        /// </summary>
        public static void Return(DamagePopup popup)
        {
            _pool?.Despawn(popup);
        }
    }
}
