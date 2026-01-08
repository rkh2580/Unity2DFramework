using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace KH.Framework2D.Services.Save
{
    #region Encryption Interfaces
    
    /// <summary>
    /// Interface for encryption providers.
    /// Implement this to add custom encryption.
    /// </summary>
    public interface IEncryptionProvider
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
    
    /// <summary>
    /// No encryption - plain text storage.
    /// Use for development/debugging.
    /// </summary>
    public class NoEncryption : IEncryptionProvider
    {
        public string Encrypt(string plainText) => plainText;
        public string Decrypt(string cipherText) => cipherText;
    }
    
    /// <summary>
    /// Simple XOR encryption - NOT SECURE.
    /// Use only for casual obfuscation, not actual security.
    /// </summary>
    public class XorEncryption : IEncryptionProvider
    {
        private readonly string _key;
        
        public XorEncryption(string key)
        {
            _key = string.IsNullOrEmpty(key) ? "DefaultKey" : key;
        }
        
        public string Encrypt(string text)
        {
            char[] result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = (char)(text[i] ^ _key[i % _key.Length]);
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(new string(result)));
        }
        
        public string Decrypt(string text)
        {
            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(text));
                char[] result = new char[decoded.Length];
                for (int i = 0; i < decoded.Length; i++)
                {
                    result[i] = (char)(decoded[i] ^ _key[i % _key.Length]);
                }
                return new string(result);
            }
            catch
            {
                return text; // Return as-is if decryption fails
            }
        }
    }
    
    /// <summary>
    /// AES encryption - RECOMMENDED for production.
    /// Provides actual security for save data.
    /// </summary>
    public class AesEncryption : IEncryptionProvider
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        
        /// <summary>
        /// Create AES encryption with auto-generated key/IV stored in PlayerPrefs.
        /// </summary>
        public AesEncryption()
        {
            // Load or generate encryption keys
            _key = LoadOrGenerateKey("__AES_KEY__", 32); // 256-bit key
            _iv = LoadOrGenerateKey("__AES_IV__", 16);   // 128-bit IV
        }
        
        /// <summary>
        /// Create AES encryption with custom key/IV.
        /// Key should be 32 bytes, IV should be 16 bytes.
        /// </summary>
        public AesEncryption(byte[] key, byte[] iv)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256", nameof(key));
            if (iv == null || iv.Length != 16)
                throw new ArgumentException("IV must be 16 bytes", nameof(iv));
            
            _key = key;
            _iv = iv;
        }
        
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
            
            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
            
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AesEncryption] Decryption failed: {ex.Message}");
                return cipherText; // Return as-is if decryption fails
            }
        }
        
        private static byte[] LoadOrGenerateKey(string prefsKey, int length)
        {
            string stored = PlayerPrefs.GetString(prefsKey, "");
            if (!string.IsNullOrEmpty(stored))
            {
                try
                {
                    return Convert.FromBase64String(stored);
                }
                catch { }
            }
            
            // Generate new key
            byte[] key = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            
            PlayerPrefs.SetString(prefsKey, Convert.ToBase64String(key));
            PlayerPrefs.Save();
            
            return key;
        }
    }
    
    #endregion
    
    /// <summary>
    /// Save/Load service supporting PlayerPrefs and JSON file storage.
    /// 
    /// [IMPROVED] Encryption is now pluggable via IEncryptionProvider.
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
        private readonly IEncryptionProvider _encryption;
        
        /// <summary>
        /// Create SaveManager with no encryption (development mode).
        /// </summary>
        public SaveManager() : this(new NoEncryption()) { }
        
        /// <summary>
        /// Create SaveManager with custom encryption provider.
        /// </summary>
        /// <param name="encryption">Encryption provider to use</param>
        public SaveManager(IEncryptionProvider encryption)
        {
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            _encryption = encryption ?? new NoEncryption();
            
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }
        
        /// <summary>
        /// Factory method for development (no encryption).
        /// </summary>
        public static SaveManager CreateDevelopment()
        {
            return new SaveManager(new NoEncryption());
        }
        
        /// <summary>
        /// Factory method for obfuscation (XOR - not secure).
        /// </summary>
        public static SaveManager CreateObfuscated(string key)
        {
            return new SaveManager(new XorEncryption(key));
        }
        
        /// <summary>
        /// Factory method for production (AES encryption).
        /// </summary>
        public static SaveManager CreateSecure()
        {
            return new SaveManager(new AesEncryption());
        }
        
        /// <summary>
        /// Factory method for production with custom keys.
        /// </summary>
        public static SaveManager CreateSecure(byte[] key, byte[] iv)
        {
            return new SaveManager(new AesEncryption(key, iv));
        }
        
        #region PlayerPrefs (Simple Values)
        
        public void Save<T>(string key, T data)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Save key cannot be null/empty.", nameof(key));

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

            payload = _encryption.Encrypt(payload);

            PlayerPrefs.SetString(key, payload);
            PlayerPrefs.Save();
        }
        
        public T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                string payload = PlayerPrefs.GetString(key);
                payload = _encryption.Decrypt(payload);

                object value = TryParseTypedPayload(typeof(T), payload);
                if (value != null)
                    return (T)value;

                if (payload.StartsWith(PrefixJson, StringComparison.Ordinal))
                {
                    string json = payload.Substring(PrefixJson.Length);
                    return JsonUtility.FromJson<T>(json);
                }

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

            if (payload.StartsWith(PrefixJson, StringComparison.Ordinal))
                return null;

            return null;
        }
        
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public void DeleteAll() => PlayerPrefs.DeleteAll();
        
        #endregion
        
        #region File-Based (Complex Data)
        
        public void SaveToFile<T>(string fileName, T data)
        {
            string path = GetFilePath(fileName);
            
            try
            {
                string json = JsonUtility.ToJson(data, true);
                json = _encryption.Encrypt(json);
                
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save file '{fileName}': {e.Message}");
            }
        }
        
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
                json = _encryption.Decrypt(json);
                
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load file '{fileName}': {e.Message}");
                return defaultValue;
            }
        }
        
        public bool FileExists(string fileName) => File.Exists(GetFilePath(fileName));
        
        public void DeleteFile(string fileName)
        {
            string path = GetFilePath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        
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
