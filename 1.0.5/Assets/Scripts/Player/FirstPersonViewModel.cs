using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Player
{
    // Presentation only: never reads input, drives gameplay, or moves the player.
    public sealed class FirstPersonViewModel : MonoBehaviour
    {
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Camera presentationCamera;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float wallProbeDistance = 0.50f;
        [SerializeField, Min(0f)] private float maximumRetraction = 0.06f;
        [SerializeField] private LayerMask obstructionLayers = Physics.DefaultRaycastLayers;
        private Vector3 restingPosition;
        private float retraction;
        private float accentUntil;
        private string currentState;
        private uint observedJump, observedLand;

        public void Configure(FirstPersonController controller, Camera camera, Animator modelAnimator, Camera overlayCamera = null)
        {
            movement = controller;
            viewCamera = camera;
            presentationCamera = overlayCamera;
            animator = modelAnimator;
        }

        private void OnEnable()
        {
            restingPosition = transform.localPosition;
            currentState = null;
            accentUntil = 0f;
            if (movement != null)
            {
                observedJump = movement.JumpSequence;
                observedLand = movement.LandSequence;
            }
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.speed = 1f;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void OnDisable()
        {
            transform.localPosition = restingPosition;
            retraction = 0f;
        }

        private void LateUpdate()
        {
            if (movement == null || viewCamera == null || animator == null) return;
            if (!movement.GameplayInputEnabled)
            {
                animator.speed = 0f;
                observedJump = movement.JumpSequence;
                observedLand = movement.LandSequence;
                accentUntil = 0f;
                return;
            }
            animator.speed = 1f;
            if (presentationCamera != null)
            { presentationCamera.fieldOfView = viewCamera.fieldOfView; presentationCamera.aspect = viewCamera.aspect; }
            if (observedJump != movement.JumpSequence)
            {
                observedJump = movement.JumpSequence;
                Play("Jump");
                accentUntil = Time.time + 0.28f;
            }
            else if (observedLand != movement.LandSequence)
            {
                observedLand = movement.LandSequence;
                Play("Land");
                accentUntil = Time.time + 0.24f;
            }
            else if (Time.time >= accentUntil)
            {
                string state = !movement.IsGrounded ? "Jump" : movement.IsCrouching ? "Crouch" :
                    movement.IsSprinting ? "Sprint" : movement.IsMoving ? "Walk" : "Idle";
                Play(state);
            }
            RetractNearWall();
        }

        public void ResetPresentation()
        {
            transform.localPosition=restingPosition;retraction=0f;accentUntil=0f;currentState=null;
            if(movement){observedJump=movement.JumpSequence;observedLand=movement.LandSequence;}
            if(animator){animator.speed=1f;animator.Play("Base Layer.Idle",0,0f);animator.Update(0f);}
        }

        private void Play(string state)
        {
            if (currentState == state) return;
            int stateHash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, stateHash)) return;
            animator.CrossFadeInFixedTime(stateHash, 0.12f, 0);
            currentState = state;
        }

        private void RetractNearWall()
        {
            // A conservative visual aid, not a replacement for a dedicated viewmodel renderer.
            // Inspect all hits so the player's own CharacterController cannot hide a wall hit.
            float closest = wallProbeDistance;
            RaycastHit[] hits = Physics.SphereCastAll(viewCamera.transform.position, 0.08f,
                viewCamera.transform.forward, wallProbeDistance, obstructionLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform.IsChildOf(movement.transform)) closest = Mathf.Min(closest, hit.distance);
            }
            float desired = maximumRetraction * (1f - Mathf.Clamp01(closest / Mathf.Max(0.01f, wallProbeDistance)));
            retraction = Mathf.Lerp(retraction, desired, 1f - Mathf.Exp(-12f * Time.deltaTime));
            transform.localPosition = restingPosition + new Vector3(0f, -retraction * 0.3f, -retraction);
        }
    }
}
