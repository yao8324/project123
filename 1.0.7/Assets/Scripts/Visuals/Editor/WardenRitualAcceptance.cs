using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Production parameters, actual frame simulation and Input System movement.</summary>
    public static class WardenRitualAcceptance
    {
        const string Output = "Docs/BossDesign/ArcaneEffects20261027";
        static BossEncounter e;
        static FirstPersonController player;
        static RecoveryMenu menu;
        static BossTestInputScope scope;
        static IEnumerator sequence;
        static Gate gate;
        static bool running, deliberatePause, orbit, move, rightMouse, previousBackground;
        static int previousFps, previousVsync, frame, peak;
        static double deadline, started;
        static string scenario, failure;
        static GameObject wall;
        static Mouse testMouse;
        static Keyboard testKeyboard;
        static readonly List<string> checks = new List<string>();
        static readonly List<float> rushTimes = new List<float>();
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class Gate { public Func<bool> ready; public float timeout; public Gate(Func<bool> condition, float seconds = 14) { ready = condition; timeout = seconds; } }
        [Serializable] class Report { public bool complete; public string scenario, failure; public string[] checks; public float[] rushSeconds; public int peakRiftSegments; }
        public static string Status => JsonUtility.ToJson(new Report { complete = !running && failure == null && checks.Count > 0, scenario = scenario, failure = failure, checks = checks.ToArray(), rushSeconds = rushTimes.ToArray(), peakRiftSegments = peak }, true);
        [MenuItem("Arcane Archive/Boss/Test ritual effects 20261027")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first");
            TrackingRiftAcceptance.Stop(); WardenFlowAcceptance.Stop(); WardenCombatV2Acceptance.Stop(); BossCombatPolishAcceptance.Stop(); BossLaserGuardAcceptance.Stop(); BossAcceptance.Stop(); Stop();
            e = Object.FindFirstObjectByType<BossEncounter>(); player = e.GetComponent<FirstPersonController>(); menu = e.GetComponent<RecoveryMenu>();
            previousBackground = Application.runInBackground; previousFps = Application.targetFrameRate; previousVsync = QualitySettings.vSyncCount;
            Application.runInBackground = true; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            scope = new BossTestInputScope(); testMouse = InputSystem.AddDevice<Mouse>("WardenRitualTestMouse");
            testKeyboard = InputSystem.AddDevice<Keyboard>("WardenRitualTestKeyboard");
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            running = true; deliberatePause = orbit = move = rightMouse = false; frame = -1; peak = 0; failure = null; checks.Clear(); rushTimes.Clear();
            started = EditorApplication.timeSinceStartup; sequence = Run(); gate = null;
            InputSystem.onBeforeUpdate += Input; EditorApplication.update += Tick;
        }
        public static void Stop()
        {
            EditorApplication.update -= Tick; InputSystem.onBeforeUpdate -= Input;
            if (testKeyboard != null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard); testKeyboard = null;
            if (testMouse != null && testMouse.added) InputSystem.RemoveDevice(testMouse); testMouse = null;
            scope?.Dispose(); scope = null;
            if (wall) Object.Destroy(wall); wall = null;
            if (running) { Application.runInBackground = previousBackground; Application.targetFrameRate = previousFps; QualitySettings.vSyncCount = previousVsync; }
            running = false;
        }
        static void Input()
        {
            if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (testKeyboard != null) InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(move ? new[] { Key.D } : Array.Empty<Key>()));
            if (testMouse != null) InputSystem.QueueStateEvent(testMouse, new MouseState { buttons = (ushort)(rightMouse ? 2 : 0) });
        }
        static void Check(bool condition, string description)
        { if (!condition) throw new InvalidOperationException(description); checks.Add("PASS: " + description); Write(); }
        static void Seed(int pattern) => typeof(BossEncounter).GetField("pattern", Private).SetValue(e, pattern);
        static void Session(int pattern, string name)
        {
            scenario = name; move = orbit = rightMouse = false;
            if (e.IsActive && !e.ReturnToLibrary()) throw new InvalidOperationException("Return between scenarios failed");
            if (!e.Begin()) throw new InvalidOperationException("Production encounter missing references");
            Seed(pattern); Write();
        }
        static bool Telegraph(int attack, float elapsed = 0) => e.CurrentState == BossEncounter.State.Telegraph && e.CurrentAttack == attack && e.AttackElapsed >= elapsed;
        static bool Recovery => e.CurrentState == BossEncounter.State.Recovery;
        static Gate Delay(float seconds)
        { double until = EditorApplication.timeSinceStartup + seconds; return new Gate(() => EditorApplication.timeSinceStartup >= until, seconds + 3); }
        static void Aim()
        {
            if (!e.IsActive || e.IsFinished || e.IsPaused) return;
            var target = orbit ? e.Arena.transform.position + Vector3.up * 1.2f : e.Arena.boss.position + Vector3.up * 2.9f;
            var rotation = Quaternion.LookRotation(target - e.View.transform.position);
            player.transform.rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            float pitch = Mathf.DeltaAngle(0, rotation.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch", Private).SetValue(player, pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot", Private).GetValue(player)).localRotation = Quaternion.Euler(pitch, 0, 0);
            player.SetCursorLocked(true);
        }
        public static void Capture(string name, Vector3? target = null)
        {
            Directory.CreateDirectory(Output + "/Captures"); var cam = e.View; var old = cam.targetTexture; var active = RenderTexture.active; var rotation = cam.transform.rotation;
            var rt = RenderTexture.GetTemporary(1440, 810, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var tex = new Texture2D(1440, 810, TextureFormat.RGB24, false);
            try { if (target.HasValue) cam.transform.LookAt(target.Value); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0); tex.Apply(); File.WriteAllBytes(Output + "/Captures/" + name + ".png", tex.EncodeToPNG()); }
            finally { cam.transform.rotation = rotation; cam.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(tex); }
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Stop(); return; }
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 300) throw new TimeoutException("Ritual acceptance timed out");
                if (e.IsPaused && !deliberatePause && !e.IsFinished) { e.SendMessage("OnApplicationFocus", true); menu.Resume(); }
                Aim();
                if (e.IsActive) peak = Mathf.Max(peak, e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>().Length);
                if (gate != null)
                {
                    if (!gate.ready()) { if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Waiting in " + scenario); return; }
                    gate = null;
                }
                if (!sequence.MoveNext()) { Stop(); scenario = "Complete"; Write(); Debug.Log("[Warden ritual acceptance] All " + checks.Count + " checks passed"); return; }
                gate = sequence.Current as Gate;
                if (gate != null) deadline = EditorApplication.timeSinceStartup + gate.timeout;
            }
            catch (Exception ex) { failure = ex.ToString(); Stop(); Write(); if (menu) menu.Open(); Debug.LogError("[Warden ritual acceptance] " + scenario + ": " + ex.Message); }
        }
        static IEnumerator Run()
        {
            Session(0, "Phase one crown and pause");
            yield return new Gate(() => Telegraph(0, 1.05f));
            Check(e.SuspendedPageCount == 6 && e.VolleyReleasedCount == 0, "First gesture produces six suspended harmless pages");
            var crown = e.VolleyCrown; var pages = crown.GetComponentsInChildren<WardenSpellMotion>();
            Check(pages.Length == 6 && pages.All(p => p.Suspended && !p.trail.enabled), "All six pages are in the non-attacking crown state");
            Check(pages.All(p => p.transform.position.y > e.Arena.model.mask.position.y), "Entire tilted crown floats above the head");
            Check(e.Combat.Health == 100, "Summoning itself does no damage"); Capture("01_crown_summon");
            float clock = e.BattleTime; var position = pages[0].transform.position; var hand = e.Arena.model.animatedRig.leftHand.position;
            deliberatePause = true; menu.Open(); yield return Delay(.35f);
            Check(e.BattleTime == clock && pages[0].transform.position == position && e.Arena.model.animatedRig.leftHand.position == hand, "Pause freezes crown, pages and summoning pose");
            deliberatePause = false; menu.Resume();
            yield return new Gate(() => e.VolleyReleasedCount >= 4);
            Check(e.VolleyReleasedCount < 6 && e.SuspendedPageCount < 6, "Second gesture releases the actual suspended objects in a burst"); Capture("02_crown_release");
            yield return new Gate(() => Recovery);
            Check(e.VolleyReleasedCount == 6 && e.SuspendedPageCount == 0, "Phase one fires exactly six pages and clears the crown");
            Check(e.MaximumEnemyHomingTurn <= e.Definition.enemyHomingBudget + .01f, "Fast pages retain the bounded weak homing budget");

            Session(0, "Phase two crown"); yield return new Gate(() => e.CanFight);
            e.DamageBoss(550, false); Seed(0);
            yield return new Gate(() => Telegraph(0, 1.05f));
            Check(e.Phase == 2 && e.SuspendedPageCount == 8, "Phase two presents eight suspended pages"); Capture("03_phase_two_crown");
            yield return new Gate(() => Recovery);
            Check(e.VolleyReleasedCount == 8, "Phase two releases all eight pages once");

            Session(0, "Fast projectile absorption at low frame rate"); Application.targetFrameRate = 20;
            yield return new Gate(() => e.CurrentState == BossEncounter.State.Attack && e.CurrentAttack == 0 && e.AttackElapsed >= .10f);
            rightMouse = true;
            yield return new Gate(() => Recovery); rightMouse = false;
            Check(e.Combat.Power > 0 && e.Combat.Health == 100, "At 20 FPS actual right-click input absorbs fast pages without skipping the absorption segment");
            Application.targetFrameRate = 60;

            Session(3, "Near instant rush collision and lingering wake");
            yield return new Gate(() => Recovery);
            rushTimes.Add(e.LastRushDuration);
            Check(e.LastRushDuration > 0 && e.LastRushDuration <= .28f, "Rush crosses its path within the blink duration plus one frame");
            Check(e.RushHitCount == 1 && Mathf.Abs(e.Combat.Health - 68) < .01f, "High-speed swept capsule hits a standing player exactly once for 32 damage");
            Check(Vector3.Distance(e.Arena.boss.position, e.transform.position) > .7f, "Rush stops before passing through the player");
            Check(e.RushWake && e.RushWake.IsLingering && e.RushWake.Distance > 3, "Wake remains in world space after entering recovery");
            var wake = e.RushWake; float fade = wake.Fade; position = wake.ghosts[1].position; clock = e.BattleTime;
            Capture("04_rush_afterimage", (wake.ghosts[0].position + wake.ghosts[2].position) * .5f + Vector3.up * 1.8f);
            deliberatePause = true; menu.Open(); yield return Delay(.35f);
            Check(wake.Fade == fade && wake.ghosts[1].position == position && e.BattleTime == clock, "Pause freezes lingering ribbons and silhouettes");
            deliberatePause = false; menu.Resume();
            yield return new Gate(() => !e.RushWake);
            Check(e.CurrentState == BossEncounter.State.Recovery, "Wake dissolves during recovery without delaying the next action");

            Session(3, "Rush sidestep"); yield return new Gate(() => Telegraph(4, .90f));
            Vector3 direction = e.RushDirection, sidestepOrigin = player.transform.position; move = true;
            yield return new Gate(() => Recovery); move = false;
            if (e.RushHitCount != 0 || e.Combat.Health != 100 || e.RushTravel <= 5)
                throw new InvalidOperationException($"Sidestep: hits={e.RushHitCount}, health={e.Combat.Health}, rushTravel={e.RushTravel:F3}, playerTravel={Vector3.Distance(sidestepOrigin, player.transform.position):F3}, inputEnabled={player.GameplayInputEnabled}, paused={e.IsPaused}");
            Check(true, "Actual sideways input evades the locked near-instant rush");
            Check(Vector3.Angle(direction, e.RushDirection) < .05f, "Committed rush does not turn toward the sidestepping player");
            Capture("08_rush_sidestepped", e.RushWake.ghosts[1].position + Vector3.up * 1.8f);

            Session(3, "Rush wall and low frame rate"); yield return new Gate(() => Telegraph(4, .90f));
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "QA_RitualRushWall";
            wall.transform.SetPositionAndRotation(e.Arena.boss.position + e.RushDirection * 3 + Vector3.up * 2, Quaternion.LookRotation(e.RushDirection)); wall.transform.localScale = new Vector3(5, 4, .25f); Physics.SyncTransforms();
            Application.targetFrameRate = 20;
            yield return new Gate(() => Recovery);
            Check(e.RushTravel < 3 && e.RushHitCount == 0 && e.Combat.Health == 100, "At 20 FPS a solid wall stops the whole swept rush");
            Check(e.RushWake && e.RushWake.Distance < 3, "Afterimage path also ends before the blocking wall");
            Object.Destroy(wall); wall = null;
            Session(3, "Rush player at low frame rate"); yield return new Gate(() => Recovery);
            rushTimes.Add(e.LastRushDuration);
            Check(e.RushHitCount == 1 && Mathf.Abs(e.Combat.Health - 68) < .01f && e.LastRushDuration <= .28f, "At 20 FPS the player collision is not skipped"); Application.targetFrameRate = 60;

            Session(2, "Wide fracture side movement"); yield return new Gate(() => Telegraph(2, .6f)); move = orbit = true;
            yield return new Gate(() => e.FractureSpawnCount >= 5);
            var rift = e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>();
            Check(Mathf.Abs(rift.transform.lossyScale.x - 1.8f) < .001f, "Phase one fracture has a 1.8 metre damage strip");
            Check(rift.Contains(rift.transform.TransformPoint(new Vector3(.49f, 0, .5f))) && !rift.Contains(rift.transform.TransformPoint(new Vector3(.66f, 0, .5f)), .24f), "Visible strip boundary and world-space player margin agree");
            Capture("05_wide_fracture", rift.StartPoint + rift.transform.forward * 2 + Vector3.up * .45f);
            yield return new Gate(() => Recovery); move = orbit = false;
            Check(e.Combat.Health == 100 && e.FracturePathLength > 7, "Continuous sideways movement avoids the wider phase one rift");

            Session(2, "Phase two wider fracture"); yield return new Gate(() => e.CanFight); e.DamageBoss(550, false); Seed(2);
            yield return new Gate(() => Telegraph(2, .6f)); move = orbit = true;
            yield return new Gate(() => e.FractureSpawnCount >= 5);
            rift = e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>();
            Check(Mathf.Abs(rift.transform.lossyScale.x - 2.2f) < .001f, "Phase two fracture grows to 2.2 metres");
            Capture("06_phase_two_fracture", rift.StartPoint + rift.transform.forward * 2 + Vector3.up * .45f);
            yield return new Gate(() => Recovery); move = orbit = false;
            Check(e.Combat.Health == 100 && peak <= 10, "Phase two remains avoidable and simultaneous rifts stay bounded");

            Session(2, "Fracture damage, pause and transition cleanup"); yield return new Gate(() => e.FractureHitCount > 0);
            Check(Mathf.Abs(e.Combat.Health - 82) < .01f, "Standing on the larger rift still deals one 18 damage impact");
            rift = e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>(); float age = rift.EruptionAge; clock = e.BattleTime;
            Capture("07_fracture_close", rift.StartPoint + Vector3.up * .4f);
            deliberatePause = true; menu.Open(); yield return Delay(.3f);
            Check(e.BattleTime == clock && rift.EruptionAge == age, "Pause freezes the widened rift including its fading embers");
            deliberatePause = false; menu.Resume(); e.DamageBoss(550, false); yield return Delay(.15f);
            Check(!e.Arena.effectRoot.GetComponentInChildren<GroundFractureVisuals>(), "Phase transition removes all pending widened strips");

            Session(2, "Wide fracture wall"); yield return new Gate(() => e.FractureSpawnCount == 1);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "QA_RitualRiftWall";
            wall.transform.SetPositionAndRotation(e.RiftHead + e.RiftDirection * 2 + Vector3.up * .6f, Quaternion.LookRotation(e.RiftDirection)); wall.transform.localScale = new Vector3(4, 1.2f, .2f); Physics.SyncTransforms();
            yield return new Gate(() => Recovery);
            Check(e.FractureBlocked && e.FracturePathLength < 4 && e.Combat.Health == 100, "Full-width rift sweep stops at a wall and protects the player"); Object.Destroy(wall); wall = null;

            Session(0, "Crown cancellation, victory, retry and return"); yield return new Gate(() => Telegraph(0, 1.05f));
            e.DamageBoss(550, false); yield return Delay(.15f);
            Check(!e.VolleyCrown && e.SuspendedPageCount == 0 && e.ActiveAttackCount == 0, "Phase break cancels every suspended projectile");
            yield return new Gate(() => e.CurrentState == BossEncounter.State.Observe); e.DamageBoss(1000, false);
            yield return new Gate(() => e.Arena.model.animatedRig.SampleTime >= 3.19f);
            Check(e.CurrentState == BossEncounter.State.Won, "Victory plays its full skeletal defeat animation");
            e.Retry(); yield return Delay(.15f);
            Check(e.Phase == 1 && e.Combat.Health == 100 && e.ActiveAttackCount == 0 && !e.RushWake, "Retry restores a clean encounter");
            Check(e.ReturnToLibrary(), "Return to the original library succeeds"); yield return Delay(.2f);
            Check(Object.FindObjectsByType<WardenVolleyVisuals>(FindObjectsSortMode.None).Length == 0 && Object.FindObjectsByType<WardenRushWake>(FindObjectsSortMode.None).Length == 0 && Object.FindObjectsByType<WardenPageImpact>(FindObjectsSortMode.None).Length == 0, "Return leaves no crown, wake or page impacts in the scene");
        }
        static void Write() { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/RuntimeAcceptance.json", Status); }
    }
}
