using System;
using System.Collections.Generic;
using UnityEngine;

namespace KH.Framework2D.Services
{
    /// <summary>
    /// Simple service locator for global service access.
    /// Use sparingly - prefer DI via VContainer when possible.
    /// Useful for: accessing services from non-DI contexts (e.g., ScriptableObjects, static methods).
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, Func<object>> _factories = new();
        
        /// <summary>
        /// Register a service instance (singleton).
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Overwriting.");
            }
            
            _services[type] = service;
        }
        
        /// <summary>
        /// Register a factory for lazy instantiation.
        /// </summary>
        public static void RegisterFactory<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }
        
        /// <summary>
        /// Get a registered service.
        /// </summary>
        public static T Get<T>() where T : class
        {
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
            
            Debug.LogError($"[ServiceLocator] Service {type.Name} not found!");
            return null;
        }
        
        /// <summary>
        /// Try to get a service (returns false if not found).
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
        
        /// <summary>
        /// Unregister a service.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            _services.Remove(type);
            _factories.Remove(type);
        }
        
        /// <summary>
        /// Clear all registered services.
        /// Call on application quit or scene transitions if needed.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}
