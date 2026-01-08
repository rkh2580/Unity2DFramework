using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using KH.Framework2D.Utils;

namespace KH.Framework2D.UI
{
    // Define은 KH.Framework2D 네임스페이스에 있음
    using Define = KH.Framework2D.Define;
{
    /// <summary>
    /// 모든 UI의 기반 클래스. enum 기반 자동 바인딩 지원.
    /// 기존 BaseView와 공존 가능 (점진적 마이그레이션).
    /// 
    /// 사용법:
    /// 1. enum으로 바인딩할 UI 요소 이름 정의 (오브젝트 이름과 일치해야 함)
    /// 2. Init()에서 Bind&lt;T&gt;(typeof(EnumType)) 호출
    /// 3. Get&lt;T&gt;((int)EnumType.Name) 또는 헬퍼 메서드로 접근
    /// </summary>
    public abstract class UI_Base : MonoBehaviour
    {
        // Type별로 바인딩된 오브젝트/컴포넌트 저장
        protected Dictionary<Type, UnityEngine.Object[]> _objects = new();
        
        private bool _isInitialized = false;

        /// <summary>
        /// 초기화 완료 여부.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        protected virtual void Awake()
        {
            Init();
        }

        /// <summary>
        /// 자식 클래스에서 오버라이드하여 Bind 호출.
        /// base.Init() 호출 필수.
        /// </summary>
        protected virtual void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        #region Bind Methods

        /// <summary>
        /// enum에 정의된 이름과 일치하는 자식 오브젝트를 찾아 바인딩.
        /// </summary>
        /// <typeparam name="T">바인딩할 컴포넌트 타입</typeparam>
        /// <param name="enumType">이름이 정의된 enum 타입</param>
        protected void Bind<T>(Type enumType) where T : UnityEngine.Object
        {
            string[] names = Enum.GetNames(enumType);
            var objects = new UnityEngine.Object[names.Length];
            _objects[typeof(T)] = objects;

            for (int i = 0; i < names.Length; i++)
            {
                if (typeof(T) == typeof(GameObject))
                {
                    objects[i] = FindChild(gameObject, names[i], recursive: true);
                }
                else
                {
                    objects[i] = FindChild<T>(gameObject, names[i], recursive: true);
                }

                if (objects[i] == null)
                {
                    Debug.LogWarning($"[UI_Base] 바인딩 실패: {names[i]} ({typeof(T).Name}) in {gameObject.name}");
                }
            }
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// 바인딩된 오브젝트 가져오기.
        /// </summary>
        /// <typeparam name="T">컴포넌트 타입</typeparam>
        /// <param name="idx">enum 인덱스 (int로 캐스팅)</param>
        protected T Get<T>(int idx) where T : UnityEngine.Object
        {
            if (!_objects.TryGetValue(typeof(T), out var objects))
                return null;

            if (idx < 0 || idx >= objects.Length)
                return null;

            return objects[idx] as T;
        }

        // 자주 쓰는 타입용 헬퍼 메서드
        protected GameObject GetObject(int idx) => Get<GameObject>(idx);
        protected Text GetText(int idx) => Get<Text>(idx);
        protected TMP_Text GetTMPText(int idx) => Get<TMP_Text>(idx);
        protected TextMeshProUGUI GetTextMeshPro(int idx) => Get<TextMeshProUGUI>(idx);
        protected Button GetButton(int idx) => Get<Button>(idx);
        protected Image GetImage(int idx) => Get<Image>(idx);
        protected RawImage GetRawImage(int idx) => Get<RawImage>(idx);
        protected Slider GetSlider(int idx) => Get<Slider>(idx);
        protected Toggle GetToggle(int idx) => Get<Toggle>(idx);
        protected InputField GetInputField(int idx) => Get<InputField>(idx);
        protected TMP_InputField GetTMPInputField(int idx) => Get<TMP_InputField>(idx);
        protected Dropdown GetDropdown(int idx) => Get<Dropdown>(idx);
        protected TMP_Dropdown GetTMPDropdown(int idx) => Get<TMP_Dropdown>(idx);
        protected ScrollRect GetScrollRect(int idx) => Get<ScrollRect>(idx);
        protected RectTransform GetRectTransform(int idx) => Get<RectTransform>(idx);
        protected CanvasGroup GetCanvasGroup(int idx) => Get<CanvasGroup>(idx);

        #endregion

        #region Event Binding

        /// <summary>
        /// UI 오브젝트에 이벤트 핸들러 바인딩.
        /// </summary>
        /// <param name="go">대상 GameObject</param>
        /// <param name="action">이벤트 핸들러</param>
        /// <param name="eventType">이벤트 타입</param>
        public static void BindEvent(
            GameObject go, 
            Action<PointerEventData> action, 
            Define.UIEvent eventType = Define.UIEvent.Click)
        {
            if (go == null) return;
            
            var handler = go.GetOrAddComponent<UI_EventHandler>();

            switch (eventType)
            {
                case Define.UIEvent.Click:
                    handler.OnClickHandler -= action;
                    handler.OnClickHandler += action;
                    break;
                case Define.UIEvent.Drag:
                    handler.OnDragHandler -= action;
                    handler.OnDragHandler += action;
                    break;
                case Define.UIEvent.BeginDrag:
                    handler.OnBeginDragHandler -= action;
                    handler.OnBeginDragHandler += action;
                    break;
                case Define.UIEvent.EndDrag:
                    handler.OnEndDragHandler -= action;
                    handler.OnEndDragHandler += action;
                    break;
                case Define.UIEvent.PointerEnter:
                    handler.OnPointerEnterHandler -= action;
                    handler.OnPointerEnterHandler += action;
                    break;
                case Define.UIEvent.PointerExit:
                    handler.OnPointerExitHandler -= action;
                    handler.OnPointerExitHandler += action;
                    break;
                case Define.UIEvent.PointerDown:
                    handler.OnPointerDownHandler -= action;
                    handler.OnPointerDownHandler += action;
                    break;
                case Define.UIEvent.PointerUp:
                    handler.OnPointerUpHandler -= action;
                    handler.OnPointerUpHandler += action;
                    break;
            }
        }

        /// <summary>
        /// 이벤트 핸들러 해제.
        /// </summary>
        public static void UnbindEvent(
            GameObject go,
            Action<PointerEventData> action,
            Define.UIEvent eventType = Define.UIEvent.Click)
        {
            if (go == null) return;
            
            var handler = go.GetComponent<UI_EventHandler>();
            if (handler == null) return;

            switch (eventType)
            {
                case Define.UIEvent.Click:
                    handler.OnClickHandler -= action;
                    break;
                case Define.UIEvent.Drag:
                    handler.OnDragHandler -= action;
                    break;
                case Define.UIEvent.BeginDrag:
                    handler.OnBeginDragHandler -= action;
                    break;
                case Define.UIEvent.EndDrag:
                    handler.OnEndDragHandler -= action;
                    break;
                case Define.UIEvent.PointerEnter:
                    handler.OnPointerEnterHandler -= action;
                    break;
                case Define.UIEvent.PointerExit:
                    handler.OnPointerExitHandler -= action;
                    break;
                case Define.UIEvent.PointerDown:
                    handler.OnPointerDownHandler -= action;
                    break;
                case Define.UIEvent.PointerUp:
                    handler.OnPointerUpHandler -= action;
                    break;
            }
        }

        /// <summary>
        /// 단순 클릭 이벤트 바인딩 (간편 버전).
        /// </summary>
        public static void BindClick(GameObject go, Action onClick)
        {
            BindEvent(go, _ => onClick?.Invoke(), Define.UIEvent.Click);
        }

        #endregion

        #region Find Utilities

        /// <summary>
        /// 자식 오브젝트에서 특정 컴포넌트를 가진 오브젝트 찾기.
        /// </summary>
        /// <typeparam name="T">찾을 컴포넌트 타입</typeparam>
        /// <param name="go">부모 GameObject</param>
        /// <param name="name">찾을 이름 (null이면 아무거나)</param>
        /// <param name="recursive">자손까지 검색할지 여부</param>
        public static T FindChild<T>(
            GameObject go, 
            string name = null, 
            bool recursive = false) where T : UnityEngine.Object
        {
            if (go == null) return null;

            if (!recursive)
            {
                // 직계 자식만 검색
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    Transform child = go.transform.GetChild(i);
                    if (string.IsNullOrEmpty(name) || child.name == name)
                    {
                        T component = child.GetComponent<T>();
                        if (component != null)
                            return component;
                    }
                }
            }
            else
            {
                // 모든 자손 검색
                foreach (T component in go.GetComponentsInChildren<T>(true))
                {
                    if (string.IsNullOrEmpty(name) || component.name == name)
                        return component;
                }
            }

            return null;
        }

        /// <summary>
        /// 자식 오브젝트 중 이름이 일치하는 GameObject 찾기.
        /// </summary>
        public static GameObject FindChild(
            GameObject go, 
            string name = null, 
            bool recursive = false)
        {
            Transform transform = FindChild<Transform>(go, name, recursive);
            return transform?.gameObject;
        }

        /// <summary>
        /// 특정 조건에 맞는 자식 찾기.
        /// </summary>
        public static Transform FindChildWhere(GameObject go, Func<Transform, bool> predicate, bool recursive = true)
        {
            if (go == null || predicate == null) return null;

            if (!recursive)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i);
                    if (predicate(child))
                        return child;
                }
            }
            else
            {
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                {
                    if (child != go.transform && predicate(child))
                        return child;
                }
            }

            return null;
        }

        #endregion

        #region Lifecycle

        protected virtual void OnDestroy()
        {
            _objects.Clear();
        }

        #endregion
    }
}
