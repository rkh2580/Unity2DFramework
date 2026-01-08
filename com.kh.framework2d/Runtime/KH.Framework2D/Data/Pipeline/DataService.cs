using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Data service implementation using XML files from Resources.
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
        private readonly Dictionary<Type, DataTypeConfig> _configs = new();
        
        private bool _isInitialized;
        
        public bool IsInitialized => _isInitialized;
        
        public event Action OnDataLoaded;
        public event Action<float> OnLoadProgress;
        
        /// <summary>
        /// Configuration for a data type.
        /// </summary>
        private class DataTypeConfig
        {
            public string ResourcePath;     // Path in Resources folder (without extension)
            public string RowElementName;   // XML row element name
            public Type DataType;
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
            
            if (_configs.ContainsKey(type))
            {
                Debug.LogWarning($"[DataService] Data type {type.Name} already registered. Overwriting.");
            }
            
            _configs[type] = new DataTypeConfig
            {
                ResourcePath = resourcePath,
                RowElementName = rowElementName,
                DataType = type
            };
            
            // Create empty container
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
            
            int total = _configs.Count;
            int loaded = 0;
            
            foreach (var kvp in _configs)
            {
                await LoadDataTypeAsync(kvp.Key, kvp.Value);
                loaded++;
                OnLoadProgress?.Invoke((float)loaded / total);
            }
            
            _isInitialized = true;
            OnDataLoaded?.Invoke();
            
            Debug.Log($"[DataService] Initialized. Loaded {total} data types.");
        }
        
        /// <summary>
        /// Load a single data type from XML.
        /// </summary>
        private async UniTask LoadDataTypeAsync(Type dataType, DataTypeConfig config)
        {
            // Load XML from Resources
            var textAsset = Resources.Load<TextAsset>(config.ResourcePath);
            
            if (textAsset == null)
            {
                Debug.LogError($"[DataService] Failed to load XML: Resources/{config.ResourcePath}.xml");
                return;
            }
            
            // Parse using reflection to call generic method
            var parseMethod = typeof(XmlDataParser)
                .GetMethod(nameof(XmlDataParser.Parse))
                .MakeGenericMethod(dataType);
            
            var parsedData = parseMethod.Invoke(null, new object[] { textAsset.text, config.RowElementName });
            
            // Get container and add data
            var container = _containers[dataType];
            var addRangeMethod = container.GetType()
                .GetMethod(nameof(DataContainer<IGameData>.AddRange));
            
            addRangeMethod.Invoke(container, new[] { parsedData });
            
            // Unload TextAsset to free memory
            Resources.UnloadAsset(textAsset);
            
            // Yield to prevent frame spike
            await UniTask.Yield();
            
            Debug.Log($"[DataService] Loaded {dataType.Name} from {config.ResourcePath}");
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
        public async UniTask ReloadAsync<T>() where T : class, IGameData
        {
            var type = typeof(T);
            
            if (!_configs.TryGetValue(type, out var config))
            {
                Debug.LogError($"[DataService] Data type {type.Name} not registered.");
                return;
            }
            
            // Clear existing data
            var container = GetContainer<T>();
            container?.Clear();
            
            // Reload
            await LoadDataTypeAsync(type, config);
            
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
    }
}
