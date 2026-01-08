using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Pool
{
    /// <summary>
    /// Interface for poolable objects.
    /// Implement this to receive pool lifecycle callbacks.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
    
    /// <summary>
    /// Generic object pool for GameObjects with prefab.
    /// Supports UniTask for async warm-up.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _pool = new();
        private readonly HashSet<T> _activeObjects = new();
        
        private readonly int _defaultCapacity;
        private readonly int _maxSize;
        
        /// <summary>
        /// Original prefab used for instantiation.
        /// </summary>
        public T Prefab => _prefab;
        
        /// <summary>
        /// Maximum pool size.
        /// </summary>
        public int MaxSize => _maxSize;
        
        public int CountInactive => _pool.Count;
        public int CountActive => _activeObjects.Count;
        public int CountTotal => CountInactive + CountActive;
        
        /// <summary>
        /// Get pool statistics.
        /// </summary>
        /// <returns>Tuple of (activeCount, pooledCount, maxSize)</returns>
        public (int active, int pooled, int max) GetInfo()
        {
            return (CountActive, CountInactive, _maxSize);
        }
        
        public ObjectPool(T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;
            _defaultCapacity = defaultCapacity;
            _maxSize = maxSize;
        }
        
        /// <summary>
        /// Pre-instantiate objects to avoid runtime allocation spikes.
        /// </summary>
        public void WarmUp(int count = -1)
        {
            count = count < 0 ? _defaultCapacity : count;
            count = Mathf.Min(count, _maxSize - CountTotal);
            
            for (int i = 0; i < count; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
            }
        }
        
        /// <summary>
        /// Async warm-up to spread instantiation across frames.
        /// </summary>
        public async UniTask WarmUpAsync(int count = -1, int perFrame = 5)
        {
            count = count < 0 ? _defaultCapacity : count;
            count = Mathf.Min(count, _maxSize - CountTotal);
            
            for (int i = 0; i < count; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                _pool.Push(obj);
                
                if ((i + 1) % perFrame == 0)
                    await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public T Spawn()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else if (CountTotal < _maxSize)
            {
                obj = CreateNew();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool for {_prefab.name} reached max size ({_maxSize})!");
                return null;
            }
            
            obj.gameObject.SetActive(true);
            _activeObjects.Add(obj);

            // Bind pooled handle so objects can return themselves without knowing pool internals.
            var handle = obj.GetComponent<PooledHandle>();
            if (handle == null)
                handle = obj.gameObject.AddComponent<PooledHandle>();
            handle.SetReturnAction(() => Despawn(obj));
            
            // Notify if IPoolable
            if (obj is IPoolable poolable)
                poolable.OnSpawn();
            
            return obj;
        }
        
        /// <summary>
        /// Get an object and set position/rotation.
        /// </summary>
        public T Spawn(Vector3 position, Quaternion rotation)
        {
            var obj = Spawn();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }
        
        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Despawn(T obj)
        {
            if (obj == null || !_activeObjects.Contains(obj))
                return;
            
            // Notify if IPoolable
            if (obj is IPoolable poolable)
                poolable.OnDespawn();
            
            obj.gameObject.SetActive(false);
            
            if (_parent != null)
                obj.transform.SetParent(_parent);
            
            _activeObjects.Remove(obj);
            _pool.Push(obj);
        }
        
        /// <summary>
        /// Return an object to the pool after a delay.
        /// </summary>
        public async UniTaskVoid DespawnDelayed(T obj, float delay)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            Despawn(obj);
        }
        
        /// <summary>
        /// Return all active objects to the pool.
        /// </summary>
        public void DespawnAll()
        {
            foreach (var obj in new List<T>(_activeObjects))
            {
                Despawn(obj);
            }
        }
        
        /// <summary>
        /// Destroy all pooled objects (both active and inactive).
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _activeObjects)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
            _activeObjects.Clear();
            
            while (_pool.Count > 0)
            {
                var obj = _pool.Pop();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
        }
        
        private T CreateNew()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_Pooled";

            // Add pooled handle once to avoid AddComponent churn at runtime.
            if (obj.GetComponent<PooledHandle>() == null)
                obj.gameObject.AddComponent<PooledHandle>();
            return obj;
        }
    }
}
