using Cysharp.Threading.Tasks;
using DG.Tweening;
using KH.Framework2D.Services;
using UnityEngine;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// 팝업 UI 기반 클래스. UIManager의 스택으로 관리됨.
    /// 기존 BaseView의 Show/Hide 기능 통합.
    /// 
    /// 특징:
    /// - 자동 Canvas 설정 (sortingOrder 자동 관리)
    /// - 페이드 인/아웃 애니메이션 지원
    /// - UIManager를 통한 스택 기반 관리
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UI_Popup : UI_Base
    {
        [Header("Popup Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private int _sortOrderOffset = 0; // 같은 레벨 팝업 간 순서 조정용
        [SerializeField] private bool _closeOnBackgroundClick = false;

        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private RectTransform _rectTransform;
        
        /// <summary>
        /// SortOrder 오프셋 (UIManager에서 사용).
        /// </summary>
        public int SortOrderOffset => _sortOrderOffset;
        
        /// <summary>
        /// 현재 팝업이 보이는 상태인지.
        /// </summary>
        public bool IsVisible => gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0;

        /// <summary>
        /// 팝업 RectTransform.
        /// </summary>
        public RectTransform RectTransform => _rectTransform;

        protected override void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();
            
            base.Awake();
        }

        protected override void Init()
        {
            base.Init();
            // UIManager에서 SetCanvas 호출됨
        }

        #region Canvas Setup

        /// <summary>
        /// Canvas 설정 (UIManager에서 호출).
        /// </summary>
        /// <param name="sortOrder">기본 sortingOrder</param>
        public void SetCanvas(int sortOrder)
        {
            if (_canvas == null)
                _canvas = gameObject.AddComponent<Canvas>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortOrder + _sortOrderOffset;

            // GraphicRaycaster 확보 (클릭 이벤트용)
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        #endregion

        #region Show/Hide (기존 BaseView 호환)

        /// <summary>
        /// 즉시 표시 (애니메이션 없음).
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            SetInteractable(true);
            OnShow();
        }

        /// <summary>
        /// 즉시 숨김 (애니메이션 없음).
        /// </summary>
        public virtual void Hide()
        {
            OnHide();
            SetInteractable(false);
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 페이드 인 애니메이션과 함께 표시.
        /// </summary>
        public virtual async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            SetInteractable(false);

            await _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(_fadeEase)
                .SetUpdate(true) // TimeScale 영향 안 받음
                .AsyncWaitForCompletion();

            SetInteractable(true);
            OnShow();
        }

        /// <summary>
        /// 페이드 아웃 애니메이션과 함께 숨김.
        /// </summary>
        public virtual async UniTask HideAsync()
        {
            OnHide();
            SetInteractable(false);

            await _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(_fadeEase)
                .SetUpdate(true)
                .AsyncWaitForCompletion();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 상호작용 가능 여부 설정.
        /// </summary>
        protected void SetInteractable(bool value)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = value;
                _canvasGroup.blocksRaycasts = value;
            }
        }

        /// <summary>
        /// 팝업이 표시된 후 호출. 자식에서 오버라이드.
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// 팝업이 숨겨지기 전 호출. 자식에서 오버라이드.
        /// </summary>
        protected virtual void OnHide() { }

        #endregion

        #region Popup Management

        /// <summary>
        /// 팝업 닫기 (UIManager 통해).
        /// </summary>
        public virtual void ClosePopup()
        {
            // UIManager가 등록되어 있다면 사용
            if (ServiceLocator.TryGet<UIManager>(out var uiManager))
            {
                uiManager.ClosePopupUI(this);
            }
            else
            {
                // 직접 닫기 (비권장)
                Debug.LogWarning("[UI_Popup] UIManager가 등록되지 않음. 직접 Hide 호출.");
                Hide();
            }
        }

        /// <summary>
        /// 팝업 닫기 (애니메이션 포함).
        /// </summary>
        public virtual async UniTask ClosePopupAsync()
        {
            if (ServiceLocator.TryGet<UIManager>(out var uiManager))
            {
                await uiManager.ClosePopupUIAsync(this);
            }
            else
            {
                await HideAsync();
            }
        }

        /// <summary>
        /// 뒤로가기/ESC 키 입력 시 호출.
        /// </summary>
        public virtual void OnBackPressed()
        {
            ClosePopup();
        }

        #endregion

        #region Scale Animation (Optional)

        /// <summary>
        /// 스케일 애니메이션과 함께 표시.
        /// </summary>
        public async UniTask ShowWithScaleAsync(float startScale = 0.8f)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * startScale;
            SetInteractable(false);

            await UniTask.WhenAll(
                _canvasGroup.DOFade(1f, _fadeDuration).SetEase(_fadeEase).SetUpdate(true).AsyncWaitForCompletion(),
                transform.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack).SetUpdate(true).AsyncWaitForCompletion()
            );

            SetInteractable(true);
            OnShow();
        }

        /// <summary>
        /// 스케일 애니메이션과 함께 숨김.
        /// </summary>
        public async UniTask HideWithScaleAsync(float endScale = 0.8f)
        {
            OnHide();
            SetInteractable(false);

            await UniTask.WhenAll(
                _canvasGroup.DOFade(0f, _fadeDuration).SetEase(_fadeEase).SetUpdate(true).AsyncWaitForCompletion(),
                transform.DOScale(endScale, _fadeDuration).SetEase(Ease.InBack).SetUpdate(true).AsyncWaitForCompletion()
            );

            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        #endregion

        protected override void OnDestroy()
        {
            _canvasGroup?.DOKill();
            transform.DOKill();
            base.OnDestroy();
        }
    }
}
