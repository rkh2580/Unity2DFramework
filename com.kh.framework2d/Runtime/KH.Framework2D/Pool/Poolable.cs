using UnityEngine;

namespace KH.Framework2D.Pool
{
    /// <summary>
    /// 풀링 대상임을 표시하는 마커 컴포넌트.
    /// 이 컴포넌트가 붙은 프리팹은 ResourceManager에서 자동으로 풀링 처리됨.
    /// 
    /// 사용법:
    /// 1. 풀링할 프리팹에 Poolable 컴포넌트 추가
    /// 2. ResourceManager.Instantiate() 호출 시 자동으로 풀에서 가져옴
    /// 3. ResourceManager.Destroy() 호출 시 자동으로 풀에 반환됨
    /// </summary>
    [DisallowMultipleComponent]
    public class Poolable : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("풀 자동 생성 시 초기 개수")]
        [SerializeField] private int _suggestedInitialSize = 10;
        
        [Tooltip("풀 자동 생성 시 최대 개수")]
        [SerializeField] private int _suggestedMaxSize = 50;

        [Header("Debug (Read Only)")]
        [Tooltip("풀에서 대기 중인지 여부")]
        [SerializeField] private bool _isPooled;

        /// <summary>
        /// 현재 풀에서 대기 중인지 여부.
        /// </summary>
        public bool IsPooled
        {
            get => _isPooled;
            internal set => _isPooled = value;
        }

        /// <summary>
        /// 원본 프리팹 이름 (풀 키로 사용).
        /// </summary>
        public string OriginalName { get; internal set; }

        /// <summary>
        /// 권장 초기 풀 크기.
        /// </summary>
        public int SuggestedInitialSize => _suggestedInitialSize;

        /// <summary>
        /// 권장 최대 풀 크기.
        /// </summary>
        public int SuggestedMaxSize => _suggestedMaxSize;

        #region Lifecycle Callbacks

        /// <summary>
        /// 풀에서 꺼낼 때 호출됨. 자식에서 오버라이드하여 초기화 로직 구현.
        /// </summary>
        public virtual void OnSpawned()
        {
            _isPooled = false;
        }

        /// <summary>
        /// 풀에 반환될 때 호출됨. 자식에서 오버라이드하여 정리 로직 구현.
        /// </summary>
        public virtual void OnDespawned()
        {
            _isPooled = true;
        }

        #endregion

        #region Manual Pool Return

        /// <summary>
        /// 풀로 직접 반환. PooledHandle이 있으면 사용, 없으면 비활성화만.
        /// </summary>
        public void ReturnToPool()
        {
            var handle = GetComponent<PooledHandle>();
            if (handle != null)
            {
                handle.TryReturnToPool();
            }
            else
            {
                // PooledHandle이 없으면 단순 비활성화
                OnDespawned();
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 지정 시간 후 풀로 반환.
        /// </summary>
        public void ReturnToPoolDelayed(float delay)
        {
            if (delay <= 0f)
            {
                ReturnToPool();
                return;
            }

            StartCoroutine(ReturnDelayedCoroutine(delay));
        }

        private System.Collections.IEnumerator ReturnDelayedCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool();
        }

        #endregion

        protected virtual void OnDisable()
        {
            // 비활성화 시 풀 상태로 간주
            _isPooled = true;
        }

        protected virtual void OnEnable()
        {
            // 활성화 시 비풀 상태로 간주
            _isPooled = false;
        }
    }
}
