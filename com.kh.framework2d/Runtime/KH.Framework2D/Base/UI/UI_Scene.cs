using UnityEngine;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// 고정 UI 기반 클래스. 씬에 항상 존재하는 HUD 등에 사용.
    /// 팝업과 달리 스택으로 관리되지 않으며, 항상 팝업보다 아래에 렌더링됨.
    /// 
    /// 사용 예시:
    /// - 게임 HUD (체력바, 점수, 미니맵 등)
    /// - 인게임 상단/하단 고정 UI
    /// - 채팅창
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public abstract class UI_Scene : UI_Base
    {
        [Header("Scene UI Settings")]
        [SerializeField] private int _sortOrder = 0; // 씬 UI 간 순서
        
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Canvas 참조.
        /// </summary>
        protected Canvas Canvas => _canvas;

        /// <summary>
        /// CanvasGroup 참조 (있는 경우).
        /// </summary>
        protected CanvasGroup CanvasGroup => _canvasGroup;

        protected override void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            base.Awake();
        }

        protected override void Init()
        {
            base.Init();
            SetupCanvas();
        }

        /// <summary>
        /// Canvas 초기 설정.
        /// </summary>
        protected virtual void SetupCanvas()
        {
            if (_canvas == null)
                _canvas = gameObject.AddComponent<Canvas>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _sortOrder; // 팝업(10+)보다 낮은 값

            // GraphicRaycaster 확보
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        #region Show/Hide

        /// <summary>
        /// UI 표시.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            OnShow();
        }

        /// <summary>
        /// UI 숨김.
        /// </summary>
        public virtual void Hide()
        {
            OnHide();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 보이는 상태 토글.
        /// </summary>
        public void Toggle()
        {
            if (gameObject.activeSelf)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// UI가 표시된 후 호출.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// UI가 숨겨지기 전 호출.
        /// </summary>
        protected virtual void OnHide() { }

        #endregion

        #region Visibility Control

        /// <summary>
        /// 알파값만 변경 (SetActive 없이).
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// 상호작용 가능 여부 설정.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        #endregion
    }
}
