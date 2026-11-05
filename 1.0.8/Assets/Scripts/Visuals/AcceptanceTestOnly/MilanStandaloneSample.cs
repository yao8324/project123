#if UNITY_EDITOR || MILAN_ACCEPTANCE_DIAGNOSTICS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MilanAcceptance
{
    // Explicit command-line opt-in; absent from the previously delivered Player.
    [DefaultExecutionOrder(32000)]
    public sealed class MilanStandaloneSample : MonoBehaviour
    {
        [Serializable] sealed class Hardware
        {
            public string unity, os, cpu, gpu, graphicsAPI, deviceModel, quality, renderPipeline;
            public int cpuCores, memoryMB, graphicsMemoryMB, width, height, vSync, targetFrameRate;
            public bool developmentBuild;
            public string method = "Non-development standalone diagnostic build; synthetic InputSystem keyboard/mouse; not OS-level or human playtest. Frame intervals include rendering and presentation. No CPU/GPU profiler attribution.";
        }
        [Serializable] sealed class Completion
        {
            public bool completed;
            public int frames, errors, exceptions;
            public float seconds;
            public string[] stages;
        }
        [Serializable] sealed class QualityState
        {
            public string selected, pipeline;
            public float renderScale;
            public bool mainLightShadows;
            public int vSync;
        }
        QualityState ReadQuality()
        {
            var pipe = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return new QualityState { selected = QualitySettings.names[QualitySettings.GetQualityLevel()], pipeline = pipe.name,
                renderScale = pipe.renderScale, mainLightShadows = pipe.supportsMainLightShadows, vSync = QualitySettings.vSyncCount };
        }
        readonly string[] labels = { "warmup", "idle_initial", "look_360", "walk_forward", "walk_back", "crouch", "jump", "sprint_forward", "sprint_back", "idle_final" };
        readonly float[] durations = { 15, 30, 30, 3, 3, 5, 5, 2, 2, 30 };
        readonly List<string> rows = new List<string>(30000);
        string output;
        FirstPersonController player;
        Camera eye;
        Keyboard keyboard;
        Mouse mouse;
        float start, stageStart, previousCapture = -100;
        int stage, errors, exceptions;
        bool ready, finished;
        Key[] keys = Array.Empty<Key>();
        Vector2 mouseDelta;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Application.isEditor) return;
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-milanStandaloneSample");
            if (i < 0 || i + 1 >= args.Length) return;
            string path = Path.GetFullPath(args[i + 1]);
            if (Directory.Exists(path)) throw new IOException("Sample output must be a new directory.");
            var go = new GameObject("MilanStandaloneSample_CommandLineOnly");
            go.AddComponent<MilanStandaloneSample>().output = path;
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            AudioListener.pause = true; AudioListener.volume = 0;
            Application.runInBackground = true;
            // Keep project quality, vSync, frame cap, resolution and camera settings intact.
            yield return null;
            player = FindFirstObjectByType<FirstPersonController>();
            if (player == null) throw new InvalidOperationException("Original controller missing.");
            eye = player.GetComponentInChildren<Camera>();
            keyboard = InputSystem.AddDevice<Keyboard>("StandaloneSampleKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("StandaloneSampleMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            if (Environment.GetCommandLineArgs().Contains("-milanQualitySwitchTest"))
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F9));
                yield return new WaitForSecondsRealtime(1);
                var ultra = ReadQuality();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "quality_ultra.png"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F8));
                yield return new WaitForSecondsRealtime(1);
                var balanced = ReadQuality();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "quality_balanced.png"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                if (ultra.selected != "Ultra" || !ultra.mainLightShadows || !Mathf.Approximately(ultra.renderScale, 1) || balanced.selected != "Balanced")
                    throw new InvalidOperationException("Quality hotkey roundtrip failed.");
                File.WriteAllText(Path.Combine(output, "quality_switch.json"), "{\"F9\":" + JsonUtility.ToJson(ultra) + ",\"F8\":" + JsonUtility.ToJson(balanced) + "}");
            }
            var h = new Hardware { unity = Application.unityVersion, os = SystemInfo.operatingSystem,
                cpu = SystemInfo.processorType, cpuCores = SystemInfo.processorCount, memoryMB = SystemInfo.systemMemorySize,
                gpu = SystemInfo.graphicsDeviceName, graphicsMemoryMB = SystemInfo.graphicsMemorySize,
                graphicsAPI = SystemInfo.graphicsDeviceType.ToString(), deviceModel = SystemInfo.deviceModel,
                width = Screen.width, height = Screen.height, vSync = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                renderPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name,
                developmentBuild = Debug.isDebugBuild };
            File.WriteAllText(Path.Combine(output, "hardware.json"), JsonUtility.ToJson(h, true));
            if (Environment.GetCommandLineArgs().Contains("-milanStartupOnly"))
            {
                yield return new WaitForSecondsRealtime(3);
                h.width=Screen.width; h.height=Screen.height;
                File.WriteAllText(Path.Combine(output, "hardware.json"), JsonUtility.ToJson(h, true));
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "default_startup.png"));
                yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(2);
                if(Screen.width != 1280 || Screen.height != 720) throw new InvalidOperationException("Default tested resolution was not applied.");
                File.WriteAllText(Path.Combine(output,"complete.json"),"{\"defaultWindow1280x720Passed\":true}");
                Application.Quit(0); yield break;
            }
            rows.Add("frame,time,stage,dt,x,y,z,yaw,pitch,eyeY,speed,grounded,crouching,sprinting,jumps,lands,cursor,commandKeys,mouseX,mouseY,excludeCaptureWindow");
            Application.logMessageReceived += OnLog;
            start = stageStart = Time.realtimeSinceStartup;
            InputSystem.onBeforeUpdate += Inject;
            ready = true;
        }
        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert) errors++;
            if (type == LogType.Exception) exceptions++;
        }
        void Inject()
        {
            if (!ready || finished || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            string s = labels[stage];
            keys = s == "walk_forward" ? new[] { Key.W } : s == "walk_back" ? new[] { Key.S } :
                s == "crouch" ? new[] { Key.C } : s == "jump" ? new[] { Key.Space } :
                s == "sprint_forward" ? new[] { Key.W, Key.LeftShift } : s == "sprint_back" ? new[] { Key.S, Key.LeftShift } : Array.Empty<Key>();
            mouseDelta = s == "look_360" ? new Vector2(150f * Time.unscaledDeltaTime, 0) : Vector2.zero;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = mouseDelta, buttons = 1 });
        }
        void LateUpdate()
        {
            if (!ready || finished) return;
            float now = Time.realtimeSinceStartup;
            Vector3 p = player.transform.position;
            string row = string.Format(CultureInfo.InvariantCulture,
                "{0},{1:F6},{2},{3:F7},{4:F6},{5:F6},{6:F6},{7:F4},{8:F4},{9:F6},{10:F4},{11},{12},{13},{14},{15},{16},{17},{18:F4},{19:F4},{20}",
                Time.frameCount, now - start, labels[stage], Time.unscaledDeltaTime, p.x, p.y, p.z,
                player.transform.eulerAngles.y, eye.transform.localEulerAngles.x, eye.transform.position.y,
                player.PlanarSpeed, player.IsGrounded, player.IsCrouching, player.IsSprinting,
                player.JumpSequence, player.LandSequence, Cursor.lockState, string.Join("+", keys), mouseDelta.x, mouseDelta.y, now - previousCapture < 2);
            rows.Add(row);
            float elapsed = now - stageStart;
            if (elapsed >= durations[stage])
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(output, labels[stage] + ".png"));
                previousCapture = now;
                stage++;
                if (stage >= labels.Length) { finished = true; StartCoroutine(Finish()); }
                else stageStart = now;
            }
        }
        IEnumerator Finish()
        {
            InputSystem.onBeforeUpdate -= Inject;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(2);
            File.WriteAllLines(Path.Combine(output, "frames.csv"), rows);
            File.WriteAllText(Path.Combine(output, "complete.json"), JsonUtility.ToJson(new Completion {
                completed = true, frames = rows.Count - 1, errors = errors, exceptions = exceptions,
                seconds = Time.realtimeSinceStartup - start, stages = labels }, true));
            Debug.Log("MILAN_STANDALONE_SAMPLE_COMPLETE " + output);
            Application.Quit(errors + exceptions == 0 ? 0 : 1);
        }
    }
}

#endif
