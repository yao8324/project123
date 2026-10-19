using UnityEngine;

namespace ArcaneArchive.Interaction
{
    /// <summary>
    /// Temporary scene-only verification target for the shared interaction framework.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableTestObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Press E to Test Interaction";

        private bool wasActivated;

        public string InteractionPrompt => promptText;

        public void Interact(PlayerInteractor interactor)
        {
            if (wasActivated)
            {
                return;
            }

            wasActivated = true;
            Debug.Log("Interaction test succeeded.", this);
        }
    }
}
