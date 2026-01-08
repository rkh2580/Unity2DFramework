namespace KH.Framework2D
{
    /// <summary>
    /// 프레임워크 전역 열거형 및 상수 정의.
    /// 모든 공통 타입을 중앙에서 관리하여 일관성 유지.
    /// </summary>
    public static class Define
    {
        #region Scene

        /// <summary>
        /// 씬 타입 열거형. 프로젝트에 맞게 확장 가능.
        /// </summary>
        public enum Scene
        {
            Unknown,
            Title,
            Login,
            Lobby,
            Game,
            Loading,
        }

        #endregion

        #region UI

        /// <summary>
        /// UI 이벤트 타입.
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
        /// 마우스 이벤트 타입.
        /// </summary>
        public enum MouseEvent
        {
            Press,       // 누르고 있는 중
            Click,       // 클릭 (눌렀다 뗌)
            PointerDown, // 누르는 순간
            PointerUp,   // 떼는 순간
        }

        /// <summary>
        /// 입력 모드.
        /// </summary>
        public enum InputMode
        {
            Gameplay,
            UI,
            Cinematic,
            Disabled,
        }

        #endregion

        #region Sound

        /// <summary>
        /// 사운드 타입.
        /// </summary>
        public enum Sound
        {
            Bgm,
            Effect,
            Voice,
            UI,
            MaxCount, // 개수 확인용
        }

        #endregion

        #region Layer (프로젝트에 맞게 수정)

        /// <summary>
        /// 레이어 상수. Unity Layer 설정과 일치해야 함.
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
            public const int Interactable = 10;
            public const int Item = 11;

            // LayerMask 헬퍼
            public static int GroundMask => 1 << Ground;
            public static int EnemyMask => 1 << Enemy;
            public static int PlayerMask => 1 << Player;
            public static int ProjectileMask => 1 << Projectile;
            public static int InteractableMask => 1 << Interactable;
            public static int ItemMask => 1 << Item;
            
            // 복합 마스크
            public static int AllEnemiesMask => EnemyMask;
            public static int AllCharactersMask => PlayerMask | EnemyMask;
        }

        #endregion

        #region Tag (프로젝트에 맞게 수정)

        /// <summary>
        /// 태그 상수. Unity Tag 설정과 일치해야 함.
        /// </summary>
        public static class Tag
        {
            public const string Untagged = "Untagged";
            public const string Respawn = "Respawn";
            public const string Finish = "Finish";
            public const string EditorOnly = "EditorOnly";
            public const string MainCamera = "MainCamera";
            public const string Player = "Player";
            public const string GameController = "GameController";
            public const string Enemy = "Enemy";
            public const string Item = "Item";
            public const string Ground = "Ground";
            public const string Interactable = "Interactable";
        }

        #endregion

        #region Sorting Layer

        /// <summary>
        /// Sorting Layer 이름 상수.
        /// </summary>
        public static class SortingLayer
        {
            public const string Background = "Background";
            public const string Default = "Default";
            public const string Midground = "Midground";
            public const string Foreground = "Foreground";
            public const string UI = "UI";
            public const string Overlay = "Overlay";
        }

        #endregion

        #region Animation

        /// <summary>
        /// 애니메이션 파라미터 이름 상수.
        /// </summary>
        public static class AnimParam
        {
            // Bool
            public const string IsMoving = "IsMoving";
            public const string IsGrounded = "IsGrounded";
            public const string IsAttacking = "IsAttacking";
            public const string IsDead = "IsDead";
            
            // Trigger
            public const string Attack = "Attack";
            public const string Jump = "Jump";
            public const string Hit = "Hit";
            public const string Die = "Die";
            
            // Float
            public const string Speed = "Speed";
            public const string VelocityX = "VelocityX";
            public const string VelocityY = "VelocityY";
            
            // Int
            public const string AttackCombo = "AttackCombo";
        }

        #endregion

        #region Resource Paths

        /// <summary>
        /// Resources 폴더 경로 상수.
        /// </summary>
        public static class Path
        {
            public const string Prefabs = "Prefabs";
            public const string UI = "Prefabs/UI";
            public const string UIPopup = "Prefabs/UI/Popup";
            public const string UIScene = "Prefabs/UI/Scene";
            public const string Effects = "Prefabs/Effects";
            public const string Characters = "Prefabs/Characters";
            public const string Audio = "Audio";
            public const string BGM = "Audio/BGM";
            public const string SFX = "Audio/SFX";
        }

        #endregion
    }
}
