using KH.Framework2D.UI;
using KH.Framework2D.Pool;
using KH.Framework2D.Services;
using KH.Framework2D.Samples.Demo.Domain;
using KH.Framework2D.Samples.Demo.Presentation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace KH.Framework2D.Samples.Demo.Application
{
    /// <summary>
    /// Root DI container configuration for the game.
    /// Registers all services, models, views, and presenters.
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("=== Core Services ===")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private PoolManager _poolManager;
        
        [Header("=== Views ===")]
        [SerializeField] private ResourceView _resourceView;
        
        protected override void Configure(IContainerBuilder builder)
        {
            // ============================================
            // 1. Core Framework Services
            // ============================================
            if (_uiManager != null)
                builder.RegisterComponent(_uiManager);
            
            if (_poolManager != null)
                builder.RegisterComponent(_poolManager);
            
            // ============================================
            // 2. Domain Layer (Models) - Singleton
            // ============================================
            builder.Register<PlayerResourceModel>(Lifetime.Singleton);
            
            // ============================================
            // 3. Presentation Layer (Views) - From Scene
            // ============================================
            if (_resourceView != null)
                builder.RegisterComponent(_resourceView);
            
            // ============================================
            // 4. Presentation Layer (Presenters) - EntryPoint
            //    VContainer will auto-call Start() and Dispose()
            // ============================================
            builder.RegisterEntryPoint<ResourcePresenter>();
            
            // ============================================
            // 5. Register to ServiceLocator (for non-DI access)
            // ============================================
            builder.RegisterBuildCallback(container =>
            {
                // Allow ScriptableObjects and static methods to access services
                if (_uiManager != null)
                    ServiceLocator.Register(_uiManager);
                
                if (_poolManager != null)
                    ServiceLocator.Register(_poolManager);
            });
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Clean up ServiceLocator
            ServiceLocator.Clear();
        }
    }
}
