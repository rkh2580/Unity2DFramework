using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace KH.Framework2D.Services.Save
{
    /// <summary>
    /// Save/Load service supporting PlayerPrefs and JSON file storage.
    /// </summary>
    public class SaveManager : ISaveService
    {
        private const string PrefixInt = "__i:";
        private const string PrefixLong = "__l:";
        private const string PrefixFloat = "__f:";
        private const string PrefixDouble = "__d:";
        private const string PrefixBool = "__b:";
        private const string PrefixString = "__s:";
        private const string PrefixJson = "__j:";

        private readonly string _saveDirectory;
        private readonly bool _useEncryption;
        private readonly string _encryptionKey;
        
        public SaveManager(bool useEncryption = false, string encryptionKey = "")
        {
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            _useEncryption = useEncryption;
            _encryptionKey = encryptionKey;
            
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }
        
        #region PlayerPrefs (Simple Values)
        
        /// <summary>
        /// Save data using PlayerPrefs (for simple values).
        /// </summary>
        public void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Save key cannot be null/empty.", nameof(key));

            // JsonUtility cannot serialize primitives directly; store a typed payload.
            string payload = data switch
            {
                int i => PrefixInt + i.ToString(CultureInfo.InvariantCulture),
                long l => PrefixLong + l.ToString(CultureInfo.InvariantCulture),
                float f => PrefixFloat + f.ToString("R", CultureInfo.InvariantCulture),
                double d => PrefixDouble + d.ToString("R", CultureInfo.InvariantCulture),
                bool b => PrefixBool + (b ? "1" : "0"),
                string s => PrefixString + s,
                null => null,
                _ => PrefixJson + JsonUtility.ToJson(data)
            };

            if (payload == null)
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                return;
            }

            if (_useEncryption)
                payload = Encrypt(payload);

            PlayerPrefs.SetString(key, payload);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load data from PlayerPrefs.
        /// </summary>
        public T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                string payload = PlayerPrefs.GetString(key);
                if (_useEncryption)
                    payload = Decrypt(payload);

                // Typed payload (new format)
                object value = TryParseTypedPayload(typeof(T), payload);
                if (value != null)
                    return (T)value;

                if (payload.StartsWith(PrefixJson, StringComparison.Ordinal))
                {
                    string json = payload.Substring(PrefixJson.Length);
                    return JsonUtility.FromJson<T>(json);
                }

                // Backward compat: treat as json string
                return JsonUtility.FromJson<T>(payload);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load key '{key}': {e.Message}");
                return defaultValue;
            }
        }

        private static object TryParseTypedPayload(Type targetType, string payload)
        {
            if (string.IsNullOrEmpty(payload)) return null;

            // For non-json prefixes
            if (payload.StartsWith(PrefixInt, StringComparison.Ordinal) && targetType == typeof(int))
                return int.Parse(payload.Substring(PrefixInt.Length), CultureInfo.InvariantCulture);
            if (payload.StartsWith(PrefixLong, StringComparison.Ordinal) && targetType == typeof(long))
                return long.Parse(payload.Substring(PrefixLong.Length), CultureInfo.InvariantCulture);
            if (payload.StartsWith(PrefixFloat, StringComparison.Ordinal) && targetType == typeof(float))
                return float.Parse(payload.Substring(PrefixFloat.Length), CultureInfo.InvariantCulture);
            if (payload.StartsWith(PrefixDouble, StringComparison.Ordinal) && targetType == typeof(double))
                return double.Parse(payload.Substring(PrefixDouble.Length), CultureInfo.InvariantCulture);
            if (payload.StartsWith(PrefixBool, StringComparison.Ordinal) && targetType == typeof(bool))
                return payload.Substring(PrefixBool.Length) == "1";
            if (payload.StartsWith(PrefixString, StringComparison.Ordinal) && targetType == typeof(string))
                return payload.Substring(PrefixString.Length);

            // Json prefix
            if (payload.StartsWith(PrefixJson, StringComparison.Ordinal))
                return null; // handled by FromJson

            return null;
        }
        
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
        
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
        
        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
        
        #endregion
        
        #region File-Based (Complex Data)
        
        /// <summary>
        /// Save data to a JSON file.
        /// </summary>
        public void SaveToFile<T>(string fileName, T data)
        {
            string path = GetFilePath(fileName);
            
            try
            {
                string json = JsonUtility.ToJson(data, true);
                
                if (_useEncryption)
                {
                    json = Encrypt(json);
                }
                
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save file '{fileName}': {e.Message}");
            }
        }
        
        /// <summary>
        /// Load data from a JSON file.
        /// </summary>
        public T LoadFromFile<T>(string fileName, T defaultValue = default)
        {
            string path = GetFilePath(fileName);
            
            if (!File.Exists(path))
            {
                return defaultValue;
            }
            
            try
            {
                string json = File.ReadAllText(path);
                
                if (_useEncryption)
                {
                    json = Decrypt(json);
                }
                
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load file '{fileName}': {e.Message}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public bool FileExists(string fileName)
        {
            return File.Exists(GetFilePath(fileName));
        }
        
        /// <summary>
        /// Delete a save file.
        /// </summary>
        public void DeleteFile(string fileName)
        {
            string path = GetFilePath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
        /// <summary>
        /// Get all save file names.
        /// </summary>
        public string[] GetAllSaveFiles()
        {
            if (!Directory.Exists(_saveDirectory))
            {
                return Array.Empty<string>();
            }
            
            var files = Directory.GetFiles(_saveDirectory, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }
        
        private string GetFilePath(string fileName)
        {
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }
            return Path.Combine(_saveDirectory, fileName);
        }
        
        #endregion
        
        #region Encryption (Simple XOR - Replace with proper encryption for production)
        
        private string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(_encryptionKey)) return text;
            
            char[] result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = (char)(text[i] ^ _encryptionKey[i % _encryptionKey.Length]);
            }
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(result)));
        }
        
        private string Decrypt(string text)
        {
            if (string.IsNullOrEmpty(_encryptionKey)) return text;
            
            try
            {
                string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(text));
                char[] result = new char[decoded.Length];
                for (int i = 0; i < decoded.Length; i++)
                {
                    result[i] = (char)(decoded[i] ^ _encryptionKey[i % _encryptionKey.Length]);
                }
                return new string(result);
            }
            catch
            {
                return text; // Return as-is if decryption fails
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Base class for saveable game data.
    /// </summary>
    [Serializable]
    public abstract class SaveData
    {
        public string version = "1.0";
        public long timestamp;
        
        protected SaveData()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
