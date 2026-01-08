using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Data service interface for loading and accessing game data.
    /// Abstracts the data source (XML, JSON, ScriptableObject, etc.)
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Initialize and load all data. Call on game start.
        /// </summary>
        UniTask InitializeAsync();
        
        /// <summary>
        /// Check if data service is initialized.
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Get a single data entry by ID.
        /// </summary>
        T Get<T>(string id) where T : class, IGameData;
        
        /// <summary>
        /// Try to get a single data entry by ID.
        /// </summary>
        bool TryGet<T>(string id, out T data) where T : class, IGameData;
        
        /// <summary>
        /// Get all data entries of a specific type.
        /// </summary>
        IReadOnlyList<T> GetAll<T>() where T : class, IGameData;
        
        /// <summary>
        /// Get filtered data entries.
        /// </summary>
        IReadOnlyList<T> GetWhere<T>(Func<T, bool> predicate) where T : class, IGameData;
        
        /// <summary>
        /// Check if data with ID exists.
        /// </summary>
        bool Exists<T>(string id) where T : class, IGameData;
        
        /// <summary>
        /// Get count of data entries for a type.
        /// </summary>
        int Count<T>() where T : class, IGameData;
        
        /// <summary>
        /// Reload specific data type (hot-reload for development).
        /// </summary>
        UniTask ReloadAsync<T>() where T : class, IGameData;
        
        /// <summary>
        /// Event fired when data loading completes.
        /// </summary>
        event Action OnDataLoaded;
        
        /// <summary>
        /// Event fired when data loading progress updates.
        /// </summary>
        event Action<float> OnLoadProgress;
    }
    
    /// <summary>
    /// Base interface for all game data.
    /// All data classes must implement this.
    /// </summary>
    public interface IGameData
    {
        /// <summary>
        /// Unique identifier for this data entry.
        /// Format: {type}_{number} (e.g., "card_001", "unit_archer_01")
        /// </summary>
        string Id { get; }
    }
    
    /// <summary>
    /// Interface for data that can be localized.
    /// </summary>
    public interface ILocalizable
    {
        string NameKey { get; }
        string DescriptionKey { get; }
    }
}
