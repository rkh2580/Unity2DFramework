using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace KH.Framework2D.Data.Pipeline
{
    /// <summary>
    /// XML data parser utility.
    /// Parses XML files exported from Excel into game data objects.
    /// 
    /// Expected XML format:
    /// <DataTable>
    ///   <Row>
    ///     <Id>card_001</Id>
    ///     <Name>Fireball</Name>
    ///     <Damage>50</Damage>
    ///   </Row>
    /// </DataTable>
    /// </summary>
    public static class XmlDataParser
    {
        /// <summary>
        /// Parse XML text into a list of data objects.
        /// </summary>
        /// <typeparam name="T">Target data type</typeparam>
        /// <param name="xmlContent">XML string content</param>
        /// <param name="rowElementName">Name of row elements (default: "Row")</param>
        /// <returns>List of parsed data objects</returns>
        public static List<T> Parse<T>(string xmlContent, string rowElementName = "Row") where T : class, IGameData, new()
        {
            var result = new List<T>();
            
            if (string.IsNullOrEmpty(xmlContent))
            {
                Debug.LogError("[XmlDataParser] XML content is null or empty");
                return result;
            }
            
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                
                var rows = doc.GetElementsByTagName(rowElementName);
                
                foreach (XmlNode row in rows)
                {
                    var data = ParseRow<T>(row);
                    if (data != null)
                    {
                        result.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XmlDataParser] Failed to parse XML: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Parse a single row node into a data object using reflection.
        /// </summary>
        private static T ParseRow<T>(XmlNode rowNode) where T : class, new()
        {
            var data = new T();
            var type = typeof(T);
            
            foreach (XmlNode child in rowNode.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;
                    
                var fieldName = child.Name;
                var value = child.InnerText.Trim();
                
                // Try to find property first, then field
                var property = type.GetProperty(fieldName);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var convertedValue = ConvertValue(value, property.PropertyType);
                        property.SetValue(data, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[XmlDataParser] Failed to set property {fieldName}: {ex.Message}");
                    }
                    continue;
                }
                
                // Try backing field with underscore prefix
                var field = type.GetField($"_{char.ToLower(fieldName[0])}{fieldName.Substring(1)}", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (field == null)
                {
                    // Try exact match
                    field = type.GetField(fieldName, 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                }
                
                if (field != null)
                {
                    try
                    {
                        var convertedValue = ConvertValue(value, field.FieldType);
                        field.SetValue(data, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[XmlDataParser] Failed to set field {fieldName}: {ex.Message}");
                    }
                }
            }
            
            return data;
        }
        
        /// <summary>
        /// Convert string value to target type.
        /// </summary>
        private static object ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return GetDefaultValue(targetType);
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                targetType = underlyingType;
            }
            
            // Primitive types
            if (targetType == typeof(string))
                return value;
                
            if (targetType == typeof(int))
                return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
                
            if (targetType == typeof(float))
                return float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : 0f;
                
            if (targetType == typeof(double))
                return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0.0;
                
            if (targetType == typeof(bool))
            {
                // Support various boolean formats
                var lower = value.ToLowerInvariant();
                return lower == "true" || lower == "1" || lower == "yes" || lower == "y";
            }
            
            if (targetType == typeof(long))
                return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : 0L;
            
            // Enum types
            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, value, ignoreCase: true);
                }
                catch
                {
                    Debug.LogWarning($"[XmlDataParser] Failed to parse enum {targetType.Name} from value: {value}");
                    return Enum.GetValues(targetType).GetValue(0);
                }
            }
            
            // Array types (comma-separated)
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType();
                var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                var array = Array.CreateInstance(elementType, parts.Length);
                
                for (int i = 0; i < parts.Length; i++)
                {
                    array.SetValue(ConvertValue(parts[i].Trim(), elementType), i);
                }
                
                return array;
            }
            
            // List<T> types (comma-separated)
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(targetType);
                var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var part in parts)
                {
                    list.Add(ConvertValue(part.Trim(), elementType));
                }
                
                return list;
            }
            
            // Fallback: try Convert.ChangeType
            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }
        
        /// <summary>
        /// Get default value for a type.
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }
    }
}
