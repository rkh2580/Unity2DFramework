using System;
using KH.Framework2D.Pool;
using UnityEngine;

namespace KH.Framework2D.Combat
{
    /// <summary>
    /// Projectile component for ranged attacks and skills.
    /// Works with object pooling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Movement")]
        [SerializeField] private float _speed = 10f;
        [SerializeField] private float _maxLifetime = 5f;
        [SerializeField] private bool _usePhysics = true;
        
        [Header("Homing (Optional)")]
        [SerializeField] private bool _isHoming = false;
        [SerializeField] private float _homingStrength = 5f;
        [SerializeField] private float _homingStartDelay = 0.2f;
        
        [Header("Collision")]
        [SerializeField] private LayerMask _hitLayers;
        [SerializeField] private bool _destroyOnHit = true;
        [SerializeField] private bool _piercing = false;
        [SerializeField] private int _maxPierceCount = 3;
        
        [Header("Effects")]
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private GameObject _trailEffect;
        [SerializeField] private AudioClip _hitSound;
        
        private Rigidbody2D _rb;
        private Transform _target;
        private Vector2 _direction;
        private float _lifetime;
        private float _homingTimer;
        private int _pierceCount;
        private bool _isActive;
        
        // Damage info
        private int _damage;
        private bool _isCritical;
        private GameObject _owner;
        
        public event Action<Projectile, Collider2D> OnHit;
        public event Action<Projectile> OnDestroyed;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
        }
        
        private void Update()
        {
            if (!_isActive) return;
            
            _lifetime += Time.deltaTime;
            
            if (_lifetime >= _maxLifetime)
            {
                Despawn();
                return;
            }
            
            // Homing
            if (_isHoming && _target != null)
            {
                _homingTimer += Time.deltaTime;
                
                if (_homingTimer >= _homingStartDelay)
                {
                    Vector2 targetDir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                    _direction = Vector2.Lerp(_direction, targetDir, _homingStrength * Time.deltaTime).normalized;
                }
            }
            
            // Non-physics movement
            if (!_usePhysics)
            {
                transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
            }
            
            // Rotate to face direction
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        private void FixedUpdate()
        {
            if (!_isActive || !_usePhysics) return;
            
            _rb.linearVelocity = _direction * _speed;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;
            
            // Check layer
            if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0)
                return;
            
            // Don't hit owner
            if (other.gameObject == _owner)
                return;
            
            // Handle hit
            HandleHit(other);
        }
        
        private void HandleHit(Collider2D target)
        {
            OnHit?.Invoke(this, target);
            
            // Spawn hit effect
            if (_hitEffect != null)
            {
                Instantiate(_hitEffect, transform.position, Quaternion.identity);
            }
            
            // Play sound
            if (_hitSound != null)
            {
                AudioSource.PlayClipAtPoint(_hitSound, transform.position);
            }
            
            // Piercing
            if (_piercing)
            {
                _pierceCount++;
                if (_pierceCount >= _maxPierceCount)
                {
                    Despawn();
                }
            }
            else if (_destroyOnHit)
            {
                Despawn();
            }
        }
        
        #region Public Methods
        
        /// <summary>
        /// Initialize and fire the projectile.
        /// </summary>
        public void Fire(Vector2 direction, float speed = -1)
        {
            _direction = direction.normalized;
            
            if (speed > 0)
                _speed = speed;
            
            _isActive = true;
            
            if (_trailEffect != null)
                _trailEffect.SetActive(true);
        }
        
        /// <summary>
        /// Initialize with damage info.
        /// </summary>
        public void Initialize(GameObject owner, int damage, bool isCritical = false)
        {
            _owner = owner;
            _damage = damage;
            _isCritical = isCritical;
        }
        
        /// <summary>
        /// Set homing target.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            _isHoming = target != null;
        }
        
        /// <summary>
        /// Get damage info.
        /// </summary>
        public (int damage, bool isCritical) GetDamageInfo()
        {
            return (_damage, _isCritical);
        }
        
        #endregion
        
        #region IPoolable
        
        public void OnSpawn()
        {
            _lifetime = 0f;
            _homingTimer = 0f;
            _pierceCount = 0;
            _isActive = false;
            _target = null;
            _owner = null;
            _damage = 0;
            _isCritical = false;
            
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }
        
        public void OnDespawn()
        {
            _isActive = false;
            
            if (_trailEffect != null)
                _trailEffect.SetActive(false);
            
            OnDestroyed?.Invoke(this);
        }
        
        #endregion
        
        private void Despawn()
        {
            // Prefer returning to pool when available.
            if (TryGetComponent<PooledHandle>(out var handle) && handle.TryReturnToPool())
                return;

            // Fallback for non-pooled usage.
            OnDespawn();
            gameObject.SetActive(false);
        }
    }
}
