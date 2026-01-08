using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Pool
{
    /// <summary>
    /// Pool configuration data.
    /// </summary>
    [Serializable]
    public class PoolConfig
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 50;
    }
    
    /// <summary>
    /// ScriptableObject to configure pools.
    /// Create in Assets folder: Create > Pool > Pool Settings
    /// </summary>
    [CreateAssetMenu(menuName = "Pool/Pool Settings", fileName = "PoolSettings")]
    public class PoolSettings : ScriptableObject
    {
        public List<PoolConfig> pools = new();
    }
    
    /// <summary>
    /// Central manager for multiple object pools.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [SerializeField] private PoolSettings _settings;
        [SerializeField] private bool _warmUpOnStart = true;
        
        private readonly Dictionary<string, ObjectPool<Transform>> _pools = new();
        private Transform _poolRoot;
        
        private void Awake()
        {
            _poolRoot = new GameObject("[Pools]").transform;
            _poolRoot.SetParent(transform);
            
            if (_settings != null)
            {
                foreach (var config in _settings.pools)
                {
                    CreatePool(config);
                }
            }
        }
        
        private async void Start()
        {
            if (_warmUpOnStart)
            {
                await WarmUpAllAsync();
            }
        }
        
        /// <summary>
        /// Create a pool from config.
        /// </summary>
        public void CreatePool(PoolConfig config)
        {
            if (string.IsNullOrEmpty(config.key) || config.prefab == null)
            {
                Debug.LogError("[PoolManager] Invalid pool config!");
                return;
            }
            
            if (_pools.ContainsKey(config.key))
            {
                Debug.LogWarning($"[PoolManager] Pool '{config.key}' already exists!");
                return;
            }
            
            var container = new GameObject($"Pool_{config.key}").transform;
            container.SetParent(_poolRoot);
            
            var pool = new ObjectPool<Transform>(
                config.prefab.transform,
                container,
                config.initialSize,
                config.maxSize
            );
            
            _pools[config.key] = pool;
        }
        
        /// <summary>
        /// Create a pool at runtime.
        /// </summary>
        public void CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            CreatePool(new PoolConfig
            {
                key = key,
                prefab = prefab,
                initialSize = initialSize,
                maxSize = maxSize
            });
        }
        
        /// <summary>
        /// Get an object from a specific pool.
        /// </summary>
        public GameObject Spawn(string key)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found!");
                return null;
            }
            
            var tr = pool.Spawn();
            if (tr == null) return null;

            // Tag pooled handle with key for debugging / analytics.
            var handle = tr.GetComponent<PooledHandle>();
            if (handle != null)
                handle.SetPoolKey(key);

            return tr.gameObject;
        }
        
        /// <summary>
        /// Get an object from a specific pool with position/rotation.
        /// </summary>
        public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found!");
                return null;
            }
            
            var tr = pool.Spawn(position, rotation);
            if (tr == null) return null;

            var handle = tr.GetComponent<PooledHandle>();
            if (handle != null)
                handle.SetPoolKey(key);

            return tr.gameObject;
        }
        
        /// <summary>
        /// Get a component from a specific pool.
        /// </summary>
        public T Spawn<T>(string key) where T : Component
        {
            var obj = Spawn(key);
            return obj != null ? obj.GetComponent<T>() : null;
        }
        
        /// <summary>
        /// Get a component from a specific pool with position/rotation.
        /// </summary>
        public T Spawn<T>(string key, Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Spawn(key, position, rotation);
            return obj != null ? obj.GetComponent<T>() : null;
        }
        
        /// <summary>
        /// Return an object to its pool.
        /// </summary>
        public void Despawn(string key, GameObject obj)
        {
            if (obj == null) return;
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found!");
                return;
            }

            pool.Despawn(obj.transform);
        }

        /// <summary>
        /// Return an object to its owning pool via <see cref="PooledHandle"/>.
        /// Prefer this overload in gameplay code to avoid tracking pool keys.
        /// </summary>
        public bool Despawn(GameObject obj)
        {
            if (obj == null) return false;
            if (obj.TryGetComponent<PooledHandle>(out var handle))
                return handle.TryReturnToPool();

            // Not a pooled object.
            obj.SetActive(false);
            return false;
        }

        /// <summary>Delayed return using <see cref="PooledHandle"/>.</summary>
        public async UniTask<bool> DespawnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return false;
            await UniTask.Delay(TimeSpan.FromSeconds(delay));
            return Despawn(obj);
        }
        
        /// <summary>
        /// Return an object to its pool after a delay.
        /// </summary>
        public void DespawnDelayed(string key, GameObject obj, float delay)
        {
            if (obj == null) return;
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"[PoolManager] Pool '{key}' not found!");
                return;
            }
            
            pool.DespawnDelayed(obj.transform, delay).Forget();
        }
        
        /// <summary>
        /// Warm up all pools.
        /// </summary>
        public async UniTask WarmUpAllAsync()
        {
            foreach (var kvp in _pools)
            {
                await kvp.Value.WarmUpAsync();
            }
        }
        
        /// <summary>
        /// Clear all pools.
        /// </summary>
        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.Clear();
            }
            _pools.Clear();
        }

        #region Query Methods (for ResourceManager integration)

        /// <summary>
        /// Check if a pool exists.
        /// </summary>
        public bool HasPool(string key)
        {
            return _pools.ContainsKey(key);
        }

        /// <summary>
        /// Get the original prefab for a pool.
        /// </summary>
        public GameObject GetOriginal(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool.Prefab?.gameObject;
            }
            return null;
        }

        /// <summary>
        /// Get pool statistics.
        /// </summary>
        /// <returns>Tuple of (activeCount, pooledCount, maxSize)</returns>
        public (int active, int pooled, int max) GetPoolInfo(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                return pool.GetInfo();
            }
            return (0, 0, 0);
        }

        /// <summary>
        /// Get all pool keys.
        /// </summary>
        public IEnumerable<string> GetAllPoolKeys()
        {
            return _pools.Keys;
        }

        #endregion
        
        private void OnDestroy()
        {
            ClearAll();
        }
    }
}
