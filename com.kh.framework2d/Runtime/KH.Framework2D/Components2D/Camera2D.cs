using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace KH.Framework2D.Components2D
{
    /// <summary>
    /// 2D Camera controller with follow, shake, bounds, and zoom.
    /// </summary>
    public class Camera2D : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10);
        
        [Header("Follow Settings")]
        [SerializeField] private bool _followEnabled = true;
        [SerializeField] private FollowMode _followMode = FollowMode.Smooth;
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private Vector2 _deadZone = Vector2.zero; // Target can move within this before camera moves
        
        [Header("Bounds")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Bounds _bounds = new Bounds(Vector3.zero, new Vector3(20, 10, 0));
        
        [Header("Zoom")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _defaultZoom = 5f;
        [SerializeField] private float _minZoom = 3f;
        [SerializeField] private float _maxZoom = 10f;
        
        [Header("Shake")]
        [SerializeField] private float _defaultShakeDuration = 0.3f;
        [SerializeField] private float _defaultShakeMagnitude = 0.2f;
        
        private Vector3 _shakeOffset;
        private bool _isShaking;
        
        public Transform Target => _target;
        public bool FollowEnabled
        {
            get => _followEnabled;
            set => _followEnabled = value;
        }
        
        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            
            if (_camera == null)
                _camera = Camera.main;
        }
        
        private void LateUpdate()
        {
            if (!_followEnabled || _target == null) return;
            
            Vector3 desiredPosition = _target.position + _offset;
            
            // Apply dead zone
            if (_deadZone != Vector2.zero)
            {
                Vector3 currentPos = transform.position;
                float dx = desiredPosition.x - currentPos.x;
                float dy = desiredPosition.y - currentPos.y;
                
                if (Mathf.Abs(dx) < _deadZone.x) desiredPosition.x = currentPos.x;
                if (Mathf.Abs(dy) < _deadZone.y) desiredPosition.y = currentPos.y;
            }
            
            // Apply bounds
            if (_useBounds)
            {
                desiredPosition = ClampToBounds(desiredPosition);
            }
            
            // Apply follow mode
            Vector3 newPosition = _followMode switch
            {
                FollowMode.Instant => desiredPosition,
                FollowMode.Smooth => Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime),
                FollowMode.SmoothDamp => SmoothDampFollow(desiredPosition),
                _ => desiredPosition
            };
            
            // Apply shake offset
            transform.position = newPosition + _shakeOffset;
        }
        
        private Vector3 _velocity;
        private Vector3 SmoothDampFollow(Vector3 target)
        {
            return Vector3.SmoothDamp(transform.position, target, ref _velocity, 1f / _smoothSpeed);
        }
        
        #region Target
        
        /// <summary>
        /// Set the follow target.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        /// <summary>
        /// Move camera to target instantly.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;
            
            Vector3 position = _target.position + _offset;
            if (_useBounds)
            {
                position = ClampToBounds(position);
            }
            transform.position = position;
        }
        
        /// <summary>
        /// Move camera to a specific position.
        /// </summary>
        public void MoveTo(Vector3 position, float duration = 0.5f)
        {
            _followEnabled = false;
            position.z = _offset.z;
            
            if (_useBounds)
            {
                position = ClampToBounds(position);
            }
            
            transform.DOMove(position, duration).SetUpdate(true);
        }
        
        /// <summary>
        /// Resume following target.
        /// </summary>
        public void ResumeFollow()
        {
            _followEnabled = true;
        }
        
        #endregion
        
        #region Shake
        
        /// <summary>
        /// Shake the camera.
        /// </summary>
        public void Shake(float duration = -1, float magnitude = -1)
        {
            duration = duration < 0 ? _defaultShakeDuration : duration;
            magnitude = magnitude < 0 ? _defaultShakeMagnitude : magnitude;
            
            ShakeAsync(duration, magnitude).Forget();
        }
        
        /// <summary>
        /// Shake camera async.
        /// </summary>
        public async UniTask ShakeAsync(float duration, float magnitude)
        {
            if (_isShaking) return;
            
            _isShaking = true;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                
                _shakeOffset = new Vector3(x, y, 0);
                
                elapsed += Time.unscaledDeltaTime;
                
                // Reduce magnitude over time
                magnitude = Mathf.Lerp(magnitude, 0, elapsed / duration);
                
                await UniTask.Yield();
            }
            
            _shakeOffset = Vector3.zero;
            _isShaking = false;
        }
        
        /// <summary>
        /// Stop shake immediately.
        /// </summary>
        public void StopShake()
        {
            _isShaking = false;
            _shakeOffset = Vector3.zero;
        }
        
        #endregion
        
        #region Zoom
        
        /// <summary>
        /// Set camera zoom (orthographic size).
        /// </summary>
        public void SetZoom(float zoom, float duration = 0f)
        {
            if (_camera == null || !_camera.orthographic) return;
            
            zoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
            
            if (duration <= 0)
            {
                _camera.orthographicSize = zoom;
            }
            else
            {
                DOTween.To(
                    () => _camera.orthographicSize,
                    x => _camera.orthographicSize = x,
                    zoom,
                    duration
                ).SetUpdate(true);
            }
        }
        
        /// <summary>
        /// Zoom in.
        /// </summary>
        public void ZoomIn(float amount = 1f, float duration = 0.3f)
        {
            SetZoom(_camera.orthographicSize - amount, duration);
        }
        
        /// <summary>
        /// Zoom out.
        /// </summary>
        public void ZoomOut(float amount = 1f, float duration = 0.3f)
        {
            SetZoom(_camera.orthographicSize + amount, duration);
        }
        
        /// <summary>
        /// Reset to default zoom.
        /// </summary>
        public void ResetZoom(float duration = 0.3f)
        {
            SetZoom(_defaultZoom, duration);
        }
        
        #endregion
        
        #region Bounds
        
        /// <summary>
        /// Set camera bounds.
        /// </summary>
        public void SetBounds(Bounds bounds)
        {
            _bounds = bounds;
            _useBounds = true;
        }
        
        /// <summary>
        /// Disable bounds.
        /// </summary>
        public void DisableBounds()
        {
            _useBounds = false;
        }
        
        private Vector3 ClampToBounds(Vector3 position)
        {
            if (_camera == null) return position;
            
            float verticalSize = _camera.orthographicSize;
            float horizontalSize = verticalSize * _camera.aspect;
            
            float minX = _bounds.min.x + horizontalSize;
            float maxX = _bounds.max.x - horizontalSize;
            float minY = _bounds.min.y + verticalSize;
            float maxY = _bounds.max.y - verticalSize;
            
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            
            return position;
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Convert screen position to world position.
        /// </summary>
        public Vector3 ScreenToWorld(Vector3 screenPos)
        {
            if (_camera != null)
            {
                screenPos.z = -_offset.z;
                return _camera.ScreenToWorldPoint(screenPos);
            }
            return screenPos;
        }
        
        /// <summary>
        /// Convert world position to screen position.
        /// </summary>
        public Vector3 WorldToScreen(Vector3 worldPos)
        {
            return _camera != null ? _camera.WorldToScreenPoint(worldPos) : worldPos;
        }
        
        #endregion
        
        private void OnDrawGizmosSelected()
        {
            if (_useBounds)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            }
        }
        
        public enum FollowMode
        {
            Instant,
            Smooth,
            SmoothDamp
        }
    }
}
