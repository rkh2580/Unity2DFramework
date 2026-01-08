using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KH.Framework2D.Services
{
    /// <summary>
    /// Service Locator for LIMITED use cases where DI is not possible.
    /// 
    /// ⚠️ USAGE POLICY - READ BEFORE USING ⚠️
    /// 
    /// This is a FALLBACK pattern, not the primary service access method.
    /// Always prefer VContainer DI when possible.
    /// 
    /// ALLOWED USE CASES:
    /// 1. ScriptableObjects (cannot receive constructor injection)
    /// 2. Static utility methods
    /// 3. Editor scripts
    /// 4. Unit tests needing mock services
    /// 5. Legacy code during migration to DI
    /// 
    /// FORBIDDEN USE CASES (use DI instead):
    /// 1. MonoBehaviours - use [Inject] attribute
    /// 2. Regular classes - use constructor injection
    /// 3. Any code that can receive DI
    /// 
    /// REGISTRATION:
    /// Services should be registered by their DI installers, not directly.
    /// This ensures single source of truth for service instances.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();
        
        // Track registration sources for debugging
        private static readonly Dictionary<Type, string> _registrationSources = new();
        
        #region Configuration
        
        /// <summary>
        /// Enable strict mode - logs warnings for potentially improper usage.
        /// Set to false in production for performance.
        /// </summary>
        public static bool StrictModeEnabled { get; set; } = 
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif
        
        /// <summary>
        /// Enable verbose logging.
        /// </summary>
        public static bool VerboseLogging { get; set; } = false;
        
        #endregion
        
        #region Registration
        
        /// <summary>
        /// Register a service instance (singleton).
        /// Should be called from DI installers, not directly from game code.
        /// </summary>
        public static void Register<T>(T service, string source = null) where T : class
        {
            var type = typeof(T);
            
            if (service == null)
            {
                Debug.LogWarning($"[ServiceLocator] Attempted to register null service for {type.Name}");
                return;
            }
            
            if (_services.ContainsKey(type))
            {
                if (StrictModeEnabled)
                {
                    var existingSource = _registrationSources.TryGetValue(type, out var s) ? s : "unknown";
                    Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered by {existingSource}. Overwriting from {source ?? GetCallerInfo()}.");
                }
            }
            
            _services[type] = service;
            _registrationSources[type] = source ?? GetCallerInfo();
            
            if (VerboseLogging)
            {
                Debug.Log($"[ServiceLocator] Registered {type.Name} from {_registrationSources[type]}");
            }
        }
        
        /// <summary>
        /// Register a factory for lazy instantiation.
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory, string source = null) where T : class
        {
            var type = typeof(T);
            _factories[type] = () => factory();
            _registrationSources[type] = source ?? GetCallerInfo();
            
            if (VerboseLogging)
            {
                Debug.Log($"[ServiceLocator] Registered factory for {type.Name}");
            }
        }
        
        #endregion
        
        #region Retrieval
        
        /// <summary>
        /// Get a registered service.
        /// 
        /// ⚠️ Consider using DI ([Inject] or constructor injection) instead.
        /// Only use from ScriptableObjects, static methods, or editor code.
        /// </summary>
        public static T Get<T>() where T : class
        {
            WarnIfImproperUsage<T>();
            
            var type = typeof(T);
            
            // Try direct registration first
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            // Try factory
            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = (T)factory();
                _services[type] = instance; // Cache for next time
                return instance;
            }
            
            Debug.LogError($"[ServiceLocator] Service {type.Name} not found! " +
                          "Ensure it's registered via a DI installer.");
            return null;
        }
        
        /// <summary>
        /// Try to get a service (returns false if not found).
        /// Safer alternative to Get<T>().
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            
            if (_factories.TryGetValue(type, out var factory))
            {
                service = (T)factory();
                _services[type] = service;
                return true;
            }
            
            service = null;
            return false;
        }
        
        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        public static bool Has<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }
        
        #endregion
        
        #region Management
        
        /// <summary>
        /// Unregister a service.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            _services.Remove(type);
            _factories.Remove(type);
            _registrationSources.Remove(type);
            
            if (VerboseLogging)
            {
                Debug.Log($"[ServiceLocator] Unregistered {type.Name}");
            }
        }
        
        /// <summary>
        /// Clear all registered services.
        /// Call on application quit or for testing.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            _factories.Clear();
            _registrationSources.Clear();
            
            if (VerboseLogging)
            {
                Debug.Log("[ServiceLocator] Cleared all services");
            }
        }
        
        #endregion
        
        #region Debugging
        
        /// <summary>
        /// Get debug info about registered services.
        /// </summary>
        public static string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ServiceLocator Debug Info ===");
            sb.AppendLine($"Registered Services: {_services.Count}");
            sb.AppendLine($"Registered Factories: {_factories.Count}");
            sb.AppendLine();
            
            foreach (var kvp in _services)
            {
                var source = _registrationSources.TryGetValue(kvp.Key, out var s) ? s : "unknown";
                sb.AppendLine($"  - {kvp.Key.Name} (Instance) from {source}");
            }
            
            foreach (var kvp in _factories)
            {
                if (!_services.ContainsKey(kvp.Key))
                {
                    var source = _registrationSources.TryGetValue(kvp.Key, out var s) ? s : "unknown";
                    sb.AppendLine($"  - {kvp.Key.Name} (Factory, not instantiated) from {source}");
                }
            }
            
            return sb.ToString();
        }
        
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void WarnIfImproperUsage<T>()
        {
            if (!StrictModeEnabled) return;
            
            // Check if caller is a MonoBehaviour (should use DI instead)
            var stackTrace = new StackTrace(2, true);
            for (int i = 0; i < Math.Min(3, stackTrace.FrameCount); i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                var declaringType = method?.DeclaringType;
                
                if (declaringType != null)
                {
                    // Skip if it's from ServiceLocator itself or common patterns
                    if (declaringType.Name.Contains("ServiceLocator") ||
                        declaringType.Name.Contains("Installer") ||
                        declaringType.Name.Contains("Editor"))
                    {
                        continue;
                    }
                    
                    // Warn if calling from MonoBehaviour
                    if (typeof(MonoBehaviour).IsAssignableFrom(declaringType))
                    {
                        Debug.LogWarning($"[ServiceLocator] ⚠️ {declaringType.Name} is using ServiceLocator.Get<{typeof(T).Name}>(). " +
                                        "Consider using [Inject] attribute or constructor injection instead.");
                        break;
                    }
                }
            }
        }
        
        private static string GetCallerInfo()
        {
            var stackTrace = new StackTrace(2, true);
            var frame = stackTrace.GetFrame(0);
            if (frame != null)
            {
                var method = frame.GetMethod();
                return $"{method?.DeclaringType?.Name}.{method?.Name}";
            }
            return "unknown";
        }
        
        #endregion
    }
}
