using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// UI 이벤트 핸들러. UI_Base.BindEvent()로 자동 추가됨.
    /// 모든 주요 포인터 이벤트를 지원.
    /// </summary>
    public class UI_EventHandler : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler,
        IScrollHandler
    {
        #region Event Handlers

        public Action<PointerEventData> OnClickHandler;
        public Action<PointerEventData> OnPointerDownHandler;
        public Action<PointerEventData> OnPointerUpHandler;
        public Action<PointerEventData> OnPointerEnterHandler;
        public Action<PointerEventData> OnPointerExitHandler;
        public Action<PointerEventData> OnDragHandler;
        public Action<PointerEventData> OnBeginDragHandler;
        public Action<PointerEventData> OnEndDragHandler;
        public Action<PointerEventData> OnScrollHandler;

        #endregion

        #region Event Implementations

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickHandler?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownHandler?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpHandler?.Invoke(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterHandler?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitHandler?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragHandler?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragHandler?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragHandler?.Invoke(eventData);
        }

        public void OnScroll(PointerEventData eventData)
        {
            OnScrollHandler?.Invoke(eventData);
        }

        #endregion

        #region Clear Methods

        /// <summary>
        /// 모든 핸들러 해제.
        /// </summary>
        public void ClearAllHandlers()
        {
            OnClickHandler = null;
            OnPointerDownHandler = null;
            OnPointerUpHandler = null;
            OnPointerEnterHandler = null;
            OnPointerExitHandler = null;
            OnDragHandler = null;
            OnBeginDragHandler = null;
            OnEndDragHandler = null;
            OnScrollHandler = null;
        }

        /// <summary>
        /// 클릭 핸들러만 해제.
        /// </summary>
        public void ClearClickHandler()
        {
            OnClickHandler = null;
        }

        /// <summary>
        /// 드래그 관련 핸들러 해제.
        /// </summary>
        public void ClearDragHandlers()
        {
            OnDragHandler = null;
            OnBeginDragHandler = null;
            OnEndDragHandler = null;
        }

        /// <summary>
        /// 포인터 Enter/Exit 핸들러 해제.
        /// </summary>
        public void ClearHoverHandlers()
        {
            OnPointerEnterHandler = null;
            OnPointerExitHandler = null;
        }

        #endregion

        private void OnDisable()
        {
            // 비활성화 시 핸들러 유지 (재사용 가능)
        }

        private void OnDestroy()
        {
            ClearAllHandlers();
        }
    }
}
