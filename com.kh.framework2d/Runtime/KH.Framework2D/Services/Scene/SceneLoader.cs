using System;
using Cysharp.Threading.Tasks;
using KH.Framework2D.UI;
using KH.Framework2D.Services.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KH.Framework2D.Services.Scene
{
    // Define은 KH.Framework2D 네임스페이스에 있음
    using Define = KH.Framework2D.Define;
{
    /// <summary>
    /// Scene loading service with loading screen support.
    /// Integrates with BaseScene for lifecycle management.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneService
    {
        [Header("Loading Screen")]
        [SerializeField] private GameObject _loadingScreen;
        [SerializeField] private UnityEngine.UI.Image _progressBar;
        [SerializeField] private TMPro.TextMeshProUGUI _progressText;
        [SerializeField] private float _minimumLoadTime = 0.5f;
        
        [Header("Settings")]
        [SerializeField] private float _fakeProgressSpeed = 0.5f;
        [SerializeField] private bool _clearManagersOnSceneLoad = true;
        
        public string CurrentSceneName => SceneManager.GetActiveScene().name;
        public bool IsLoading { get; private set; }
        
        /// <summary>
        /// Current scene's BaseScene component (if any).
        /// </summary>
        public BaseScene CurrentScene
        {
            get
            {
                if (_currentScene == null)
                    _currentScene = FindObjectOfType<BaseScene>();
                return _currentScene;
            }
        }
        private BaseScene _currentScene;
        
        public event Action<float> OnLoadProgress;
        public event Action<string> OnSceneLoaded;
        public event Action<string> OnSceneUnloaded;
        
        private void Awake()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        #region Load by Define.Scene
        
        /// <summary>
        /// Load a scene by enum type.
        /// </summary>
        public async UniTask LoadSceneAsync(Define.Scene sceneType, bool showLoadingScreen = true)
        {
            string sceneName = sceneType.ToString();
            await LoadSceneAsync(sceneName, showLoadingScreen);
        }
        
        #endregion
        
        #region Load by Name
        
        /// <summary>
        /// Load a scene with optional loading screen.
        /// </summary>
        public async UniTask LoadSceneAsync(string sceneName, bool showLoadingScreen = true)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneLoader] Already loading a scene!");
                return;
            }
            
            IsLoading = true;
            
            // Clear current scene first
            if (_clearManagersOnSceneLoad)
            {
                ClearCurrentScene();
            }
            
            if (showLoadingScreen)
            {
                ShowLoadingScreen();
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Start async loading
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            
            // Progress update loop
            while (operation.progress < 0.9f)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                UpdateProgress(progress * 0.9f);
                await UniTask.Yield();
            }
            
            // Ensure minimum load time (for UX)
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < _minimumLoadTime)
            {
                float fakeProgress = 0.9f;
                
                while (fakeProgress < 1f)
                {
                    fakeProgress += Time.unscaledDeltaTime * _fakeProgressSpeed;
                    UpdateProgress(Mathf.Min(fakeProgress, 0.99f));
                    await UniTask.Yield();
                }
            }
            
            // Activate scene
            UpdateProgress(1f);
            operation.allowSceneActivation = true;
            
            await UniTask.WaitUntil(() => operation.isDone);
            
            // Reset current scene reference
            _currentScene = null;
            
            if (showLoadingScreen)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
                HideLoadingScreen();
            }
            
            IsLoading = false;
            OnSceneLoaded?.Invoke(sceneName);
        }
        
        #endregion
        
        #region Clear Current Scene
        
        /// <summary>
        /// Clear current scene and all related managers.
        /// Called automatically before scene load if _clearManagersOnSceneLoad is true.
        /// </summary>
        public void ClearCurrentScene()
        {
            // Clear BaseScene
            CurrentScene?.Clear();
            
            // Clear UIManager
            var uiManager = FindObjectOfType<UIManager>();
            uiManager?.Clear();
            
            // Clear InputManager
            var inputManager = FindObjectOfType<InputManager>();
            inputManager?.Clear();
            
            // Note: Pool clearing is optional and should be done explicitly if needed
            // _resourceManager?.ClearAllPools();
            
            Debug.Log("[SceneLoader] Current scene cleared");
        }
        
        #endregion
        
        #region Additive Loading
        
        /// <summary>
        /// Load a scene additively (for multi-scene setups).
        /// </summary>
        public async UniTask LoadSceneAdditiveAsync(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await UniTask.WaitUntil(() => operation.isDone);
            OnSceneLoaded?.Invoke(sceneName);
        }
        
        /// <summary>
        /// Unload an additive scene.
        /// </summary>
        public async UniTask UnloadSceneAsync(string sceneName)
        {
            if (!IsSceneLoaded(sceneName))
            {
                Debug.LogWarning($"[SceneLoader] Scene not loaded: {sceneName}");
                return;
            }
            
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            await UniTask.WaitUntil(() => operation.isDone);
            OnSceneUnloaded?.Invoke(sceneName);
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Reload the current scene.
        /// </summary>
        public async UniTask ReloadCurrentSceneAsync()
        {
            await LoadSceneAsync(CurrentSceneName, true);
        }
        
        /// <summary>
        /// Check if a scene is currently loaded.
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Set the active scene (for lighting, etc.).
        /// </summary>
        public void SetActiveScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
            }
        }
        
        #endregion
        
        #region Loading Screen
        
        private void ShowLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
                UpdateProgress(0f);
            }
        }
        
        private void HideLoadingScreen()
        {
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
        }
        
        private void UpdateProgress(float progress)
        {
            OnLoadProgress?.Invoke(progress);
            
            if (_progressBar != null)
            {
                _progressBar.fillAmount = progress;
            }
            
            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }
        
        #endregion
    }
}
