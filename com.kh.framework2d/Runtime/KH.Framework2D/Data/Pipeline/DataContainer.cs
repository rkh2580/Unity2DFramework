using System;
using System.Collections.Generic;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Generic container for game data.
    /// Stores data in a dictionary for O(1) lookup by ID.
    /// </summary>
    /// <typeparam name="T">Data type implementing IGameData</typeparam>
    public class DataContainer<T> where T : class, IGameData
    {
        private readonly Dictionary<string, T> _dataById = new();
        private readonly List<T> _allData = new();
        
        /// <summary>
        /// Number of entries in this container.
        /// </summary>
        public int Count => _allData.Count;
        
        /// <summary>
        /// All data entries as readonly list.
        /// </summary>
        public IReadOnlyList<T> All => _allData;
        
        /// <summary>
        /// Add a data entry. Logs warning if ID already exists.
        /// </summary>
        public void Add(T data)
        {
            if (data == null)
            {
                Debug.LogError($"[DataContainer<{typeof(T).Name}>] Cannot add null data");
                return;
            }
            
            if (string.IsNullOrEmpty(data.Id))
            {
                Debug.LogError($"[DataContainer<{typeof(T).Name}>] Data has null or empty ID");
                return;
            }
            
            if (_dataById.ContainsKey(data.Id))
            {
                Debug.LogWarning($"[DataContainer<{typeof(T).Name}>] Duplicate ID: {data.Id}. Overwriting.");
                // Remove old entry from list
                _allData.RemoveAll(x => x.Id == data.Id);
            }
            
            _dataById[data.Id] = data;
            _allData.Add(data);
        }
        
        /// <summary>
        /// Add multiple data entries.
        /// </summary>
        public void AddRange(IEnumerable<T> dataList)
        {
            foreach (var data in dataList)
            {
                Add(data);
            }
        }
        
        /// <summary>
        /// Get data by ID. Returns null if not found.
        /// </summary>
        public T Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
                
            return _dataById.TryGetValue(id, out var data) ? data : null;
        }
        
        /// <summary>
        /// Try to get data by ID.
        /// </summary>
        public bool TryGet(string id, out T data)
        {
            if (string.IsNullOrEmpty(id))
            {
                data = null;
                return false;
            }
            
            return _dataById.TryGetValue(id, out data);
        }
        
        /// <summary>
        /// Check if data with ID exists.
        /// </summary>
        public bool Exists(string id)
        {
            return !string.IsNullOrEmpty(id) && _dataById.ContainsKey(id);
        }
        
        /// <summary>
        /// Get filtered data entries.
        /// </summary>
        public List<T> GetWhere(Func<T, bool> predicate)
        {
            var result = new List<T>();
            
            foreach (var data in _allData)
            {
                if (predicate(data))
                    result.Add(data);
            }
            
            return result;
        }
        
        /// <summary>
        /// Clear all data.
        /// </summary>
        public void Clear()
        {
            _dataById.Clear();
            _allData.Clear();
        }
        
        /// <summary>
        /// Remove data by ID.
        /// </summary>
        public bool Remove(string id)
        {
            if (!_dataById.TryGetValue(id, out var data))
                return false;
                
            _dataById.Remove(id);
            _allData.Remove(data);
            return true;
        }
    }
}
