using System.Collections.Generic;
using System.Linq;
using KH.Framework2D.Base;
using KH.Framework2D.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.UI
{
    /// <summary>
    /// Manages UI view stack for popup/screen navigation.
    /// Handles show/hide with history tracking.
    /// 
    /// Supports both:
    /// - Legacy BaseView (for backward compatibility)
    /// - New UI_Popup/UI_Scene system (recommended for new UI)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Popup Settings")]
        [SerializeField] private int _popupStartOrder = 10;
        [SerializeField] private int _popupOrderIncrement = 10;
        
        // Legacy BaseView support
        private readonly Stack<BaseView> _history = new();
        
        // New UI_Popup system
        private readonly Stack<UI_Popup> _popupStack = new();
        private readonly Dictionary<string, UI_Popup> _popupCache = new();
        private int _currentPopupOrder;
        
        // UI_Scene system
        private UI_Scene _currentSceneUI;
        private readonly Dictionary<string, UI_Scene> _sceneUICache = new();
        
        #region Properties
        
        /// <summary>
        /// Currently visible top-most view. Null if stack is empty.
        /// </summary>
        public BaseView CurrentView => _history.Count > 0 ? _history.Peek() : null;
        
        /// <summary>
        /// Number of views in the history stack.
        /// </summary>
        public int HistoryCount => _history.Count;
        
        /// <summary>
        /// Currently visible top-most popup.
        /// </summary>
        public UI_Popup CurrentPopup => _popupStack.Count > 0 ? _popupStack.Peek() : null;
        
        /// <summary>
        /// Number of open popups.
        /// </summary>
        public int PopupCount => _popupStack.Count;
        
        /// <summary>
        /// Current scene UI.
        /// </summary>
        public UI_Scene CurrentSceneUI => _currentSceneUI;
        
        #endregion
        
        private void Awake()
        {
            _currentPopupOrder = _popupStartOrder;
        }
        
        #region Legacy BaseView - Sync Methods
        
        /// <summary>
        /// Show a view immediately and add to history.
        /// </summary>
        public void Show<T>(T view) where T : BaseView
        {
            if (view == null) return;
            
            view.Show();
            _history.Push(view);
        }
        
        /// <summary>
        /// Close the most recent view in history.
        /// </summary>
        public void CloseLast()
        {
            if (_history.TryPop(out var view))
            {
                view.Hide();
            }
        }
        
        /// <summary>
        /// Close a specific view and remove from history.
        /// </summary>
        public void Close<T>(T view) where T : BaseView
        {
            if (view == null) return;
            
            view.Hide();
            RemoveFromHistory(view);
        }
        
        /// <summary>
        /// Close all views in history.
        /// </summary>
        public void CloseAll()
        {
            while (_history.TryPop(out var view))
            {
                view.Hide();
            }
        }
        
        #endregion
        
        #region Legacy BaseView - Async Methods
        
        /// <summary>
        /// Show a view with animation and add to history.
        /// </summary>
        public async UniTask ShowAsync<T>(T view) where T : BaseView
        {
            if (view == null) return;
            
            await view.ShowAsync();
            _history.Push(view);
        }
        
        /// <summary>
        /// Close the most recent view with animation.
        /// </summary>
        public async UniTask CloseLastAsync()
        {
            if (_history.TryPop(out var view))
            {
                await view.HideAsync();
            }
        }
        
        /// <summary>
        /// Close a specific view with animation and remove from history.
        /// </summary>
        public async UniTask CloseAsync<T>(T view) where T : BaseView
        {
            if (view == null) return;
            
            await view.HideAsync();
            RemoveFromHistory(view);
        }
        
        /// <summary>
        /// Close all views with animation (in parallel).
        /// </summary>
        public async UniTask CloseAllAsync()
        {
            var tasks = new List<UniTask>();
            
            while (_history.TryPop(out var view))
            {
                tasks.Add(view.HideAsync());
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        #endregion
        
        #region New UI_Popup System
        
        /// <summary>
        /// Show a popup UI (loads from Resources if not cached).
        /// </summary>
        /// <typeparam name="T">Popup type</typeparam>
        /// <param name="name">Resource path under "Prefabs/UI/Popup/" (defaults to type name)</param>
        /// <returns>The popup instance</returns>
        public T ShowPopupUI<T>(string name = null) where T : UI_Popup
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;
            
            // Check cache first
            if (_popupCache.TryGetValue(name, out var cached) && cached != null)
            {
                return ShowCachedPopup(cached) as T;
            }
            
            // Load from Resources
            string path = $"Prefabs/UI/Popup/{name}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Popup prefab not found: {path}");
                return null;
            }
            
            var go = Instantiate(prefab);
            var popup = go.GetComponent<T>();
            if (popup == null)
            {
                Debug.LogError($"[UIManager] Prefab does not have {typeof(T).Name} component: {path}");
                Destroy(go);
                return null;
            }
            
            // Setup and show
            popup.SetCanvas(_currentPopupOrder);
            _currentPopupOrder += _popupOrderIncrement;
            _popupStack.Push(popup);
            _popupCache[name] = popup;
            
            popup.Show();
            return popup;
        }
        
        /// <summary>
        /// Show a popup UI asynchronously with animation.
        /// </summary>
        public async UniTask<T> ShowPopupUIAsync<T>(string name = null) where T : UI_Popup
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;
            
            // Check cache first
            if (_popupCache.TryGetValue(name, out var cached) && cached != null)
            {
                cached.SetCanvas(_currentPopupOrder);
                _currentPopupOrder += _popupOrderIncrement;
                _popupStack.Push(cached);
                await cached.ShowAsync();
                return cached as T;
            }
            
            // Load from Resources
            string path = $"Prefabs/UI/Popup/{name}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Popup prefab not found: {path}");
                return null;
            }
            
            var go = Instantiate(prefab);
            var popup = go.GetComponent<T>();
            if (popup == null)
            {
                Debug.LogError($"[UIManager] Prefab does not have {typeof(T).Name} component: {path}");
                Destroy(go);
                return null;
            }
            
            // Setup and show
            popup.SetCanvas(_currentPopupOrder);
            _currentPopupOrder += _popupOrderIncrement;
            _popupStack.Push(popup);
            _popupCache[name] = popup;
            
            await popup.ShowAsync();
            return popup;
        }
        
        private UI_Popup ShowCachedPopup(UI_Popup popup)
        {
            popup.SetCanvas(_currentPopupOrder);
            _currentPopupOrder += _popupOrderIncrement;
            _popupStack.Push(popup);
            popup.Show();
            return popup;
        }
        
        /// <summary>
        /// Close a specific popup.
        /// </summary>
        public void ClosePopupUI(UI_Popup popup)
        {
            if (popup == null || _popupStack.Count == 0) return;
            
            if (_popupStack.Peek() != popup)
            {
                Debug.LogWarning("[UIManager] Can only close the top-most popup. Use CloseAllPopupUI() to close all.");
                return;
            }
            
            ClosePopupUI();
        }
        
        /// <summary>
        /// Close the top-most popup.
        /// </summary>
        public void ClosePopupUI()
        {
            if (_popupStack.Count == 0) return;
            
            var popup = _popupStack.Pop();
            popup.Hide();
            _currentPopupOrder -= _popupOrderIncrement;
        }
        
        /// <summary>
        /// Close the top-most popup asynchronously with animation.
        /// </summary>
        public async UniTask ClosePopupUIAsync()
        {
            if (_popupStack.Count == 0) return;
            
            var popup = _popupStack.Pop();
            await popup.HideAsync();
            _currentPopupOrder -= _popupOrderIncrement;
        }
        
        /// <summary>
        /// Close all popups.
        /// </summary>
        public void CloseAllPopupUI()
        {
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                popup.Hide();
            }
            _currentPopupOrder = _popupStartOrder;
        }
        
        /// <summary>
        /// Close all popups asynchronously with animation.
        /// </summary>
        public async UniTask CloseAllPopupUIAsync()
        {
            var tasks = new List<UniTask>();
            
            while (_popupStack.Count > 0)
            {
                var popup = _popupStack.Pop();
                tasks.Add(popup.HideAsync());
            }
            
            await UniTask.WhenAll(tasks);
            _currentPopupOrder = _popupStartOrder;
        }
        
        /// <summary>
        /// Check if a specific popup type is currently open.
        /// </summary>
        public bool IsPopupOpen<T>() where T : UI_Popup
        {
            return _popupStack.Any(p => p is T);
        }
        
        /// <summary>
        /// Get an open popup of a specific type.
        /// </summary>
        public T GetOpenPopup<T>() where T : UI_Popup
        {
            return _popupStack.FirstOrDefault(p => p is T) as T;
        }
        
        #endregion
        
        #region New UI_Scene System
        
        /// <summary>
        /// Show a scene UI (loads from Resources if not cached).
        /// </summary>
        public T ShowSceneUI<T>(string name = null) where T : UI_Scene
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;
            
            // Close current scene UI if exists
            if (_currentSceneUI != null)
            {
                _currentSceneUI.Hide();
            }
            
            // Check cache first
            if (_sceneUICache.TryGetValue(name, out var cached) && cached != null)
            {
                _currentSceneUI = cached;
                cached.Show();
                return cached as T;
            }
            
            // Load from Resources
            string path = $"Prefabs/UI/Scene/{name}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Scene UI prefab not found: {path}");
                return null;
            }
            
            var go = Instantiate(prefab);
            var sceneUI = go.GetComponent<T>();
            if (sceneUI == null)
            {
                Debug.LogError($"[UIManager] Prefab does not have {typeof(T).Name} component: {path}");
                Destroy(go);
                return null;
            }
            
            _currentSceneUI = sceneUI;
            _sceneUICache[name] = sceneUI;
            sceneUI.Show();
            
            return sceneUI;
        }
        
        /// <summary>
        /// Set an existing scene UI as current (for manually created scene UIs).
        /// </summary>
        public void SetSceneUI(UI_Scene sceneUI)
        {
            if (_currentSceneUI != null && _currentSceneUI != sceneUI)
            {
                _currentSceneUI.Hide();
            }
            _currentSceneUI = sceneUI;
        }
        
        /// <summary>
        /// Clear current scene UI.
        /// </summary>
        public void ClearSceneUI()
        {
            if (_currentSceneUI != null)
            {
                _currentSceneUI.Hide();
                _currentSceneUI = null;
            }
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Check if a view is in the history stack.
        /// </summary>
        public bool IsInHistory<T>(T view) where T : BaseView
        {
            return _history.Contains(view);
        }
        
        /// <summary>
        /// Navigate back to a specific view, closing all views above it.
        /// </summary>
        public void NavigateBackTo<T>(T targetView) where T : BaseView
        {
            while (_history.Count > 0 && _history.Peek() != targetView)
            {
                var view = _history.Pop();
                view.Hide();
            }
        }
        
        /// <summary>
        /// Navigate back to a specific view with animation.
        /// </summary>
        public async UniTask NavigateBackToAsync<T>(T targetView) where T : BaseView
        {
            var tasks = new List<UniTask>();
            
            while (_history.Count > 0 && _history.Peek() != targetView)
            {
                var view = _history.Pop();
                tasks.Add(view.HideAsync());
            }
            
            await UniTask.WhenAll(tasks);
        }
        
        /// <summary>
        /// Clear all UI (called on scene transition).
        /// </summary>
        public void Clear()
        {
            // Close all legacy views
            CloseAll();
            
            // Close all popups
            CloseAllPopupUI();
            
            // Clear scene UI
            ClearSceneUI();
            
            // Clear caches (objects may be destroyed)
            _popupCache.Clear();
            _sceneUICache.Clear();
            
            Debug.Log("[UIManager] All UI cleared");
        }
        
        #endregion
        
        #region Private Helpers
        
        private void RemoveFromHistory(BaseView view)
        {
            if (!_history.Contains(view)) return;
            
            // Rebuild stack without the target view (preserve order)
            var temp = new Stack<BaseView>();
            
            while (_history.Count > 0)
            {
                var popped = _history.Pop();
                if (popped != view)
                {
                    temp.Push(popped);
                }
            }
            
            while (temp.Count > 0)
            {
                _history.Push(temp.Pop());
            }
        }
        
        #endregion
    }
}
