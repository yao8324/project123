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
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.5f;
        [SerializeField, Min(0f)] private float jumpSpeed = 7f;
        [SerializeField] private float gravity = -20f;

        [Header("Crouch")]
        [SerializeField, Min(0.1f)] private float crouchHeight = 1f;
        [SerializeField, Min(0f)] private float crouchTransitionSpeed = 8f;
        [SerializeField] private LayerMask ceilingLayers = Physics.DefaultRaycastLayers;

        [Header("Ground Check")]
        [SerializeField, Min(0f)] private float groundCheckOriginHeight = 0.15f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.35f;
        [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(1f, 89f)] private float maxPitch = 80f;

        private CharacterController characterController;
        private InputActionMap playerActions;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private float verticalVelocity;
        private float pitch;
        private float standingHeight;
        private Vector3 standingCenter;
        private Vector3 standingCameraLocalPosition;
        private bool isOnGroundSurface;
        private bool wasOnGroundSurface;
        private bool jumpConsumed;

        public bool GameplayInputEnabled { get; private set; } = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            standingHeight = characterController.height;
            standingCenter = characterController.center;

            if (cameraPivot != null)
            {
                standingCameraLocalPosition = cameraPivot.localPosition;
            }

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

            UpdateCrouch();
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
            crouchAction = playerActions.FindAction("Crouch", throwIfNotFound: true);
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
            return Physics.Raycast(
                origin,
                Vector3.down,
                groundCheckDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);
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

        private void UpdateCrouch()
        {
            bool isBelowStandingHeight = characterController.height < standingHeight - 0.01f;
            bool wantsToCrouch = crouchAction.IsPressed() ||
                                 (isBelowStandingHeight && !HasStandingClearance());
            float minimumHeight = characterController.radius * 2f;
            float targetHeight = wantsToCrouch
                ? Mathf.Clamp(crouchHeight, minimumHeight, standingHeight)
                : standingHeight;
            float heightDifference = standingHeight - targetHeight;
            float maxDelta = crouchTransitionSpeed * Time.deltaTime;

            characterController.height = Mathf.MoveTowards(
                characterController.height,
                targetHeight,
                maxDelta);
            characterController.center = Vector3.MoveTowards(
                characterController.center,
                standingCenter - Vector3.up * (heightDifference * 0.5f),
                maxDelta);

            if (cameraPivot != null)
            {
                cameraPivot.localPosition = Vector3.MoveTowards(
                    cameraPivot.localPosition,
                    standingCameraLocalPosition - Vector3.up * heightDifference,
                    maxDelta);
            }
        }

        private bool HasStandingClearance()
        {
            Vector3 scale = transform.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float heightScale = Mathf.Abs(scale.y);
            float radius = Mathf.Max(0.01f, (characterController.radius - characterController.skinWidth) * radiusScale);
            float height = Mathf.Max(standingHeight * heightScale, radius * 2f);
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 center = transform.TransformPoint(standingCenter);
            Vector3 lowerPoint = center - transform.up * halfSegment + transform.up * characterController.skinWidth;
            Vector3 upperPoint = center + transform.up * halfSegment - transform.up * characterController.skinWidth;

            Collider[] overlaps = Physics.OverlapCapsule(
                lowerPoint,
                upperPoint,
                radius,
                ceilingLayers,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (overlap != characterController && !overlap.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateMovement()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
            float speed = sprintAction.IsPressed() ? walkSpeed * sprintMultiplier : walkSpeed;
            characterController.Move(movement.normalized * (speed * Time.deltaTime));
        }
    }
}
