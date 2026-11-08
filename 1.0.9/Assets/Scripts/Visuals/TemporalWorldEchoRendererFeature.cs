using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ArcaneArchive.Visuals
{
    // Draw the reward's spectral bodies after the grey world pass, with normal scene depth.
    public sealed class TemporalWorldEchoRendererFeature : RenderObjects
    {
        public override void Create()
        {
            settings.passTag="Temporal coloured world echoes";
            settings.Event=RenderPassEvent.BeforeRenderingPostProcessing;
            settings.filterSettings.RenderQueueType=RenderQueueType.Transparent;
            settings.filterSettings.LayerMask=LayerMask.GetMask("TemporalAfterimages");
            settings.overrideMode=RenderObjectsSettings.OverrideMaterialMode.None;
            settings.overrideDepthState=false;
            base.Create();
        }
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
        {
            if(!Application.isPlaying || renderingData.cameraData.cameraType!=CameraType.Game ||
                renderingData.cameraData.renderType!=CameraRenderType.Base)return;
            var echoes=renderingData.cameraData.camera.GetComponentInParent<DodgeWorldEchoes>();
            if(!echoes || !echoes.Visible)return;
            base.AddRenderPasses(renderer,ref renderingData);
        }
    }
}
