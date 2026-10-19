namespace ArcaneArchive.Interaction
{
    /// <summary>
    /// Common contract for all world objects the player can activate.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }

        void Interact(PlayerInteractor interactor);
    }
}
