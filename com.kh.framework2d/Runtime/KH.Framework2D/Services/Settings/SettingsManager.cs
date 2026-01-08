using System;
using KH.Framework2D.Utils;
using UnityEngine;

namespace KH.Framework2D.Services.Settings
{
    /// <summary>
    /// Game settings manager with observable properties and persistence.
    /// </summary>
    public class SettingsManager
    {
        private const string KEY_PREFIX = "Settings_";
        
        // Audio
        public ObservableProperty<float> MasterVolume { get; } = new(1f);
        public ObservableProperty<float> BGMVolume { get; } = new(1f);
        public ObservableProperty<float> SFXVolume { get; } = new(1f);
        public ObservableProperty<bool> MuteAudio { get; } = new(false);
        
        // Display
        public ObservableProperty<int> ResolutionIndex { get; } = new(0);
        public ObservableProperty<bool> Fullscreen { get; } = new(true);
        public ObservableProperty<bool> VSync { get; } = new(true);
        public ObservableProperty<int> TargetFrameRate { get; } = new(60);
        
        // Graphics
        public ObservableProperty<int> QualityLevel { get; } = new(2);
        public ObservableProperty<bool> ScreenShake { get; } = new(true);
        public ObservableProperty<bool> ShowDamageNumbers { get; } = new(true);
        
        // Gameplay
        public ObservableProperty<string> Language { get; } = new("en");
        public ObservableProperty<float> GameSpeed { get; } = new(1f);
        public ObservableProperty<bool> AutoSave { get; } = new(true);
        
        public event Action OnSettingsChanged;
        
        public SettingsManager()
        {
            LoadAll();
            SubscribeToChanges();
        }
        
        #region Load / Save
        
        /// <summary>
        /// Load all settings from PlayerPrefs.
        /// </summary>
        public void LoadAll()
        {
            // Audio
            MasterVolume.Value = PlayerPrefs.GetFloat(KEY_PREFIX + "MasterVolume", 1f);
            BGMVolume.Value = PlayerPrefs.GetFloat(KEY_PREFIX + "BGMVolume", 1f);
            SFXVolume.Value = PlayerPrefs.GetFloat(KEY_PREFIX + "SFXVolume", 1f);
            MuteAudio.Value = PlayerPrefs.GetInt(KEY_PREFIX + "MuteAudio", 0) == 1;
            
            // Display
            ResolutionIndex.Value = PlayerPrefs.GetInt(KEY_PREFIX + "ResolutionIndex", 0);
            Fullscreen.Value = PlayerPrefs.GetInt(KEY_PREFIX + "Fullscreen", 1) == 1;
            VSync.Value = PlayerPrefs.GetInt(KEY_PREFIX + "VSync", 1) == 1;
            TargetFrameRate.Value = PlayerPrefs.GetInt(KEY_PREFIX + "TargetFrameRate", 60);
            
            // Graphics
            QualityLevel.Value = PlayerPrefs.GetInt(KEY_PREFIX + "QualityLevel", 2);
            ScreenShake.Value = PlayerPrefs.GetInt(KEY_PREFIX + "ScreenShake", 1) == 1;
            ShowDamageNumbers.Value = PlayerPrefs.GetInt(KEY_PREFIX + "ShowDamageNumbers", 1) == 1;
            
            // Gameplay
            Language.Value = PlayerPrefs.GetString(KEY_PREFIX + "Language", "en");
            GameSpeed.Value = PlayerPrefs.GetFloat(KEY_PREFIX + "GameSpeed", 1f);
            AutoSave.Value = PlayerPrefs.GetInt(KEY_PREFIX + "AutoSave", 1) == 1;
        }
        
        /// <summary>
        /// Save all settings to PlayerPrefs.
        /// </summary>
        public void SaveAll()
        {
            // Audio
            PlayerPrefs.SetFloat(KEY_PREFIX + "MasterVolume", MasterVolume.Value);
            PlayerPrefs.SetFloat(KEY_PREFIX + "BGMVolume", BGMVolume.Value);
            PlayerPrefs.SetFloat(KEY_PREFIX + "SFXVolume", SFXVolume.Value);
            PlayerPrefs.SetInt(KEY_PREFIX + "MuteAudio", MuteAudio.Value ? 1 : 0);
            
            // Display
            PlayerPrefs.SetInt(KEY_PREFIX + "ResolutionIndex", ResolutionIndex.Value);
            PlayerPrefs.SetInt(KEY_PREFIX + "Fullscreen", Fullscreen.Value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_PREFIX + "VSync", VSync.Value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_PREFIX + "TargetFrameRate", TargetFrameRate.Value);
            
            // Graphics
            PlayerPrefs.SetInt(KEY_PREFIX + "QualityLevel", QualityLevel.Value);
            PlayerPrefs.SetInt(KEY_PREFIX + "ScreenShake", ScreenShake.Value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_PREFIX + "ShowDamageNumbers", ShowDamageNumbers.Value ? 1 : 0);
            
            // Gameplay
            PlayerPrefs.SetString(KEY_PREFIX + "Language", Language.Value);
            PlayerPrefs.SetFloat(KEY_PREFIX + "GameSpeed", GameSpeed.Value);
            PlayerPrefs.SetInt(KEY_PREFIX + "AutoSave", AutoSave.Value ? 1 : 0);
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Reset all settings to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            // Audio
            MasterVolume.Value = 1f;
            BGMVolume.Value = 1f;
            SFXVolume.Value = 1f;
            MuteAudio.Value = false;
            
            // Display
            ResolutionIndex.Value = 0;
            Fullscreen.Value = true;
            VSync.Value = true;
            TargetFrameRate.Value = 60;
            
            // Graphics
            QualityLevel.Value = 2;
            ScreenShake.Value = true;
            ShowDamageNumbers.Value = true;
            
            // Gameplay
            Language.Value = "en";
            GameSpeed.Value = 1f;
            AutoSave.Value = true;
            
            SaveAll();
        }
        
        #endregion
        
        #region Apply Settings
        
        /// <summary>
        /// Apply display settings to Unity.
        /// </summary>
        public void ApplyDisplaySettings()
        {
            // Resolution
            var resolutions = Screen.resolutions;
            if (ResolutionIndex.Value >= 0 && ResolutionIndex.Value < resolutions.Length)
            {
                var res = resolutions[ResolutionIndex.Value];
                Screen.SetResolution(res.width, res.height, Fullscreen.Value);
            }
            
            // VSync
            QualitySettings.vSyncCount = VSync.Value ? 1 : 0;
            
            // Frame rate (only effective when VSync is off)
            Application.targetFrameRate = VSync.Value ? -1 : TargetFrameRate.Value;
        }
        
        /// <summary>
        /// Apply quality settings.
        /// </summary>
        public void ApplyQualitySettings()
        {
            QualitySettings.SetQualityLevel(QualityLevel.Value, true);
        }
        
        /// <summary>
        /// Apply all settings.
        /// </summary>
        public void ApplyAll()
        {
            ApplyDisplaySettings();
            ApplyQualitySettings();
        }
        
        #endregion
        
        private void SubscribeToChanges()
        {
            // Auto-save on change (optional)
            // NOTE: ObservableProperty invokes immediately by default; we disable immediate calls here
            // to avoid firing "changed" events during construction / LoadAll.
            MasterVolume.Subscribe(_ => OnSettingChanged(), false);
            BGMVolume.Subscribe(_ => OnSettingChanged(), false);
            SFXVolume.Subscribe(_ => OnSettingChanged(), false);
            MuteAudio.Subscribe(_ => OnSettingChanged(), false);
            ResolutionIndex.Subscribe(_ => OnSettingChanged(), false);
            Fullscreen.Subscribe(_ => OnSettingChanged(), false);
            VSync.Subscribe(_ => OnSettingChanged(), false);
            TargetFrameRate.Subscribe(_ => OnSettingChanged(), false);
            QualityLevel.Subscribe(_ => OnSettingChanged(), false);
            ScreenShake.Subscribe(_ => OnSettingChanged(), false);
            ShowDamageNumbers.Subscribe(_ => OnSettingChanged(), false);
            Language.Subscribe(_ => OnSettingChanged(), false);
            GameSpeed.Subscribe(_ => OnSettingChanged(), false);
            AutoSave.Subscribe(_ => OnSettingChanged(), false);
        }
        
        private void OnSettingChanged()
        {
            OnSettingsChanged?.Invoke();
        }
    }
}
