using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ArcaneArchive.Visuals
{
    /// <summary>Enable the arena's restrained bloom only while this arena exists.</summary>
    public sealed class WardenArenaPostProcessing : MonoBehaviour
    {
        Camera view;
        UniversalAdditionalCameraData data;
        bool previousPost, previousHdr;
        void OnEnable()
        {
            view = Camera.main;
            if (!view) return;
            data = view.GetComponent<UniversalAdditionalCameraData>();
            previousHdr = view.allowHDR; view.allowHDR = true;
            if (data) { previousPost = data.renderPostProcessing; data.renderPostProcessing = true; }
        }
        void OnDisable()
        {
            if (data) data.renderPostProcessing = previousPost;
            if (view) view.allowHDR = previousHdr;
            data = null; view = null;
        }
    }
}
