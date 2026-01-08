using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using KH.Framework2D.Pool;
using UnityEngine;

namespace KH.Framework2D.Services
{
    /// <summary>
    /// 리소스 관리자. 로드/생성/파괴 및 풀링 통합.
    /// Poolable 컴포넌트가 있는 프리팹은 자동으로 풀링 처리됨.
    /// 
    /// 설정:
    /// - VContainer 사용 시: PoolManager를 주입받아 사용
    /// - ServiceLocator 사용 시: 등록된 PoolManager 자동 탐색
    /// </summary>
    public class ResourceManager : MonoBehaviour, IResourceService
    {
        [Header("Settings")]
        [SerializeField] private bool _autoCreatePools = true;
        [SerializeField] private int _defaultPoolInitialSize = 5;
        [SerializeField] private int _defaultPoolMaxSize = 30;

        private PoolManager _poolManager;
        private readonly Dictionary<string, GameObject> _prefabCache = new();
        private readonly HashSet<string> _failedPaths = new(); // 로드 실패한 경로 캐시

        #region Initialization

        private void Awake()
        {
            // PoolManager 자동 탐색
            _poolManager = FindObjectOfType<PoolManager>();
        }

        /// <summary>
        /// PoolManager 설정 (VContainer 주입용).
        /// </summary>
        public void SetPoolManager(PoolManager poolManager)
        {
            _poolManager = poolManager;
        }

        #endregion

        #region Load

        public T Load<T>(string path) where T : Object
        {
            // 실패한 경로 캐시 확인
            if (_failedPaths.Contains(path))
                return null;

            // GameObject 요청 시 캐시 확인
            if (typeof(T) == typeof(GameObject))
            {
                if (_prefabCache.TryGetValue(path, out var cached))
                    return cached as T;
            }

            // Resources 폴더에서 로드
            T resource = Resources.Load<T>(path);

            if (resource == null)
            {
                _failedPaths.Add(path);
                Debug.LogWarning($"[ResourceManager] 리소스 로드 실패: {path}");
                return null;
            }

            // GameObject면 캐시
            if (resource is GameObject go)
            {
                _prefabCache[path] = go;
            }

            return resource;
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            // 캐시 확인
            if (typeof(T) == typeof(GameObject) && _prefabCache.TryGetValue(path, out var cached))
            {
                return cached as T;
            }

            var request = Resources.LoadAsync<T>(path);
            await request;

            if (request.asset == null)
            {
                Debug.LogWarning($"[ResourceManager] 비동기 리소스 로드 실패: {path}");
                return null;
            }

            // GameObject면 캐시
            if (request.asset is GameObject go)
            {
                _prefabCache[path] = go;
            }

            return request.asset as T;
        }

        public async UniTask<T[]> LoadAllAsync<T>(string path) where T : Object
        {
            var request = Resources.LoadAsync<T>(path);
            await request;
            return Resources.LoadAll<T>(path);
        }

        #endregion

        #region Instantiate

        public GameObject Instantiate(string path, Transform parent = null)
        {
            return Instantiate(path, Vector3.zero, Quaternion.identity, parent);
        }

        public GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            string fullPath = path.StartsWith("Prefabs/") ? path : $"Prefabs/{path}";
            
            GameObject prefab = Load<GameObject>(fullPath);
            if (prefab == null)
            {
                // Prefabs/ 없이도 시도
                prefab = Load<GameObject>(path);
            }
            
            if (prefab == null)
            {
                Debug.LogError($"[ResourceManager] 프리팹 로드 실패: {path}");
                return null;
            }

            return InstantiateInternal(prefab, position, rotation, parent);
        }

        public T Instantiate<T>(string path, Transform parent = null) where T : Component
        {
            GameObject go = Instantiate(path, parent);
            return go?.GetComponent<T>();
        }

        public T Instantiate<T>(string path, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject go = Instantiate(path, position, rotation, parent);
            return go?.GetComponent<T>();
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            return Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
        }

        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[ResourceManager] 프리팹이 null입니다.");
                return null;
            }

            return InstantiateInternal(prefab, position, rotation, parent);
        }

        private GameObject InstantiateInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject go = null;

            // Poolable 컴포넌트 확인 → 풀에서 가져옴
            bool isPoolable = prefab.GetComponent<Poolable>() != null;

            if (isPoolable && _poolManager != null)
            {
                string key = prefab.name;

                // 풀이 없으면 자동 생성
                if (_autoCreatePools && !_poolManager.HasPool(key))
                {
                    _poolManager.CreatePool(key, prefab, _defaultPoolInitialSize, _defaultPoolMaxSize);
                }

                go = _poolManager.Spawn(key, position, rotation);
            }

            // 풀에서 가져오지 못했으면 일반 생성
            if (go == null)
            {
                go = Object.Instantiate(prefab, position, rotation, parent);
            }

            if (go != null)
            {
                // (Clone) 제거
                go.name = prefab.name;

                // 부모 설정 (풀에서 가져온 경우)
                if (parent != null && go.transform.parent != parent)
                {
                    go.transform.SetParent(parent);
                }
            }

            return go;
        }

        #endregion

        #region Destroy

        public void Destroy(GameObject go, float delay = 0f)
        {
            if (go == null) return;

            if (delay > 0f)
            {
                DestroyDelayed(go, delay).Forget();
                return;
            }

            DestroyInternal(go);
        }

        public void Destroy(Component component, float delay = 0f)
        {
            if (component == null) return;
            Destroy(component.gameObject, delay);
        }

        private void DestroyInternal(GameObject go)
        {
            // Poolable이면 풀로 반환 시도
            var poolable = go.GetComponent<Poolable>();
            if (poolable != null && _poolManager != null)
            {
                // PooledHandle을 통해 반환 시도
                var handle = go.GetComponent<PooledHandle>();
                if (handle != null && handle.TryReturnToPool())
                {
                    return;
                }

                // PoolManager.Despawn 시도
                if (_poolManager.Despawn(go))
                {
                    return;
                }
            }

            // 일반 파괴
            Object.Destroy(go);
        }

        private async UniTaskVoid DestroyDelayed(GameObject go, float delay)
        {
            await UniTask.Delay((int)(delay * 1000));
            
            if (go != null) // 지연 중 이미 파괴되었을 수 있음
            {
                DestroyInternal(go);
            }
        }

        #endregion

        #region Pool Management

        public void CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            if (_poolManager == null)
            {
                Debug.LogWarning("[ResourceManager] PoolManager가 없어 풀을 생성할 수 없습니다.");
                return;
            }

            _poolManager.CreatePool(key, prefab, initialSize, maxSize);
        }

        public void WarmUpPool(string key, int count)
        {
            // PoolManager에 WarmUp 기능이 있다면 호출
            // 현재 구현에서는 WarmUpAllAsync만 있으므로 생략
        }

        public async UniTask WarmUpPoolAsync(string key, int count)
        {
            if (_poolManager != null)
            {
                await _poolManager.WarmUpAllAsync();
            }
        }

        public void ClearPool(string key)
        {
            // 특정 풀만 정리 (PoolManager 확장 필요)
        }

        public void ClearAllPools()
        {
            _poolManager?.ClearAll();
        }

        public bool HasPool(string key)
        {
            return _poolManager?.HasPool(key) ?? false;
        }

        #endregion

        #region Cache Management

        public void ClearPrefabCache()
        {
            _prefabCache.Clear();
            _failedPaths.Clear();
        }

        public void RemoveFromCache(string path)
        {
            _prefabCache.Remove(path);
            _failedPaths.Remove(path);
        }

        #endregion

        #region Utilities

        /// <summary>
        /// 경로에서 이름만 추출.
        /// </summary>
        private string GetNameFromPath(string path)
        {
            int index = path.LastIndexOf('/');
            return index >= 0 ? path.Substring(index + 1) : path;
        }

        #endregion

        private void OnDestroy()
        {
            ClearPrefabCache();
        }
    }
}
