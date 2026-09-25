using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcaneArchive.Interaction
{
    /// <summary>
    /// Finds and activates the interactable at the centre of the player's view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float interactionDistance = 3f;
        [SerializeField] private Camera playerCamera;

        private IInteractable currentTarget;
        private InteractionPromptUI promptUI;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            promptUI ??= InteractionPromptUI.Instance;
            currentTarget = FindTarget();

            if (currentTarget == null)
            {
                promptUI?.Hide();
                return;
            }

            promptUI?.Show(currentTarget.InteractionPrompt);

            if (Keyboard.current?.eKey.wasPressedThisFrame == true)
            {
                currentTarget.Interact(this);
            }
        }

        private IInteractable FindTarget()
        {
            if (playerCamera == null)
            {
                return null;
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
