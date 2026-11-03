using UnityEngine;

namespace IMMonster.SceneArt
{
    // Presentation only. The program-owned story system decides when to call these methods.
    public sealed class ArchiveWorkstationArt : MonoBehaviour
    {
        public GameObject intactProps;
        public GameObject disturbedProps;
        public GameObject[] looseSeals;
        public GameObject[] tiedSeals;
        [SerializeField] bool disturbed;
        [SerializeField] bool archiveSealed;
        public bool IsDisturbed => disturbed;
        public bool IsArchiveSealed => archiveSealed;

        public void SetDisturbed(bool value) { disturbed = value; Apply(); }
        public void SetArchiveSealed(bool value) { archiveSealed = value; Apply(); }
        public void ResetPresentation() { disturbed = false; archiveSealed = false; Apply(); }
        void OnEnable() { Apply(); }
        void Apply()
        {
            if (intactProps) intactProps.SetActive(!disturbed);
            if (disturbedProps) disturbedProps.SetActive(disturbed);
            SetGroup(looseSeals, !archiveSealed);
            SetGroup(tiedSeals, archiveSealed);
        }
        static void SetGroup(GameObject[] items, bool active)
        {
            if (items == null) return;
            foreach (var item in items) if (item) item.SetActive(active);
        }
    }
}
