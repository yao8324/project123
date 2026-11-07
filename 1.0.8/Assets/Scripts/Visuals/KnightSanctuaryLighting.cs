using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ArcaneArchive.Core;

namespace ArcaneArchive.Visuals
{
    /// <summary>Owns sanctuary-only presentation; restores scene lighting and pipeline on exit.</summary>
    public sealed class KnightSanctuaryLighting : MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        public Light gateLight;
        public float Pressure {get;private set;}
        AmbientMode oldMode;Color oldSky,oldEquator,oldGround,oldFogColor;
        float oldIntensity,oldFogDensity,oldReflection;bool oldFog,ownsState;
        RenderPipelineAsset oldGraphics,oldQuality;
        BossEncounter encounter;float previousClock=-1;
        UniversalAdditionalCameraData cameraData;bool oldPost;
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            oldMode=RenderSettings.ambientMode;oldSky=RenderSettings.ambientSkyColor;oldEquator=RenderSettings.ambientEquatorColor;oldGround=RenderSettings.ambientGroundColor;
            oldIntensity=RenderSettings.ambientIntensity;oldFog=RenderSettings.fog;oldFogColor=RenderSettings.fogColor;oldFogDensity=RenderSettings.fogDensity;oldReflection=RenderSettings.reflectionIntensity;
            oldGraphics=GraphicsSettings.defaultRenderPipeline;oldQuality=QualitySettings.renderPipeline;ownsState=true;
            if(pipeline){GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.5f,.52f,.55f);RenderSettings.ambientEquatorColor=new Color(.31f,.30f,.29f);RenderSettings.ambientGroundColor=new Color(.23f,.22f,.21f);
            RenderSettings.ambientIntensity=1;RenderSettings.fog=false;RenderSettings.reflectionIntensity=.8f;
        }
        void LateUpdate()
        {
            if(!encounter)encounter=Object.FindFirstObjectByType<BossEncounter>();
            if(!encounter||!encounter.Arena||encounter.Arena.gameObject!=gameObject)return;
            if(!cameraData && encounter.View){cameraData=encounter.View.GetUniversalAdditionalCameraData();oldPost=cameraData.renderPostProcessing;cameraData.renderPostProcessing=true;}
            Present(encounter.Phase,encounter.BattleTime);
        }
        public void Present(int phase,float clock)
        {
            if(previousClock>=0 && Mathf.Abs(clock-previousClock)<.000001f)return;
            if(clock<previousClock)Pressure=0;
            float dt=previousClock<0?0:Mathf.Max(0,clock-previousClock);previousClock=clock;
            Pressure=Mathf.MoveTowards(Pressure,phase==2?1:0,dt*.45f);
            if(gateLight)gateLight.intensity=Mathf.Lerp(16,25,Pressure);
        }
        void OnDisable()
        {
            if(!ownsState)return;
            RenderSettings.ambientMode=oldMode;RenderSettings.ambientSkyColor=oldSky;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;RenderSettings.ambientIntensity=oldIntensity;
            RenderSettings.fog=oldFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogDensity=oldFogDensity;RenderSettings.reflectionIntensity=oldReflection;
            GraphicsSettings.defaultRenderPipeline=oldGraphics;QualitySettings.renderPipeline=oldQuality;
            if(cameraData)cameraData.renderPostProcessing=oldPost;
            ownsState=false;previousClock=-1;Pressure=0;
        }
    }
}
