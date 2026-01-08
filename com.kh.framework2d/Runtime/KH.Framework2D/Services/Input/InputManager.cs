using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KH.Framework2D.Services.Input
{
    /// <summary>
    /// Input management service supporting both legacy and new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour, IInputService
    {
        [Header("Settings")]
        [SerializeField] private bool _useNewInputSystem = true;
        
        // Movement
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        
        // Actions
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool SkillPressed { get; private set; }
        public bool UltimatePressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DashPressed { get; private set; }
        
        // UI
        public bool PausePressed { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool CancelPressed { get; private set; }
        public bool ConfirmPressed { get; private set; }
        
        // Mouse
        public Vector2 MousePosition { get; private set; }
        public Vector2 MouseDelta { get; private set; }
        public bool MouseLeftPressed { get; private set; }
        public bool MouseRightPressed { get; private set; }
        
        // State
        public bool InputEnabled { get; private set; } = true;
        
        // Events
        public event Action OnAttack;
        public event Action OnSkill;
        public event Action OnUltimate;
        public event Action OnInteract;
        public event Action OnJump;
        public event Action OnDash;
        public event Action OnPause;
        public event Action OnInventory;
        public event Action OnCancel;
        public event Action OnConfirm;
        
        // New Input System references (assign in inspector)
        [Header("Input Actions (New Input System)")]
        [SerializeField] private InputActionAsset _inputActions;
        
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _attackAction;
        private InputAction _skillAction;
        private InputAction _ultimateAction;
        private InputAction _interactAction;
        private InputAction _jumpAction;
        private InputAction _dashAction;
        private InputAction _pauseAction;
        private InputAction _inventoryAction;
        private InputAction _cancelAction;
        private InputAction _confirmAction;

        private bool _subscribed;
        
        private void Awake()
        {
            if (_useNewInputSystem && _inputActions != null)
            {
                SetupNewInputSystem();
            }
        }
        
        private void OnEnable()
        {
            EnableInput();
        }
        
        private void OnDisable()
        {
            DisableInput();
        }

        private void OnDestroy()
        {
            // Prevent callback leaks if this manager is destroyed/recreated.
            UnsubscribeFromActions();
        }
        
        private void Update()
        {
            if (!InputEnabled) return;
            
            if (_useNewInputSystem)
            {
                UpdateNewInputSystem();
            }
            else
            {
                UpdateLegacyInput();
            }
            
            // Mouse (works with both systems)
            MousePosition = UnityEngine.Input.mousePosition;
        }
        
        private void LateUpdate()
        {
            // Reset single-frame inputs
            ResetFrameInputs();
        }
        
        #region New Input System
        
        private void SetupNewInputSystem()
        {
            var gameplayMap = _inputActions.FindActionMap("Gameplay");
            var uiMap = _inputActions.FindActionMap("UI");
            
            if (gameplayMap != null)
            {
                _moveAction = gameplayMap.FindAction("Move");
                _lookAction = gameplayMap.FindAction("Look");
                _attackAction = gameplayMap.FindAction("Attack");
                _skillAction = gameplayMap.FindAction("Skill");
                _ultimateAction = gameplayMap.FindAction("Ultimate");
                _interactAction = gameplayMap.FindAction("Interact");
                _jumpAction = gameplayMap.FindAction("Jump");
                _dashAction = gameplayMap.FindAction("Dash");
            }
            
            if (uiMap != null)
            {
                _pauseAction = uiMap.FindAction("Pause");
                _inventoryAction = uiMap.FindAction("Inventory");
                _cancelAction = uiMap.FindAction("Cancel");
                _confirmAction = uiMap.FindAction("Confirm");
            }
            
            // Subscribe to performed events
            SubscribeToActions();
        }
        
        private void SubscribeToActions()
        {
            if (_subscribed) return;

            _attackAction?.performed += OnAttackPerformed;
            _skillAction?.performed += OnSkillPerformed;
            _ultimateAction?.performed += OnUltimatePerformed;
            _interactAction?.performed += OnInteractPerformed;
            _jumpAction?.performed += OnJumpPerformed;
            _dashAction?.performed += OnDashPerformed;
            _pauseAction?.performed += OnPausePerformed;
            _inventoryAction?.performed += OnInventoryPerformed;
            _cancelAction?.performed += OnCancelPerformed;
            _confirmAction?.performed += OnConfirmPerformed;

            _subscribed = true;
        }

        private void UnsubscribeFromActions()
        {
            if (!_subscribed) return;

            _attackAction?.performed -= OnAttackPerformed;
            _skillAction?.performed -= OnSkillPerformed;
            _ultimateAction?.performed -= OnUltimatePerformed;
            _interactAction?.performed -= OnInteractPerformed;
            _jumpAction?.performed -= OnJumpPerformed;
            _dashAction?.performed -= OnDashPerformed;
            _pauseAction?.performed -= OnPausePerformed;
            _inventoryAction?.performed -= OnInventoryPerformed;
            _cancelAction?.performed -= OnCancelPerformed;
            _confirmAction?.performed -= OnConfirmPerformed;

            _subscribed = false;
        }

        private void OnAttackPerformed(InputAction.CallbackContext _) { AttackPressed = true; OnAttack?.Invoke(); }
        private void OnSkillPerformed(InputAction.CallbackContext _) { SkillPressed = true; OnSkill?.Invoke(); }
        private void OnUltimatePerformed(InputAction.CallbackContext _) { UltimatePressed = true; OnUltimate?.Invoke(); }
        private void OnInteractPerformed(InputAction.CallbackContext _) { InteractPressed = true; OnInteract?.Invoke(); }
        private void OnJumpPerformed(InputAction.CallbackContext _) { JumpPressed = true; OnJump?.Invoke(); }
        private void OnDashPerformed(InputAction.CallbackContext _) { DashPressed = true; OnDash?.Invoke(); }
        private void OnPausePerformed(InputAction.CallbackContext _) { PausePressed = true; OnPause?.Invoke(); }
        private void OnInventoryPerformed(InputAction.CallbackContext _) { InventoryPressed = true; OnInventory?.Invoke(); }
        private void OnCancelPerformed(InputAction.CallbackContext _) { CancelPressed = true; OnCancel?.Invoke(); }
        private void OnConfirmPerformed(InputAction.CallbackContext _) { ConfirmPressed = true; OnConfirm?.Invoke(); }
        
        private void UpdateNewInputSystem()
        {
            if (_moveAction != null)
                MoveInput = _moveAction.ReadValue<Vector2>();
            
            if (_lookAction != null)
                LookInput = _lookAction.ReadValue<Vector2>();
            
            if (_attackAction != null)
                AttackHeld = _attackAction.IsPressed();
        }
        
        #endregion
        
        #region Legacy Input
        
        private void UpdateLegacyInput()
        {
            // Movement
            MoveInput = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            );
            
            // Mouse look
            MouseDelta = new Vector2(
                UnityEngine.Input.GetAxis("Mouse X"),
                UnityEngine.Input.GetAxis("Mouse Y")
            );
            
            // Attack
            if (UnityEngine.Input.GetButtonDown("Fire1") || UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                AttackPressed = true;
                OnAttack?.Invoke();
            }
            AttackHeld = UnityEngine.Input.GetButton("Fire1") || UnityEngine.Input.GetKey(KeyCode.J);
            
            // Skill
            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
            {
                SkillPressed = true;
                OnSkill?.Invoke();
            }
            
            // Ultimate
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                UltimatePressed = true;
                OnUltimate?.Invoke();
            }
            
            // Interact
            if (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                InteractPressed = true;
                OnInteract?.Invoke();
            }
            
            // Jump
            if (UnityEngine.Input.GetButtonDown("Jump"))
            {
                JumpPressed = true;
                OnJump?.Invoke();
            }
            
            // Dash
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift))
            {
                DashPressed = true;
                OnDash?.Invoke();
            }
            
            // Pause
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                PausePressed = true;
                OnPause?.Invoke();
            }
            
            // Inventory
            if (UnityEngine.Input.GetKeyDown(KeyCode.I) || UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                InventoryPressed = true;
                OnInventory?.Invoke();
            }
            
            // Cancel/Confirm
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPressed = true;
                OnCancel?.Invoke();
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmPressed = true;
                OnConfirm?.Invoke();
            }
            
            // Mouse buttons
            MouseLeftPressed = UnityEngine.Input.GetMouseButtonDown(0);
            MouseRightPressed = UnityEngine.Input.GetMouseButtonDown(1);
        }
        
        #endregion
        
        #region Control
        
        /// <summary>
        /// Enable all input.
        /// </summary>
        public void EnableInput()
        {
            InputEnabled = true;
            _inputActions?.Enable();
        }
        
        /// <summary>
        /// Disable all input.
        /// </summary>
        public void DisableInput()
        {
            InputEnabled = false;
            _inputActions?.Disable();
            ResetAllInputs();
        }
        
        /// <summary>
        /// Switch to UI input mode.
        /// </summary>
        public void SwitchToUI()
        {
            _inputActions?.FindActionMap("Gameplay")?.Disable();
            _inputActions?.FindActionMap("UI")?.Enable();
        }
        
        /// <summary>
        /// Switch to gameplay input mode.
        /// </summary>
        public void SwitchToGameplay()
        {
            _inputActions?.FindActionMap("UI")?.Disable();
            _inputActions?.FindActionMap("Gameplay")?.Enable();
        }
        
        private void ResetFrameInputs()
        {
            AttackPressed = false;
            SkillPressed = false;
            UltimatePressed = false;
            InteractPressed = false;
            JumpPressed = false;
            DashPressed = false;
            PausePressed = false;
            InventoryPressed = false;
            CancelPressed = false;
            ConfirmPressed = false;
            MouseLeftPressed = false;
            MouseRightPressed = false;
        }
        
        private void ResetAllInputs()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            AttackHeld = false;
            ResetFrameInputs();
        }
        
        #endregion
        
        #region Utility
        
        /// <summary>
        /// Get mouse position in world space (2D).
        /// </summary>
        public Vector2 GetMouseWorldPosition2D()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera.ScreenToWorldPoint(MousePosition);
            }
            return Vector2.zero;
        }
        
        /// <summary>
        /// Get mouse position in world space (3D).
        /// </summary>
        public Vector3 GetMouseWorldPosition3D(float zDepth = 10f)
        {
            var camera = Camera.main;
            if (camera != null)
            {
                Vector3 mousePos = MousePosition;
                mousePos.z = zDepth;
                return camera.ScreenToWorldPoint(mousePos);
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// Get direction from player to mouse (2D).
        /// </summary>
        public Vector2 GetDirectionToMouse(Vector2 fromPosition)
        {
            return (GetMouseWorldPosition2D() - fromPosition).normalized;
        }
        
        #endregion
    }
}
