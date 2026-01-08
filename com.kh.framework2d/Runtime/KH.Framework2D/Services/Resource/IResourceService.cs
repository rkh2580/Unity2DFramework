using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Services
{
    /// <summary>
    /// 리소스 로드/생성/파괴 통합 인터페이스.
    /// 풀링 여부를 호출자가 알 필요 없이 투명하게 처리.
    /// 
    /// 사용법:
    /// var enemy = resourceService.Instantiate("Enemies/Goblin");
    /// resourceService.Destroy(enemy); // 풀링이면 자동 반환, 아니면 실제 파괴
    /// </summary>
    public interface IResourceService
    {
        #region Load

        /// <summary>
        /// 리소스 동기 로드.
        /// </summary>
        /// <typeparam name="T">로드할 타입</typeparam>
        /// <param name="path">Resources 폴더 기준 경로</param>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 리소스 비동기 로드.
        /// </summary>
        UniTask<T> LoadAsync<T>(string path) where T : Object;

        /// <summary>
        /// 여러 리소스 비동기 로드.
        /// </summary>
        UniTask<T[]> LoadAllAsync<T>(string path) where T : Object;

        #endregion

        #region Instantiate

        /// <summary>
        /// 오브젝트 생성 (풀링 자동 처리).
        /// </summary>
        /// <param name="path">Prefabs/ 폴더 기준 경로</param>
        /// <param name="parent">부모 Transform</param>
        GameObject Instantiate(string path, Transform parent = null);

        /// <summary>
        /// 오브젝트 생성 (위치/회전 지정).
        /// </summary>
        GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// 특정 컴포넌트 가져오며 생성.
        /// </summary>
        T Instantiate<T>(string path, Transform parent = null) where T : Component;

        /// <summary>
        /// 특정 컴포넌트 가져오며 생성 (위치/회전 지정).
        /// </summary>
        T Instantiate<T>(string path, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component;

        /// <summary>
        /// 프리팹으로 직접 생성.
        /// </summary>
        GameObject Instantiate(GameObject prefab, Transform parent = null);

        /// <summary>
        /// 프리팹으로 직접 생성 (위치/회전 지정).
        /// </summary>
        GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);

        #endregion

        #region Destroy

        /// <summary>
        /// 오브젝트 파괴 (풀링이면 자동 반환).
        /// </summary>
        /// <param name="go">파괴할 GameObject</param>
        /// <param name="delay">지연 시간 (초)</param>
        void Destroy(GameObject go, float delay = 0f);

        /// <summary>
        /// 오브젝트 파괴 (컴포넌트 버전).
        /// </summary>
        void Destroy(Component component, float delay = 0f);

        #endregion

        #region Pool Management

        /// <summary>
        /// 풀 생성.
        /// </summary>
        void CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 50);

        /// <summary>
        /// 풀 웜업 (미리 오브젝트 생성).
        /// </summary>
        void WarmUpPool(string key, int count);

        /// <summary>
        /// 풀 웜업 (비동기).
        /// </summary>
        UniTask WarmUpPoolAsync(string key, int count);

        /// <summary>
        /// 특정 풀 정리.
        /// </summary>
        void ClearPool(string key);

        /// <summary>
        /// 모든 풀 정리.
        /// </summary>
        void ClearAllPools();

        /// <summary>
        /// 풀 존재 여부 확인.
        /// </summary>
        bool HasPool(string key);

        #endregion

        #region Cache Management

        /// <summary>
        /// 프리팹 캐시 정리.
        /// </summary>
        void ClearPrefabCache();

        /// <summary>
        /// 특정 프리팹 캐시에서 제거.
        /// </summary>
        void RemoveFromCache(string path);

        #endregion
    }
}
