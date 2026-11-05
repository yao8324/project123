using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    /// <summary>Battle-clock driven presentation. No world-state, damage or ending logic.</summary>
    public sealed class KnightArenaPresentation : MonoBehaviour
    {
        public Renderer[] sealLines;
        public Light thresholdLight;
        MaterialPropertyBlock block;
        float previousClock = -1, pressure;
        AmbientMode originalMode;
        Color originalSky,originalEquator,originalGround;
        float originalIntensity;
        bool ownsAmbient;
        public Color OriginalSky=>originalSky;
        public AmbientMode OriginalMode=>originalMode;
        public float Pressure => pressure;
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            originalMode=RenderSettings.ambientMode;originalSky=RenderSettings.ambientSkyColor;originalEquator=RenderSettings.ambientEquatorColor;originalGround=RenderSettings.ambientGroundColor;originalIntensity=RenderSettings.ambientIntensity;ownsAmbient=true;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.16f,.18f,.23f);RenderSettings.ambientEquatorColor=new Color(.095f,.11f,.14f);RenderSettings.ambientGroundColor=new Color(.045f,.05f,.065f);RenderSettings.ambientIntensity=1;
        }
        void OnDisable()
        {
            if(!ownsAmbient)return;
            RenderSettings.ambientMode=originalMode;RenderSettings.ambientSkyColor=originalSky;RenderSettings.ambientEquatorColor=originalEquator;RenderSettings.ambientGroundColor=originalGround;RenderSettings.ambientIntensity=originalIntensity;ownsAmbient=false;
        }
        public void Present(int phase, float clock)
        {
            if (previousClock >= 0 && Mathf.Abs(clock - previousClock) < .000001f) return;
            if (clock < previousClock) pressure = 0;
            float dt = previousClock < 0 ? 0 : Mathf.Max(0, clock - previousClock); previousClock = clock;
            pressure = Mathf.MoveTowards(pressure, phase == 2 ? 1 : 0, dt * .7f);
            if (block == null) block = new MaterialPropertyBlock();
            Color value = Color.Lerp(new Color(.15f, .08f, .02f), new Color(.65f, .29f, .055f), pressure);
            foreach (var line in sealLines) if (line)
            {
                line.GetPropertyBlock(block); block.SetColor("_EmissionColor", value); line.SetPropertyBlock(block);
            }
            if (thresholdLight) thresholdLight.intensity = Mathf.Lerp(8, 14, pressure);
        }
    }
}
