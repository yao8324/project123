using UnityEngine;
using UnityEngine.Rendering;
namespace ArcaneArchive.Visuals
{
    // The battle chamber owns its atmosphere only while instantiated. Library presentation is restored exactly.
    public sealed class SealedArenaAtmosphere : MonoBehaviour
    {
        public Material arenaSky;
        AmbientMode mode;FogMode previousFogMode;Color ambient,fogColor,background;float density;bool fog;Material sky;
        Camera view;CameraClearFlags flags;
        void OnEnable()
        {
            previousFogMode=RenderSettings.fogMode;mode=RenderSettings.ambientMode;ambient=RenderSettings.ambientLight;fog=RenderSettings.fog;
            fogColor=RenderSettings.fogColor;density=RenderSettings.fogDensity;sky=RenderSettings.skybox;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.33f,.32f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.13f,.15f,.18f);RenderSettings.fogDensity=.006f;
            RenderSettings.skybox=arenaSky;view=Camera.main;
            if(view){background=view.backgroundColor;flags=view.clearFlags;view.clearFlags=arenaSky?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;view.backgroundColor=new Color(.26f,.34f,.45f);}
        }
        void OnDisable()
        {
            RenderSettings.fogMode=previousFogMode;RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;
            RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=density;RenderSettings.skybox=sky;
            if(view){view.backgroundColor=background;view.clearFlags=flags;}
        }
    }
}
