using UnityEngine;
namespace ArcaneArchive.Knowledge
{
    [CreateAssetMenu(menuName = "Arcane Archive/UI Demo Book")]
    public sealed class DemoBookRecord : ScriptableObject
    {
        public string id, title, category, fileType, keywords;
        [TextArea(3, 8)] public string summary;
        [TextArea(8, 20)] public string previewText;
        public Color coverColor = new Color(.20f, .35f, .32f);
        [Tooltip("Only the designated hall display book opens the trial interface.")]
        public bool opensTrial;
    }
}
