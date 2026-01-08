using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Data service implementation using XML files from Resources.
    /// 
    /// [IMPROVED] Reduced reflection usage via explicit loader registration.
    /// 
    /// Usage:
    /// 1. Place XML files in Resources/Data/ folder
    /// 2. Register data types with RegisterDataType<T>()
    /// 3. Call InitializeAsync() to load all data
    /// 4. Access data with Get<T>(id) or GetAll<T>()
    /// </summary>
    public class DataService : IDataService
    {
        private readonly Dictionary<Type, object> _containers = new();
        private readonly Dictionary<Type, IDataLoader> _loaders = new();
        
        private bool _isInitialized;
        
        public bool IsInitialized => _isInitialized;
        
        public event Action OnDataLoaded;
        public event Action<float> OnLoadProgress;
        
        /// <summary>
        /// [NEW] Generic data loader interface - eliminates runtime reflection.
        /// </summary>
        private interface IDataLoader
        {
            UniTask LoadAsync(DataService service);
        }
        
        /// <summary>
        /// [NEW] Typed data loader - compile-time type safety.
        /// </summary>
        private class DataLoader<T> : IDataLoader where T : class, IGameData, new()
        {
            private readonly string _resourcePath;
            private readonly string _rowElementName;
            
            public DataLoader(string resourcePath, string rowElementName)
            {
                _resourcePath = resourcePath;
                _rowElementName = rowElementName;
            }
            
            public async UniTask LoadAsync(DataService service)
            {
                // Load XML from Resources
                var textAsset = Resources.Load<TextAsset>(_resourcePath);
                
                if (textAsset == null)
                {
                    Debug.LogError($"[DataService] Failed to load XML: Resources/{_resourcePath}.xml");
                    return;
                }
                
                // Parse using generic method - NO REFLECTION
                var parsedData = XmlDataParser.Parse<T>(textAsset.text, _rowElementName);
                
                // Get or create container
                var container = service.GetOrCreateContainer<T>();
                container.AddRange(parsedData);
                
                // Unload TextAsset to free memory
                Resources.UnloadAsset(textAsset);
                
                // Yield to prevent frame spike
                await UniTask.Yield();
                
                Debug.Log($"[DataService] Loaded {typeof(T).Name} from {_resourcePath} ({parsedData.Count} entries)");
            }
        }
        
        /// <summary>
        /// Register a data type to be loaded from XML.
        /// Must be called before InitializeAsync().
        /// </summary>
        /// <typeparam name="T">Data type implementing IGameData</typeparam>
        /// <param name="resourcePath">Path in Resources folder (e.g., "Data/Cards")</param>
        /// <param name="rowElementName">XML row element name (default: "Row")</param>
        public void RegisterDataType<T>(string resourcePath, string rowElementName = "Row") 
            where T : class, IGameData, new()
        {
            var type = typeof(T);
            
            if (_loaders.ContainsKey(type))
            {
                Debug.LogWarning($"[DataService] Data type {type.Name} already registered. Overwriting.");
            }
            
            // Create typed loader - no reflection needed at load time
            _loaders[type] = new DataLoader<T>(resourcePath, rowElementName);
            
            // Pre-create container
            _containers[type] = new DataContainer<T>();
        }
        
        /// <summary>
        /// Initialize and load all registered data types.
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DataService] Already initialized. Call ReloadAsync to reload.");
                return;
            }
            
            Debug.Log("[DataService] Initializing...");
            
            int total = _loaders.Count;
            int loaded = 0;
            
            foreach (var kvp in _loaders)
            {
                try
                {
                    await kvp.Value.LoadAsync(this);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataService] Failed to load {kvp.Key.Name}: {ex.Message}");
                }
                
                loaded++;
                OnLoadProgress?.Invoke((float)loaded / total);
            }
            
            _isInitialized = true;
            OnDataLoaded?.Invoke();
            
            Debug.Log($"[DataService] Initialized. Loaded {total} data types.");
        }
        
        /// <summary>
        /// Get a single data entry by ID.
        /// </summary>
        public T Get<T>(string id) where T : class, IGameData
        {
            var container = GetContainer<T>();
            return container?.Get(id);
        }
        
        /// <summary>
        /// Try to get a single data entry by ID.
        /// </summary>
        public bool TryGet<T>(string id, out T data) where T : class, IGameData
        {
            var container = GetContainer<T>();
            if (container == null)
            {
                data = null;
                return false;
            }
            
            return container.TryGet(id, out data);
        }
        
        /// <summary>
        /// Get all data entries of a specific type.
        /// </summary>
        public IReadOnlyList<T> GetAll<T>() where T : class, IGameData
        {
            var container = GetContainer<T>();
            return container?.All ?? Array.Empty<T>();
        }
        
        /// <summary>
        /// Get filtered data entries.
        /// </summary>
        public IReadOnlyList<T> GetWhere<T>(Func<T, bool> predicate) where T : class, IGameData
        {
            var container = GetContainer<T>();
            return container?.GetWhere(predicate) ?? new List<T>();
        }
        
        /// <summary>
        /// Check if data with ID exists.
        /// </summary>
        public bool Exists<T>(string id) where T : class, IGameData
        {
            var container = GetContainer<T>();
            return container?.Exists(id) ?? false;
        }
        
        /// <summary>
        /// Get count of data entries for a type.
        /// </summary>
        public int Count<T>() where T : class, IGameData
        {
            var container = GetContainer<T>();
            return container?.Count ?? 0;
        }
        
        /// <summary>
        /// Reload specific data type.
        /// </summary>
        public async UniTask ReloadAsync<T>() where T : class, IGameData, new()
        {
            var type = typeof(T);
            
            if (!_loaders.TryGetValue(type, out var loader))
            {
                Debug.LogError($"[DataService] Data type {type.Name} not registered.");
                return;
            }
            
            // Clear existing data
            var container = GetContainer<T>();
            container?.Clear();
            
            // Reload
            await loader.LoadAsync(this);
            
            Debug.Log($"[DataService] Reloaded {type.Name}");
        }
        
        /// <summary>
        /// Get the container for a data type.
        /// </summary>
        private DataContainer<T> GetContainer<T>() where T : class, IGameData
        {
            var type = typeof(T);
            
            if (_containers.TryGetValue(type, out var container))
            {
                return container as DataContainer<T>;
            }
            
            Debug.LogError($"[DataService] Data type {type.Name} not registered. Call RegisterDataType first.");
            return null;
        }
        
        /// <summary>
        /// Get or create container for a data type.
        /// </summary>
        internal DataContainer<T> GetOrCreateContainer<T>() where T : class, IGameData, new()
        {
            var type = typeof(T);
            
            if (!_containers.TryGetValue(type, out var container))
            {
                container = new DataContainer<T>();
                _containers[type] = container;
            }
            
            return container as DataContainer<T>;
        }
    }
    
    /// <summary>
    /// [NEW] Fluent API for DataService configuration.
    /// More readable than direct RegisterDataType calls.
    /// </summary>
    public static class DataServiceExtensions
    {
        /// <summary>
        /// Register multiple data types fluently.
        /// </summary>
        public static DataService WithData<T>(this DataService service, string resourcePath, string rowElement = "Row")
            where T : class, IGameData, new()
        {
            service.RegisterDataType<T>(resourcePath, rowElement);
            return service;
        }
    }
}
