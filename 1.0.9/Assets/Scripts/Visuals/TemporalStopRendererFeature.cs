using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArcaneArchive.Visuals
{
    // Reuse URP's supported fullscreen pass, including RenderGraph and camera-stack copies.
    // Only the world camera is graded: hands, spectral echoes and HUD keep their readability.
    public sealed class TemporalStopRendererFeature : FullScreenPassRendererFeature
    {
        public Material effectMaterial;
        Material runtimeMaterial;
        public override void Create()
        {
            injectionPoint = InjectionPoint.BeforeRenderingPostProcessing;
            fetchColorBuffer = true;
            requirements = ScriptableRenderPassInput.None;
            bindDepthStencilAttachment = false;
            base.Create();
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            if(!Application.isPlaying || cameraData.cameraType != CameraType.Game ||
                cameraData.renderType != CameraRenderType.Base || !effectMaterial)return;
            var dash = cameraData.camera.GetComponentInParent<PlayerDash>();
            if(!dash || !dash.SlowActive)return;
            if(!runtimeMaterial)
            {
                runtimeMaterial = new Material(effectMaterial);
                runtimeMaterial.name = "Temporal stop runtime grading";
                runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            runtimeMaterial.SetVector("_TemporalPhase",new Vector4(dash.RewardAge,PlayerDash.SlowHold,PlayerDash.SlowDuration,.18f));
            runtimeMaterial.SetFloat("_Aspect",cameraData.camera.aspect);
            passMaterial = runtimeMaterial;
            base.AddRenderPasses(renderer, ref renderingData);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(runtimeMaterial)CoreUtils.Destroy(runtimeMaterial);
            runtimeMaterial = null; passMaterial = null;
        }
    }
}
