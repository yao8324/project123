#if UNITY_EDITOR || MILAN_ACCEPTANCE_DIAGNOSTICS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MilanAcceptance
{
    // One-factor-at-a-time runtime experiments; no scene or asset saves.
    public sealed class MilanRenderProbe : MonoBehaviour
    {
        string output;
        readonly string[] phases = { "cascade1_distance25", "cascade1_distance25_msaa1", "no_sun_scale085", "no_sun_scale070", "no_sun_vsync2" };
        readonly List<string> rows = new List<string>(30000);
        UniversalRenderPipelineAsset original, pipe;
        Camera eye, overlay;
        Light[] additional;
        int phase, errors;
        float phaseStart, start;
        bool ready;
        ulong lastTimestamp;
        FrameTiming[] timing = new FrameTiming[1];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-milanRenderProbe");
            if (i < 0 || i + 1 >= args.Length) return;
            string path = Path.GetFullPath(args[i + 1]);
            if (Directory.Exists(path)) throw new IOException("Probe output already exists.");
            new GameObject("MilanRenderProbe_CommandLineOnly").AddComponent<MilanRenderProbe>().output = path;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output); AudioListener.pause = true; AudioListener.volume = 0; Application.runInBackground = true;
            yield return null;
            eye = Camera.main;
            overlay = eye.GetUniversalAdditionalCameraData().cameraStack.Single();
            additional = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x => x.type != LightType.Directional && x.enabled).ToArray();
            original = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            pipe = Instantiate(original); QualitySettings.renderPipeline = pipe;
            File.WriteAllText(Path.Combine(output, "inventory.json"), "{\"additionalLights\":" + additional.Length + ",\"renderers\":" + FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length + ",\"colliders\":" + FindObjectsByType<Collider>(FindObjectsSortMode.None).Length + ",\"overlayMask\":" + overlay.cullingMask + ",\"baseMask\":" + eye.cullingMask + ",\"overlayFar\":" + overlay.farClipPlane.ToString(CultureInfo.InvariantCulture) + ",\"width\":" + Screen.width + ",\"height\":" + Screen.height + "}");
            rows.Add("phase,time,dt,cpuFrameMS,cpuMainMS,cpuRenderMS,cpuPresentWaitMS,gpuMS,validTiming,timestamp,focused");
            Application.logMessageReceived += (m,s,t) => { if(t == LogType.Error || t == LogType.Exception) errors++; };
            start = phaseStart = Time.realtimeSinceStartup; Apply(); ready = true;
        }
        void Apply()
        {
            pipe.shadowDistance = original.shadowDistance; pipe.renderScale = original.renderScale; pipe.msaaSampleCount = original.msaaSampleCount; pipe.shadowCascadeCount = original.shadowCascadeCount;
            QualitySettings.vSyncCount = 1; Application.targetFrameRate = 60;
            RenderSettings.sun.shadows = LightShadows.Soft;
            eye.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            overlay.GetUniversalAdditionalCameraData().renderShadows = true; overlay.enabled = true;
            foreach(var light in additional) light.enabled = true;
            switch(phases[phase])
            {
                case "cascade1_distance25": pipe.shadowCascadeCount = 1; pipe.shadowDistance = 25; break;
                case "cascade1_distance25_msaa1": pipe.shadowCascadeCount = 1; pipe.shadowDistance = 25; pipe.msaaSampleCount = 1; break;
                case "no_sun_scale085": RenderSettings.sun.shadows = LightShadows.None; pipe.renderScale = .85f; break;
                case "no_sun_scale070": RenderSettings.sun.shadows = LightShadows.None; pipe.renderScale = .7f; break;
                case "no_sun_vsync2": RenderSettings.sun.shadows = LightShadows.None; QualitySettings.vSyncCount = 2; break;
                case "cascade1_vsync0": pipe.shadowCascadeCount = 1; break;
                case "cascade1_distance25_vsync0": pipe.shadowCascadeCount = 1; pipe.shadowDistance = 25; break;
                case "cascade1_distance25_scale085_vsync0": pipe.shadowCascadeCount = 1; pipe.shadowDistance = 25; pipe.renderScale = .85f; break;
                case "no_sun_vsync0":
                case "no_sun_shadows": RenderSettings.sun.shadows = LightShadows.None; break;
                case "shadow_distance25": pipe.shadowDistance = 25; break;
                case "shadow_distance12": pipe.shadowDistance = 12; break;
                case "no_post": eye.GetUniversalAdditionalCameraData().renderPostProcessing = false; break;
                case "scale_070": pipe.renderScale = .7f; break;
                case "overlay_no_shadows": overlay.GetUniversalAdditionalCameraData().renderShadows = false; break;
                case "no_additional_lights": foreach(var light in additional) light.enabled = false; break;
                case "no_overlay": overlay.enabled = false; break;
                case "msaa1": pipe.msaaSampleCount = 1; break;
            }
            Debug.Log("MILAN_PROBE_PHASE " + phases[phase]);
        }
        void LateUpdate()
        {
            if (!ready) return;
            FrameTimingManager.CaptureFrameTimings();
            uint valid = FrameTimingManager.GetLatestTimings(1, timing);
            bool fresh = valid > 0 && timing[0].frameStartTimestamp != lastTimestamp;
            if(fresh) lastTimestamp = timing[0].frameStartTimestamp;
            float now = Time.realtimeSinceStartup;
            if(now - phaseStart > 5)
                rows.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1:F5},{2:F7},{3:F5},{4:F5},{5:F5},{6:F5},{7:F5},{8},{9},{10}",phases[phase],now-start,Time.unscaledDeltaTime,timing[0].cpuFrameTime,timing[0].cpuMainThreadFrameTime,timing[0].cpuRenderThreadFrameTime,timing[0].cpuMainThreadPresentWaitTime,timing[0].gpuFrameTime,fresh,lastTimestamp,Application.isFocused));
            if(now - phaseStart >= 23)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(output,phases[phase]+".png"));
                phase++;
                if(phase == phases.Length) { ready=false; StartCoroutine(Finish()); }
                else { Apply(); phaseStart=now; }
            }
        }
        IEnumerator Finish()
        {
            yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(2);
            File.WriteAllLines(Path.Combine(output,"frames.csv"),rows);
            File.WriteAllText(Path.Combine(output,"complete.json"),"{\"complete\":true,\"errors\":"+errors+"}");
            QualitySettings.renderPipeline=original;
            Application.Quit(errors == 0 ? 0 : 1);
        }
    }
}

#endif
