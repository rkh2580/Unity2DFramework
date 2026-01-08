using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KH.Framework2D.Services.Scene
{
    // Define은 KH.Framework2D 네임스페이스에 있음
    using Define = KH.Framework2D.Define;
{
    /// <summary>
    /// 모든 게임 씬의 기반 클래스.
    /// 각 씬의 @Scene 오브젝트에 해당 씬 스크립트를 붙여 사용.
    /// 
    /// 기능:
    /// - 씬 초기화 (OnSceneInit) - 비동기 지원
    /// - 씬 정리 (OnSceneClear) - 씬 전환 전 호출
    /// - EventSystem 자동 생성
    /// 
    /// 사용법:
    /// 1. 씬별 스크립트 생성 (예: GameScene : BaseScene)
    /// 2. SceneType 오버라이드
    /// 3. OnSceneInit, OnSceneClear 구현
    /// 4. 씬에 빈 GameObject(@Scene) 생성 후 스크립트 부착
    /// </summary>
    public abstract class BaseScene : MonoBehaviour
    {
        /// <summary>
        /// 이 씬의 타입 (자식에서 정의).
        /// </summary>
        public abstract Define.Scene SceneType { get; }

        /// <summary>
        /// 씬 초기화 완료 여부.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 씬 정리 완료 여부.
        /// </summary>
        public bool IsCleared { get; private set; }

        protected virtual void Awake()
        {
            InitializeScene().Forget();
        }

        private async UniTaskVoid InitializeScene()
        {
            // 공통 초기화
            EnsureEventSystem();

            // 자식 씬 초기화
            try
            {
                await OnSceneInit();
                IsInitialized = true;
                Debug.Log($"[BaseScene] {SceneType} 씬 초기화 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BaseScene] {SceneType} 씬 초기화 실패: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// EventSystem이 없으면 생성.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("@EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                Debug.Log("[BaseScene] EventSystem 자동 생성됨");
            }
        }

        /// <summary>
        /// 씬 초기화 (자식에서 구현).
        /// 비동기 작업 (데이터 로드, UI 생성 등) 수행.
        /// </summary>
        protected virtual UniTask OnSceneInit()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 씬 정리 (씬 전환 전 SceneLoader에서 호출).
        /// </summary>
        public void Clear()
        {
            if (IsCleared) return;
            
            try
            {
                OnSceneClear();
                IsCleared = true;
                Debug.Log($"[BaseScene] {SceneType} 씬 정리 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BaseScene] {SceneType} 씬 정리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 씬 정리 로직 (자식에서 구현).
        /// 저장, 리소스 정리 등 수행.
        /// </summary>
        protected virtual void OnSceneClear() { }

        protected virtual void OnDestroy()
        {
            if (!IsCleared && IsInitialized)
            {
                Clear();
            }
        }
    }

    #region Example Scene Implementations

    /// <summary>
    /// 타이틀 씬 예시.
    /// </summary>
    // public class TitleScene : BaseScene
    // {
    //     public override Define.Scene SceneType => Define.Scene.Title;
    //
    //     protected override UniTask OnSceneInit()
    //     {
    //         // 타이틀 UI 표시
    //         return UniTask.CompletedTask;
    //     }
    //
    //     protected override void OnSceneClear()
    //     {
    //         // 정리
    //     }
    // }

    /// <summary>
    /// 게임 씬 예시.
    /// </summary>
    // public class GameScene : BaseScene
    // {
    //     public override Define.Scene SceneType => Define.Scene.Game;
    //
    //     protected override async UniTask OnSceneInit()
    //     {
    //         // 데이터 로드
    //         // UI 생성
    //         // 플레이어 스폰
    //         await UniTask.CompletedTask;
    //     }
    //
    //     protected override void OnSceneClear()
    //     {
    //         // 진행 상황 저장
    //         // 리소스 정리
    //     }
    // }

    #endregion
}
