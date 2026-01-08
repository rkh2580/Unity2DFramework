namespace KH.Framework2D
{
    /// <summary>
    /// Framework-wide enumerations and constants.
    /// Central location for all type definitions to prevent scattering.
    /// </summary>
    public static class Define
    {
        #region Scene
        
        /// <summary>
        /// Scene types for type-safe scene loading.
        /// </summary>
        public enum Scene
        {
            Unknown,
            Title,
            Login,
            Lobby,
            Game,
            Battle,
            Loading,
            Shop,
            Inventory,
        }
        
        #endregion
        
        #region UI
        
        /// <summary>
        /// UI event types for event binding.
        /// </summary>
        public enum UIEvent
        {
            Click,
            Drag,
            BeginDrag,
            EndDrag,
            PointerEnter,
            PointerExit,
            PointerDown,
            PointerUp,
        }
        
        #endregion
        
        #region Input
        
        /// <summary>
        /// Mouse event types.
        /// </summary>
        public enum MouseEvent
        {
            Press,      // Held down
            Click,      // Press and release
            PointerDown,// Just pressed
            PointerUp,  // Just released
        }
        
        /// <summary>
        /// Input mode for context-aware input handling.
        /// Controls how InputManager interprets inputs (e.g., Escape key behavior).
        /// </summary>
        public enum InputMode
        {
            /// <summary>
            /// Normal gameplay - player controls character.
            /// Escape = Pause menu.
            /// </summary>
            Gameplay,
            
            /// <summary>
            /// UI is active (popup, menu, etc.).
            /// Escape = Cancel/Close.
            /// </summary>
            UI,
            
            /// <summary>
            /// Cutscene or cinematic playing.
            /// Most inputs disabled or limited.
            /// </summary>
            Cinematic,
            
            /// <summary>
            /// All input disabled.
            /// Used during loading, transitions, etc.
            /// </summary>
            Disabled,
        }
        
        #endregion
        
        #region Sound
        
        /// <summary>
        /// Sound channel types.
        /// </summary>
        public enum Sound
        {
            Bgm,
            Effect,
            Voice,
            UI,
            MaxCount, // For array sizing
        }
        
        #endregion
        
        #region Layer
        
        /// <summary>
        /// Physics layer constants.
        /// Must match Unity's Layer settings.
        /// </summary>
        public static class Layer
        {
            public const int Default = 0;
            public const int TransparentFX = 1;
            public const int IgnoreRaycast = 2;
            public const int Water = 4;
            public const int UI = 5;
            public const int Ground = 6;
            public const int Player = 7;
            public const int Enemy = 8;
            public const int Projectile = 9;
            public const int Item = 10;
            public const int Obstacle = 11;
            
            // Layer masks for physics queries
            public static int GroundMask => 1 << Ground;
            public static int EnemyMask => 1 << Enemy;
            public static int PlayerMask => 1 << Player;
            public static int AllCharactersMask => PlayerMask | EnemyMask;
        }
        
        #endregion
        
        #region Tag
        
        /// <summary>
        /// Tag constants.
        /// Must match Unity's Tag settings.
        /// </summary>
        public static class Tag
        {
            public const string Untagged = "Untagged";
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Item = "Item";
            public const string Ground = "Ground";
            public const string Projectile = "Projectile";
            public const string Trigger = "Trigger";
        }
        
        #endregion
        
        #region Sorting Layer
        
        /// <summary>
        /// Sorting layer names for 2D rendering order.
        /// </summary>
        public static class SortingLayer
        {
            public const string Background = "Background";
            public const string Default = "Default";
            public const string Character = "Character";
            public const string Foreground = "Foreground";
            public const string Effect = "Effect";
            public const string UI = "UI";
        }
        
        #endregion
        
        #region Resource Paths
        
        /// <summary>
        /// Resource path constants.
        /// </summary>
        public static class Path
        {
            public const string Prefabs = "Prefabs";
            public const string UI = "Prefabs/UI";
            public const string Popup = "Prefabs/UI/Popup";
            public const string SceneUI = "Prefabs/UI/Scene";
            public const string Effects = "Prefabs/Effects";
            public const string Units = "Prefabs/Units";
            public const string Data = "Data";
        }
        
        #endregion
    }
}
