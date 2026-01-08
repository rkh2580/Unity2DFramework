using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using KH.Framework2D.Pool;
using UnityEngine;

namespace KH.Framework2D.Components2D
{
    /// <summary>
    /// Base component for 2D characters with sprite and animation support.
    /// Provides common functionality: flip, flash, shake, color tint.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Character2D : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] protected SpriteRenderer _spriteRenderer;
        [SerializeField] protected Animator _animator;
        
        [Header("Flip Settings")]
        [SerializeField] private bool _flipViaScale = true; // true: scale.x, false: spriteRenderer.flipX
        [SerializeField] private bool _faceRightByDefault = true;
        
        [Header("Hit Effect")]
        [SerializeField] private Color _hitColor = Color.red;
        [SerializeField] private float _hitFlashDuration = 0.1f;
        [SerializeField] private int _hitFlashCount = 2;
        
        private Color _originalColor;
        private Vector3 _originalScale;
        private bool _isFacingRight;
        
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public Animator Animator => _animator;
        public bool IsFacingRight => _isFacingRight;
        
        protected virtual void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (_animator == null)
                _animator = GetComponent<Animator>();
            
            _originalColor = _spriteRenderer.color;
            _originalScale = transform.localScale;
            _isFacingRight = _faceRightByDefault;
        }
        
        #region Sprite
        
        /// <summary>
        /// Change the sprite.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = sprite;
        }
        
        /// <summary>
        /// Set sprite color.
        /// </summary>
        public void SetColor(Color color)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = color;
        }
        
        /// <summary>
        /// Set sprite alpha.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_spriteRenderer != null)
            {
                var color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// Reset to original color.
        /// </summary>
        public void ResetColor()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;
        }
        
        #endregion
        
        #region Flip / Facing
        
        /// <summary>
        /// Flip the character to face a direction.
        /// </summary>
        public void SetFacing(bool faceRight)
        {
            if (_isFacingRight == faceRight) return;
            
            _isFacingRight = faceRight;
            
            if (_flipViaScale)
            {
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1);
                transform.localScale = scale;
            }
            else
            {
                _spriteRenderer.flipX = !faceRight;
            }
        }
        
        /// <summary>
        /// Face towards a target position.
        /// </summary>
        public void FaceTarget(Vector3 targetPosition)
        {
            SetFacing(targetPosition.x > transform.position.x);
        }
        
        /// <summary>
        /// Face towards a target transform.
        /// </summary>
        public void FaceTarget(Transform target)
        {
            if (target != null)
                FaceTarget(target.position);
        }
        
        /// <summary>
        /// Flip current facing direction.
        /// </summary>
        public void Flip()
        {
            SetFacing(!_isFacingRight);
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// Play an animation by name.
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (_animator != null && !string.IsNullOrEmpty(animationName))
            {
                _animator.Play(animationName);
            }
        }
        
        /// <summary>
        /// Play animation and wait for completion.
        /// </summary>
        public async UniTask PlayAnimationAsync(string animationName)
        {
            if (_animator == null) return;
            
            _animator.Play(animationName);
            
            // Wait one frame for animator to update
            await UniTask.Yield();
            
            // Wait for animation to complete
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length));
        }
        
        /// <summary>
        /// Set animator trigger.
        /// </summary>
        public void SetTrigger(string triggerName)
        {
            if (_animator != null)
                _animator.SetTrigger(triggerName);
        }
        
        /// <summary>
        /// Set animator bool parameter.
        /// </summary>
        public void SetBool(string paramName, bool value)
        {
            if (_animator != null)
                _animator.SetBool(paramName, value);
        }
        
        /// <summary>
        /// Set animator float parameter.
        /// </summary>
        public void SetFloat(string paramName, float value)
        {
            if (_animator != null)
                _animator.SetFloat(paramName, value);
        }
        
        /// <summary>
        /// Set animator integer parameter.
        /// </summary>
        public void SetInteger(string paramName, int value)
        {
            if (_animator != null)
                _animator.SetInteger(paramName, value);
        }
        
        /// <summary>
        /// Set animator speed.
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null)
                _animator.speed = speed;
        }
        
        #endregion
        
        #region Effects
        
        /// <summary>
        /// Flash the sprite (hit effect).
        /// </summary>
        public async UniTask FlashAsync()
        {
            if (_spriteRenderer == null) return;
            
            for (int i = 0; i < _hitFlashCount; i++)
            {
                _spriteRenderer.color = _hitColor;
                await UniTask.Delay(TimeSpan.FromSeconds(_hitFlashDuration));
                _spriteRenderer.color = _originalColor;
                await UniTask.Delay(TimeSpan.FromSeconds(_hitFlashDuration));
            }
        }
        
        /// <summary>
        /// Flash with custom color.
        /// </summary>
        public async UniTask FlashAsync(Color color, float duration = 0.1f, int count = 2)
        {
            if (_spriteRenderer == null) return;
            
            for (int i = 0; i < count; i++)
            {
                _spriteRenderer.color = color;
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
                _spriteRenderer.color = _originalColor;
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
            }
        }
        
        /// <summary>
        /// Shake the character (hit reaction).
        /// </summary>
        public void Shake(float duration = 0.2f, float strength = 0.1f)
        {
            transform.DOShakePosition(duration, strength).SetUpdate(true);
        }
        
        /// <summary>
        /// Punch scale effect (attack impact).
        /// </summary>
        public void PunchScale(float strength = 0.2f, float duration = 0.2f)
        {
            transform.DOPunchScale(Vector3.one * strength, duration).SetUpdate(true);
        }
        
        /// <summary>
        /// Fade out the sprite.
        /// </summary>
        public async UniTask FadeOutAsync(float duration = 0.5f)
        {
            if (_spriteRenderer != null)
            {
                await _spriteRenderer.DOFade(0f, duration).AsyncWaitForCompletion();
            }
        }
        
        /// <summary>
        /// Fade in the sprite.
        /// </summary>
        public async UniTask FadeInAsync(float duration = 0.5f)
        {
            if (_spriteRenderer != null)
            {
                await _spriteRenderer.DOFade(1f, duration).AsyncWaitForCompletion();
            }
        }
        
        /// <summary>
        /// Death effect: flash, fade, disable.
        /// </summary>
        public async UniTask DeathEffectAsync()
        {
            await FlashAsync(Color.white, 0.05f, 3);
            await FadeOutAsync(0.3f);
            // Stop tweens before disabling/returning.
            transform.DOKill();
            if (_spriteRenderer != null)
                _spriteRenderer.DOKill();

            // Prefer returning to pool when available.
            if (TryGetComponent<PooledHandle>(out var handle) && handle.TryReturnToPool())
                return;

            gameObject.SetActive(false);
        }
        
        #endregion
        
        protected virtual void OnDestroy()
        {
            transform.DOKill();
            if (_spriteRenderer != null)
                _spriteRenderer.DOKill();
        }
    }
}
