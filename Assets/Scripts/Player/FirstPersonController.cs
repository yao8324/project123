using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform cameraPivot;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpSpeed = 7f;
        [SerializeField] private float gravity = -20f;

        [Header("Ground Check")]
        [SerializeField, Min(0f)] private float groundCheckOriginHeight = 0.15f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.35f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float maxPitch = 80f;

        private CharacterController characterController;
        private InputActionMap playerActions;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private float verticalVelocity;
        private float pitch;
        private bool isOnGroundSurface;
        private bool wasOnGroundSurface;
        private bool jumpConsumed;

        public bool GameplayInputEnabled { get; private set; } = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ConfigureActions();
        }

        private void OnEnable()
        {
            playerActions?.Enable();
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            playerActions?.Disable();
            SetCursorLocked(false);
        }

        private void Update()
        {
            UpdateGroundState();
            HandleCursorLock();
            ApplyGravity();

            if (!GameplayInputEnabled)
            {
                return;
            }

            UpdateLook();
            UpdateMovement();
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            GameplayInputEnabled = enabled;
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void ConfigureActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("FirstPersonController requires the project InputActionAsset.", this);
                return;
            }

            playerActions = inputActions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = playerActions.FindAction("Move", throwIfNotFound: true);
            lookAction = playerActions.FindAction("Look", throwIfNotFound: true);
            sprintAction = playerActions.FindAction("Sprint", throwIfNotFound: true);
            jumpAction = playerActions.FindAction("Jump", throwIfNotFound: true);
        }

        private void HandleCursorLock()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                SetCursorLocked(false);
                return;
            }

            if (GameplayInputEnabled && Cursor.lockState != CursorLockMode.Locked &&
                Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                SetCursorLocked(true);
            }
        }

        private void ApplyGravity()
        {
            if (isOnGroundSurface && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (GameplayInputEnabled && isOnGroundSurface && !jumpConsumed && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = jumpSpeed;
                jumpConsumed = true;
            }

            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void UpdateGroundState()
        {
            isOnGroundSurface = IsGroundSurfaceBelowPlayer();

            if (isOnGroundSurface && !wasOnGroundSurface)
            {
                jumpConsumed = false;
            }

            wasOnGroundSurface = isOnGroundSurface;
        }

        private bool IsGroundSurfaceBelowPlayer()
        {
            Vector3 origin = transform.position + Vector3.up * groundCheckOriginHeight;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.GetComponentInParent<GroundSurface>() != null;
        }

        private void UpdateLook()
        {
            if (cameraPivot == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - lookInput.y, -maxPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(Vector3.up * lookInput.x);
        }

        private void UpdateMovement()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
            float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
            characterController.Move(movement.normalized * (speed * Time.deltaTime));
        }
    }
}
