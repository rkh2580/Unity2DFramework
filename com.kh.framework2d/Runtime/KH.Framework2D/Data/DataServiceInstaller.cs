using Cysharp.Threading.Tasks;
using KH.Framework2D.Data.Pipeline;
using KH.Framework2D.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace KH.Framework2D.Data
{
    /// <summary>
    /// Data service installer for VContainer.
    /// Add this to your LifetimeScope to set up data loading.
    /// 
    /// Usage:
    /// 1. Add DataServiceInstaller to your GameLifetimeScope
    /// 2. Data service automatically loads on game start
    /// 3. Access data via IDataService.Get<CardData>("card_001")
    /// </summary>
    public class DataServiceInstaller : LifetimeScope
    {
        [Header("Data Configuration")]
        [SerializeField] private DataConfig[] _dataConfigs;
        
        [Header("Options")]
        [SerializeField] private bool _loadOnStart = true;
        
        protected override void Configure(IContainerBuilder builder)
        {
            // Create and configure DataService
            var dataService = new DataService();
            
            // Register data types from config
            foreach (var config in _dataConfigs)
            {
                if (!config.Enabled) continue;
                
                RegisterDataType(dataService, config);
            }
            
            // Register as singleton
            builder.RegisterInstance<IDataService>(dataService);
            
            // Auto-load on start
            if (_loadOnStart)
            {
                builder.RegisterBuildCallback(container =>
                {
                    var service = container.Resolve<IDataService>();
                    LoadDataAsync(service).Forget();
                });
            }
            
            // Also register to ServiceLocator for non-DI access
            builder.RegisterBuildCallback(container =>
            {
                ServiceLocator.Register(container.Resolve<IDataService>());
            });
        }
        
        private void RegisterDataType(DataService dataService, DataConfig config)
        {
            // Use reflection to call generic method
            var dataType = config.GetDataType();
            if (dataType == null)
            {
                Debug.LogError($"[DataServiceInstaller] Invalid data type: {config.TypeName}");
                return;
            }
            
            var method = typeof(DataService)
                .GetMethod(nameof(DataService.RegisterDataType))
                .MakeGenericMethod(dataType);
            
            method.Invoke(dataService, new object[] { config.ResourcePath, config.RowElementName });
        }
        
        private async UniTaskVoid LoadDataAsync(IDataService service)
        {
            await service.InitializeAsync();
        }
    }
    
    /// <summary>
    /// Configuration for a data type.
    /// </summary>
    [System.Serializable]
    public class DataConfig
    {
        [Tooltip("Enable/disable this data type")]
        public bool Enabled = true;
        
        [Tooltip("Full type name (e.g., KH.Framework2D.Data.CardData)")]
        public string TypeName;
        
        [Tooltip("Path in Resources folder (without extension)")]
        public string ResourcePath;
        
        [Tooltip("XML row element name")]
        public string RowElementName = "Row";
        
        public System.Type GetDataType()
        {
            if (string.IsNullOrEmpty(TypeName))
                return null;
            
            // Try to find type in all assemblies
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(TypeName);
                if (type != null)
                    return type;
            }
            
            return null;
        }
    }
}
