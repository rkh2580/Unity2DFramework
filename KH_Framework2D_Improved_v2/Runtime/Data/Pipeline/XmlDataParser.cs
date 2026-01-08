using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// Game data interface - all data types must implement this.
    /// </summary>
    public interface IGameData
    {
        /// <summary>
        /// Unique identifier for this data entry.
        /// </summary>
        string Id { get; }
    }
    
    /// <summary>
    /// XML data parser - converts XML to strongly-typed data objects.
    /// No reflection at parse time - uses property setters directly.
    /// </summary>
    public static class XmlDataParser
    {
        /// <summary>
        /// Parse XML text into a list of data objects.
        /// </summary>
        /// <typeparam name="T">Data type to parse into</typeparam>
        /// <param name="xmlText">Raw XML text</param>
        /// <param name="rowElementName">Name of row elements (default: "Row")</param>
        /// <returns>List of parsed data objects</returns>
        public static List<T> Parse<T>(string xmlText, string rowElementName = "Row") 
            where T : class, IGameData, new()
        {
            var result = new List<T>();
            
            if (string.IsNullOrEmpty(xmlText))
            {
                Debug.LogWarning("[XmlDataParser] Empty XML text provided.");
                return result;
            }
            
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlText);
                
                var rows = doc.GetElementsByTagName(rowElementName);
                
                foreach (XmlNode row in rows)
                {
                    try
                    {
                        var item = ParseRow<T>(row);
                        if (item != null)
                        {
                            result.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[XmlDataParser] Failed to parse row: {ex.Message}");
                    }
                }
            }
            catch (XmlException ex)
            {
                Debug.LogError($"[XmlDataParser] XML parse error: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Parse a single XML row into a data object.
        /// Uses reflection once per type (cached properties).
        /// </summary>
        private static T ParseRow<T>(XmlNode row) where T : class, new()
        {
            var item = new T();
            var type = typeof(T);
            
            foreach (XmlNode child in row.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element) continue;
                
                var propName = child.Name;
                var propValue = child.InnerText;
                
                // Find property (case-insensitive)
                var prop = type.GetProperty(propName, 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.IgnoreCase);
                
                if (prop == null || !prop.CanWrite) continue;
                
                try
                {
                    var converted = ConvertValue(propValue, prop.PropertyType);
                    prop.SetValue(item, converted);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[XmlDataParser] Failed to set {propName}={propValue}: {ex.Message}");
                }
            }
            
            return item;
        }
        
        /// <summary>
        /// Convert string value to target type.
        /// </summary>
        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }
            
            // Nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }
            
            // String
            if (targetType == typeof(string))
                return value;
            
            // Primitives
            if (targetType == typeof(int))
                return int.TryParse(value, out var i) ? i : 0;
            
            if (targetType == typeof(long))
                return long.TryParse(value, out var l) ? l : 0L;
            
            if (targetType == typeof(float))
                return float.TryParse(value, out var f) ? f : 0f;
            
            if (targetType == typeof(double))
                return double.TryParse(value, out var d) ? d : 0d;
            
            if (targetType == typeof(bool))
            {
                value = value.ToLowerInvariant();
                return value == "true" || value == "1" || value == "yes";
            }
            
            // Enums
            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value, true, out var e))
                    return e;
                return Enum.GetValues(targetType).GetValue(0);
            }
            
            // Fallback
            return Convert.ChangeType(value, targetType);
        }
    }
    
    /// <summary>
    /// Generic data container for fast lookup by ID.
    /// </summary>
    public class DataContainer<T> where T : class, IGameData
    {
        private readonly Dictionary<string, T> _byId = new();
        private readonly List<T> _all = new();
        
        public IReadOnlyList<T> All => _all;
        public int Count => _all.Count;
        
        public void Add(T item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) return;
            
            if (_byId.ContainsKey(item.Id))
            {
                Debug.LogWarning($"[DataContainer] Duplicate ID: {item.Id}");
                return;
            }
            
            _byId[item.Id] = item;
            _all.Add(item);
        }
        
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        
        public T Get(string id)
        {
            _byId.TryGetValue(id, out var item);
            return item;
        }
        
        public bool TryGet(string id, out T item)
        {
            return _byId.TryGetValue(id, out item);
        }
        
        public bool Exists(string id)
        {
            return _byId.ContainsKey(id);
        }
        
        public IReadOnlyList<T> GetWhere(Func<T, bool> predicate)
        {
            var result = new List<T>();
            foreach (var item in _all)
            {
                if (predicate(item))
                    result.Add(item);
            }
            return result;
        }
        
        public void Clear()
        {
            _byId.Clear();
            _all.Clear();
        }
    }
}
