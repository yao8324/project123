using ArcaneArchive.Knowledge;
using ArcaneArchive.UI;
using UnityEngine;
namespace ArcaneArchive.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DemoBookTarget : MonoBehaviour, IInteractable
    {
        [SerializeField] private DemoBookRecord record;
        public DemoBookRecord Record => record;
        public string InteractionPrompt
        {
            get
            {
                if(!record)return "";
                var bindings=FindFirstObjectByType<ArcaneArchive.Core.LibraryInputBindings>();
                return record.title+"\n["+(bindings?bindings.Display("Interact"):"E")+"] 查看馆藏";
            }
        }
        public void Interact(PlayerInteractor interactor)
        {
            if (record) FindFirstObjectByType<RecoveryMenu>()?.ShowBook(record);
        }
    }
}
