# Changelog

All notable changes to KH Framework 2D will be documented in this file.

## [2.1.1] - 2025-01-09

### Fixed
- **InputManager**: `SetInputMode(Gameplay)` now properly calls `EnableInput()` when transitioning from `Disabled` mode
- **PoolManager**: `IsWarmedUp` is now set to `true` immediately when `_warmUpOnStart` is disabled, preventing infinite wait

### Added
- **Define.cs**: Centralized enum definitions for `InputMode`, `Scene`, `Layer`, `Tag`, etc.
- **XmlDataParser.cs**: Generic XML parser with type-safe property mapping
- **SampleDataClasses.cs**: Sample data classes (`CardData`, `UnitData`, `SkillData`, etc.)
- **ExcelToXmlConverter.cs**: Editor tool for Excel/CSV to XML conversion
- **PoolManager.WarmUpSucceeded**: New property to distinguish between successful warmup and skipped/failed warmup

### Changed
- **InputManager**: Added debug logging for mode transitions
- **PoolManager**: Improved error handling with separate `WarmUpSucceeded` flag

## [2.1.0] - 2025-01-09

### Fixed
- **InputManager**: Escape key no longer triggers both `Pause` and `Cancel` events simultaneously
- **PoolManager**: Changed `async void Start()` to `UniTaskVoid` for proper exception handling
- **DataService**: Removed runtime reflection, now uses typed loaders

### Added
- **ServiceLocator**: Strict mode with usage warnings for MonoBehaviour callers
- **ServiceLocator**: Registration source tracking for debugging
- **SaveManager**: `IEncryptionProvider` interface for pluggable encryption
- **SaveManager**: `AesEncryption` class with AES-256 support
- **SaveManager**: Factory methods (`CreateDevelopment`, `CreateObfuscated`, `CreateSecure`)
- **InputManager**: `InputMode` enum for context-aware input handling
- **InputManager**: `Clear()` method for event cleanup on scene change
- **DataService**: Fluent API with `WithData<T>()` extension method

### Changed
- **SaveManager**: Constructor now requires `IEncryptionProvider` instead of simple flags

## [2.0.0] - 2025-01-08

### Added
- Initial release of refactored framework
- VContainer DI integration
- UniTask async/await support
- MVP pattern implementation
- ScriptableObject EventChannel system
- Object pooling system
- Input management with New Input System support
- Save/Load system with encryption
- Data loading from XML

---

## Migration Guide

### From 2.1.0 to 2.1.1

No breaking changes. Simply replace the files.

### From 2.0.0 to 2.1.0

1. **SaveManager**: Update constructor calls
   ```csharp
   // Before
   new SaveManager(useEncryption: true, encryptionKey: "key");
   
   // After
   SaveManager.CreateSecure(); // or CreateObfuscated("key")
   ```

2. **InputManager**: Add mode management to UI system
   ```csharp
   // When opening UI
   inputManager.SetInputMode(Define.InputMode.UI);
   
   // When closing UI
   inputManager.SetInputMode(Define.InputMode.Gameplay);
   ```
