using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Live encounter presentation regression. Seeds attack order, not timings or damage.</summary>
    public static class WardenCombatV2Acceptance
    {
        const string Output = "Docs/BossDesign/CombatV2";
        static BossEncounter e;
        static RecoveryMenu menu;
        static FirstPersonController player;
        static BossTestInputScope input;
        static bool running, background, oldPost;
        static int step, lastFrame;
        static float since, sampledTime, battleTime;
        static Vector3 handPosition;
        static Quaternion sealRotation;
        static double start;
        static string failure;
        static readonly List<string> checks = new List<string>();
        [Serializable] class Report { public int step; public string failure; public string[] checks; }
        public static string Status => JsonUtility.ToJson(new Report { step = step, failure = failure, checks = checks.ToArray() }, true);
        public static string Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first");
            BossCombatPolishAcceptance.Stop(); BossLaserGuardAcceptance.Stop(); BossAcceptance.Stop(); Stop();
            e = UnityEngine.Object.FindFirstObjectByType<BossEncounter>(); player = e.GetComponent<FirstPersonController>(); menu = e.GetComponent<RecoveryMenu>();
            e.enabled = true; background = Application.runInBackground; Application.runInBackground = true;
            oldPost = e.View.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
            input = new BossTestInputScope(); EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            checks.Clear(); failure = null; running = true; start = EditorApplication.timeSinceStartup; lastFrame = -1;
            Session(1); Step(0); EditorApplication.update += Tick; return Status;
        }
        public static void Stop()
        {
            EditorApplication.update -= Tick; input?.Dispose(); input = null;
            Keys();
            if (running) Application.runInBackground = background; running = false;
        }
        static void Check(bool condition, string text)
        { if (!condition) throw new InvalidOperationException(text); checks.Add("PASS: " + text); }
        static void Step(int next) { step = next; since = Time.time; }
        static void Keys(params Key[] keys) { if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys)); }
        static void Session(int pattern)
        {
            if (e.IsActive) Check(e.ReturnToLibrary(), "Return between presentation cases");
            Check(e.Begin(), "Enter rigged encounter");
            typeof(BossEncounter).GetField("pattern", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(e, pattern);
        }
        static void Aim()
        {
            var target = e.Arena.boss.position + Vector3.up * 2.5f;
            var look = Quaternion.LookRotation(target - e.View.transform.position);
            player.transform.rotation = Quaternion.Euler(0, look.eulerAngles.y, 0);
            float pitch = Mathf.DeltaAngle(0, look.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory(Output + "/Captures");
            // Capture the actual gameplay camera at a known resolution, even when the editor is in the background.
            var camera = e.View; var old = camera.targetTexture; var active = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(1440, 810, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(1440, 810, TextureFormat.RGB24, false);
            Quaternion rotation = camera.transform.rotation;
            try
            {
                if(name.Contains("fracture")) camera.transform.LookAt(e.transform.position + e.transform.forward*4 + Vector3.up*.15f);
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1440, 810), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/Captures/" + name + ".png", image.EncodeToPNG());
            }
            finally { camera.transform.rotation=rotation; camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }
        static bool Is(BossEncounter.State state, int attack, float elapsed)
        { return e.CurrentState == state && e.CurrentAttack == attack && e.AttackElapsed > elapsed; }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Stop(); return; }
            if (lastFrame == Time.frameCount) return; lastFrame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup - start > 180) throw new TimeoutException("Presentation check timed out at " + step);
                if (e.IsPaused && step != 1 && !e.IsFinished) { e.SendMessage("OnApplicationFocus", true); menu.Resume(); }
                if (e.IsActive) Aim();
                var d = e.Arena ? e.Arena.model.animatedRig : null;
                float t = Time.time - since;
                switch (step)
                {
                    case 0:
                        if (Is(BossEncounter.State.Telegraph, 3, e.Definition.laserChargeTime-.15f))
                        {
                            Check(d.clips.Length == 17 && !d.animator.applyRootMotion, "17 imported clips, root motion disabled");
                            Check(d.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 12, "12 skinned material groups are present");
                            Check(d.GetComponentsInChildren<Renderer>().All(r => r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported && m.shader.name.StartsWith("Universal Render Pipeline/"))), "All active Boss materials use supported URP shaders");
                            Check(d.leftHand.position.y > e.Arena.model.mask.position.y && d.rightHand.position.y > e.Arena.model.mask.position.y, "Laser pose lifts both hands above head");
                            Check(d.CurrentClip == "AA_LaserCharge", "Laser windup uses imported charge clip");
                            var gate=e.Arena.effectRoot.GetComponentInChildren<LaserBeamVisuals>().ornateGate;
                            var bounds=gate.GetComponentInChildren<Renderer>().bounds;
                            Check(bounds.size.magnitude>1 && bounds.size.magnitude<4,"Imported seal retains metre-scale geometry after animation");
                            Check(e.View.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing, "Arena enables restrained bloom");
                            Capture("01_laser_charge"); menu.Open(); sampledTime = d.SampleTime; handPosition = d.leftHand.position; battleTime = e.BattleTime;
                            sealRotation = e.Arena.effectRoot.GetComponentInChildren<LaserBeamVisuals>().ornateGate.rotation; Step(1);
                        } break;
                    case 1:
                        if (t > .6f)
                        {
                            Check(d.SampleTime == sampledTime && e.BattleTime == battleTime && Vector3.Distance(handPosition, d.leftHand.position) < .0001f, "Pause freezes the skeleton and battle clock");
                            Check(Quaternion.Angle(sealRotation, e.Arena.effectRoot.GetComponentInChildren<LaserBeamVisuals>().ornateGate.rotation) < .001f, "Pause freezes ornate laser seal");
                            menu.Resume(); Step(2);
                        } break;
                    case 2:
                        Keys(Key.D,Key.LeftShift);
                        if (Is(BossEncounter.State.Attack, 3, .28f))
                        {
                            Keys();
                            var laser = e.Arena.effectRoot.GetComponentInChildren<LaserBeamVisuals>();
                            Check(d.CurrentClip == "AA_LaserChannel" && laser.goldHelix.enabled && laser.violetHelix.enabled, "Laser channel and both helical filaments are active");
                            Check(Vector3.Distance(laser.coreBeam.GetPosition(1), e.LaserOrigin + e.LaserDirection * e.LaserLength) < .02f, "Beam visual ends at the physical hit distance");
                            Capture("02_laser_channel"); Step(3);
                        } break;
                    case 3:
                        if (e.CurrentState == BossEncounter.State.Recovery) { Session(0); Step(10); } break;
                    case 10:
                        if (Is(BossEncounter.State.Attack, 0, .5f))
                        {
                            Check(d.CurrentClip == "AA_VolleyRelease", "Volley follows the imported staff-sweep clip");
                            var projectiles = e.Arena.effectRoot.GetComponentsInChildren<WardenSpellMotion>().Where(x => x.projectile).ToArray();
                            Check(projectiles.Length > 0 && projectiles.All(p => p.trail && p.trail.positionCount == 14), "Forbidden-page projectiles carry bounded trail geometry");
                            Check(projectiles[0].ornament.GetComponentInChildren<Renderer>().bounds.size.magnitude>.2f,"Forbidden-page mesh has a visible world-space size");
                            Capture("03_forbidden_pages"); Step(11);
                        } break;
                    case 11:
                        if (e.CurrentState == BossEncounter.State.Recovery) { Session(2); Step(19); } break;
                    case 19:
                        if(Is(BossEncounter.State.Telegraph,2,.8f)){Step(20);OrbitRifts();}break;
                    case 20:
                        OrbitRifts();
                        if (Is(BossEncounter.State.Attack, 2, .5f))
                        {
                            var patches = e.Arena.effectRoot.GetComponentsInChildren<GroundFractureVisuals>();
                            Check(d.CurrentClip == "AA_FractureRelease" && patches.Length>0 && patches.Length<=10, "Sustained gesture accompanies a bounded advancing fracture line");
                            Check(patches.All(p=>p.embers.Length==3) && patches.Any(p=>p.embers.Any(x=>x.gameObject.activeInHierarchy)), "Fractures release restrained rising gilt embers");
                            Capture("04_fracture_eruption"); Step(21);
                        } break;
                    case 21:
                        OrbitRifts();
                        if (e.CurrentState == BossEncounter.State.Recovery) { Session(3); Step(30); } break;
                    case 30:
                        if (Is(BossEncounter.State.Attack, 4, .2f))
                        {
                            Check(d.CurrentClip == "AA_RushDrive" && d.rushRibbons.All(r => r.enabled), "Rush pose and paired wakes follow committed movement");
                            Capture("05_rush"); Step(31);
                        } break;
                    case 31:
                        if (e.CurrentState == BossEncounter.State.Recovery) { Session(4); Step(40); } break;
                    case 40:
                        if (Is(BossEncounter.State.Attack, 1, .6f))
                        {
                            Check(d.CurrentClip == "AA_WaveRelease" && e.ActiveAttackCount > 0, "Runic shock crown expands during the wave gesture");
                            Capture("06_shock_crown"); Step(41);
                        } break;
                    case 41:
                        if (e.CurrentState == BossEncounter.State.Recovery)
                        { e.DamageBoss(e.Definition.health * .55f, false); Step(50); } break;
                    case 50:
                        if (e.CurrentState == BossEncounter.State.PhaseBreak && e.AttackElapsed > 1.4f)
                        {
                            Check(d.CurrentClip == "AA_PhaseBreak" && d.phaseSeal.gameObject.activeInHierarchy, "Phase break opens the seal and imported pose"); Capture("07_phase_break"); Step(51);
                        } break;
                    case 51:
                        if (e.CurrentState == BossEncounter.State.Observe) { e.DamageBoss(e.Definition.health, false); Step(60); } break;
                    case 60:
                        if (t > 3.5f)
                        {
                            Check(e.CurrentState == BossEncounter.State.Won && d.CurrentClip == "AA_Defeat" && d.SampleTime > 3, "Defeat animation reaches the end despite frozen gameplay state time");
                            Capture("08_defeat"); e.Retry(); Step(61);
                        } break;
                    case 61:
                        if (t > .4f)
                        {
                            Check(e.CurrentState == BossEncounter.State.Intro && e.Phase == 1 && d.CurrentClip == "AA_Intro", "Retry restores the phase and rig pose");
                            Check(e.ReturnToLibrary(), "Return to library after victory and retry"); Step(70);
                        } break;
                    case 70:
                        if (t > .4f)
                        {
                            Check(UnityEngine.Object.FindObjectsByType<WardenAnimationDriver>(FindObjectsSortMode.None).Length == 0 && UnityEngine.Object.FindObjectsByType<WardenSpellMotion>(FindObjectsSortMode.None).Length == 0, "No rig or ornate spell instances remain in library");
                            Check(e.View.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing == oldPost, "Returning restores the original camera post-processing setting");
                            step = 100; Write(); Stop();
                        } break;
                }
                if (e.CurrentState == BossEncounter.State.Lost) throw new InvalidOperationException("Unexpected defeat during presentation case " + step);
            }
            catch (Exception ex) { failure = ex.ToString(); step = -1; Write(); Stop(); if (menu) menu.Open(); Debug.LogError("[Warden v2 acceptance] " + ex.Message); }
        }
        static void Write() { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/UnityPresentationAcceptance.json", Status); Debug.Log("[Warden v2 acceptance] " + Status); }
        static void OrbitRifts()
        {
            Vector3 toward=e.Arena.transform.position-player.transform.position;toward.y=0;
            player.transform.rotation=Quaternion.LookRotation(toward);Keys(Key.D);
        }
    }
}
