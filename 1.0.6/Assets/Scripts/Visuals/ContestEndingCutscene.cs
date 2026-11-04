using System;
using System.Collections.Generic;
using System.Linq;
using ArcaneArchive.Contest;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ArcaneArchive.Visuals
{
    /// <summary>Timeline presentation only. Story completion and checkpoints belong to ContestStoryGame.</summary>
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class ContestEndingCutscene : MonoBehaviour
    {
        public const string ResourcePath = "Ending20261104/LegacyEnding";
        public const float Duration = 26f;
        public bool Running { get; private set; }
        public double Seconds => director ? director.time : 0;
        public Camera ShotCamera { get; private set; }
        public string Subtitle { get; private set; }
        public bool Ready => Resources.Load<TimelineAsset>(ResourcePath);

        ContestStoryGame game;
        PlayableDirector director;
        GameObject rig;
        Transform bundleParent;
        Vector3 bundlePosition, bundleScale;
        Quaternion bundleRotation, lidRotation;
        bool bundleActive, viewEnabled, timelinePaused;
        Transform lid;
        Animator lidAnimator;
        Action finished;
        Material glowMaterial;
        readonly List<Transform> motes = new List<Transform>();
        readonly Dictionary<Light, float> lamps = new Dictionary<Light, float>();
        readonly List<LineRenderer> rings = new List<LineRenderer>();

        public void Play(ContestStoryGame owner, Action onFinished)
        {
            Cancel();
            game = owner;
            var asset = Resources.Load<TimelineAsset>(ResourcePath);
            if (!asset) { Debug.LogError("[Ending] Missing Timeline: " + ResourcePath); onFinished?.Invoke(); return; }
            finished = onFinished;
            director = GetComponent<PlayableDirector>();
            director.playableAsset = asset;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.playOnAwake = false;

            rig = new GameObject("Ending — 遗赠");
            rig.transform.SetPositionAndRotation(game.transferCase.transform.position, game.transferCase.transform.rotation);
            var cameraObject = Child("EndingCamera", rig.transform);
            ShotCamera = cameraObject.AddComponent<Camera>();
            ShotCamera.CopyFrom(game.view);
            ShotCamera.fieldOfView = 46;
            ShotCamera.nearClipPlane = .04f;
            viewEnabled = game.view.enabled;
            game.view.enabled = false;
            ShotCamera.enabled = true;
            var cameraAnimator = cameraObject.AddComponent<Animator>();

            lid = game.transferCase.transform.Find("Lid");
            lidRotation = lid.localRotation;
            lidAnimator = lid.GetComponent<Animator>();
            if (!lidAnimator) lidAnimator = lid.gameObject.AddComponent<Animator>();

            var bundle = game.sealedBundle.transform;
            bundleParent = bundle.parent; bundlePosition = bundle.localPosition;
            bundleRotation = bundle.localRotation; bundleScale = bundle.localScale;
            bundleActive = bundle.gameObject.activeSelf;
            var package = Child("SingleBatchMotion", rig.transform);
            var packageAnimator = package.AddComponent<Animator>();
            // Move the existing unique package; never instantiate a second story batch.
            bundle.SetParent(package.transform, false);
            bundle.localPosition = Vector3.zero;
            bundle.localRotation = Quaternion.Euler(0, -10, 0);
            bundle.localScale = bundleScale;
            bundle.gameObject.SetActive(true);

            var glow = Child("SealLight", rig.transform);
            glow.transform.localPosition = new Vector3(0, 1.18f, -.03f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point; light.range = 2.2f; light.color = new Color(.48f, .80f, .67f);
            light.shadows = LightShadows.None;
            var glowAnimator = glow.AddComponent<Animator>();
            var audio = rig.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 0; audio.volume = .65f;
            foreach (var lamp in game.library.GetComponentsInChildren<Light>())
                if (lamp.type == LightType.Point) lamps[lamp] = lamp.intensity;

            foreach (var track in asset.GetOutputTracks())
            {
                switch (track.name)
                {
                    case "镜头": director.SetGenericBinding(track, cameraAnimator); break;
                    case "箱盖": director.SetGenericBinding(track, lidAnimator); break;
                    case "封档包": director.SetGenericBinding(track, packageAnimator); break;
                    case "封印灯光": director.SetGenericBinding(track, glowAnimator); break;
                    case "音乐与撤离脚步": director.SetGenericBinding(track, audio); break;
                }
            }
            CreateSeal();
            Running = true; timelinePaused = false;
            director.time = 0; director.Play(); director.Evaluate();
            Render();
        }

        void Update()
        {
            if (!Running || !game) return;
            if (game.Paused)
            {
                if (!timelinePaused) { director.Pause(); timelinePaused = true; }
                game.Hud.EndingCinema(false, "", "");
                game.Hud.Fade(0);
                return;
            }
            if (timelinePaused) { director.Resume(); timelinePaused = false; }
            Render();
            if (director.time >= Duration - .02) Finish();
        }

        void Render()
        {
            float t = (float)director.time;
            string id = t < 6 ? null : t < 13 ? "D022" : t < 18 ? "D023" : "D024";
            var line = id == null ? null : game.StoryContent.dialogue.Single(l => l.id == id);
            Subtitle = line?.text ?? "";
            game.Hud.EndingCinema(true, line?.speaker.Replace("玩家内心", "内心") ?? "遗赠 · 安全转移", Subtitle);
            game.Hud.Fade(t < .8f ? 1 - t / .8f : Mathf.Clamp01((t - 24.6f) / 1.4f));
            float seal = Mathf.SmoothStep(0, 1, (t - 4) / 2) * (1 - Mathf.SmoothStep(0, 1, (t - 12) / 3));
            foreach (var ring in rings)
            {
                ring.enabled = seal > .001f;
                ring.startColor = ring.endColor = new Color(.69f, .87f, .70f, seal * .8f);
            }
            for (int i = 0; i < motes.Count; i++)
            {
                float phase = Mathf.Repeat(t * .18f + i * .137f, 1);
                float angle = i * 2.39996f;
                motes[i].localPosition = new Vector3(Mathf.Cos(angle) * .27f, 1.04f + phase * .4f, Mathf.Sin(angle) * .17f);
                motes[i].gameObject.SetActive(t > 1 && t < 7 && !game.ReduceFlash);
            }
            float warmth = Mathf.SmoothStep(0, 1, (t - 8) / 9);
            foreach (var pair in lamps) if (pair.Key) pair.Key.intensity = pair.Value * Mathf.Lerp(1, 1.24f, warmth);
        }

        public void Skip()
        {
            if (Running && game && !game.Paused) Finish();
        }

        void Finish()
        {
            if (!Running) return;
            Running = false;
            // Keep the final shot behind the ending panel; cancellation restores the normal camera.
            director.time = Duration - .001; director.Evaluate(); director.Pause();
            game.Hud.EndingCinema(false, "", "");
            var done = finished; finished = null;
            done?.Invoke();
        }

        public void Cancel()
        {
            Running = false; finished = null; timelinePaused = false;
            if (director)
            {
                director.Stop();
                if (director.playableAsset)
                    foreach (var output in director.playableAsset.outputs) director.ClearGenericBinding(output.sourceObject);
                director.playableAsset = null;
            }
            if (game)
            {
                if (game.view) game.view.enabled = viewEnabled;
                if (game.sealedBundle && rig)
                {
                    var bundle = game.sealedBundle.transform;
                    bundle.SetParent(bundleParent, false); bundle.localPosition = bundlePosition;
                    bundle.localRotation = bundleRotation; bundle.localScale = bundleScale;
                    bundle.gameObject.SetActive(bundleActive);
                }
                if (game.Hud) { game.Hud.EndingCinema(false, "", ""); game.Hud.Fade(0); }
            }
            if (lid) lid.localRotation = lidRotation;
            // Reuse the runtime Animator on replays; destroying it here races with a same-frame restart.
            lid = null; lidAnimator = null;
            foreach (var pair in lamps) if (pair.Key) pair.Key.intensity = pair.Value;
            lamps.Clear(); rings.Clear(); motes.Clear();
            if (rig) { rig.SetActive(false); Destroy(rig); }
            if (glowMaterial) Destroy(glowMaterial);
            rig = null; ShotCamera = null; game = null;
        }

        void OnDestroy() => Cancel();

        void CreateSeal()
        {
            glowMaterial = new Material(Resources.Load<Material>("Ending20261104/SealGlow"));
            for (int arc = 0; arc < 3; arc++)
            {
                var line = Child("ThreeGapSeal_" + arc, rig.transform).AddComponent<LineRenderer>();
                line.sharedMaterial = glowMaterial; line.useWorldSpace = false;
                line.widthMultiplier = .008f; line.positionCount = 25;
                for (int j = 0; j < 25; j++)
                {
                    float a = (arc * 120 + 17 + j * 86f / 24) * Mathf.Deg2Rad;
                    line.SetPosition(j, new Vector3(Mathf.Cos(a) * .16f, 1.097f, Mathf.Sin(a) * .16f - .02f));
                }
                rings.Add(line);
            }
            for (int i = 0; i < 12; i++)
            {
                var mote = Child("MemoryMote_" + i, rig.transform);
                var line = mote.AddComponent<LineRenderer>(); line.sharedMaterial = glowMaterial;
                line.useWorldSpace = false; line.positionCount = 2; line.widthMultiplier = .005f;
                line.SetPosition(0, Vector3.zero); line.SetPosition(1, Vector3.up * .025f);
                line.startColor = line.endColor = new Color(.9f, .77f, .46f, .55f);
                motes.Add(mote.transform);
            }
        }

        static GameObject Child(string name, Transform parent)
        { var child = new GameObject(name); child.transform.SetParent(parent, false); return child; }
    }
}
