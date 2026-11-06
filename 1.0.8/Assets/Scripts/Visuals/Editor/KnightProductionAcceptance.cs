using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Editor-only, controlled production-clock verification and motion sampling; not a human playtest.</summary>
    [InitializeOnLoad]
    public static class KnightProductionAcceptance
    {
        const string Pending = "KnightProductionV5.Pending";
        const string Output = KnightProductionBuilder.Output;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static IEnumerator sequence;
        static BossEncounter e;
        static KnightVisuals visual;
        static KnightRigConstraints rig;
        static FirstPersonController player;
        static RecoveryMenu menu;
        static BossTestInputScope inputScope;
        static bool running;
        static int waits;
        static double deadline;
        static readonly List<string> checks = new List<string>(), errors = new List<string>(), editorIssues = new List<string>();
        static float maximumLeftGripError, maximumRightGripError, maximumFootError;
        static int sampledClips;
        static KnightProductionAcceptance()
        {
            EditorApplication.playModeStateChanged += Changed;
            if (SessionState.GetBool(Pending, false) && EditorApplication.isPlaying) EditorApplication.delayCall += StartWhenReady;
        }
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start from Edit Mode.");
            Directory.CreateDirectory(Output + "/Captures"); Directory.CreateDirectory(KnightProductionBuilder.Root + "/Animations");
            AssetDatabase.Refresh(); EditorSceneManager.OpenScene(KnightProductionBuilder.TestScene);
            SessionState.SetBool(Pending, true); SessionState.SetInt(Pending + ".Result", 1);
            EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) StartWhenReady();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                AssetDatabase.SaveAssets();
                int result = SessionState.GetInt(Pending + ".Result", 1);
                EditorApplication.delayCall += () => EditorApplication.Exit(result);
            }
        }
        static void StartWhenReady()
        {
            if (running) return;
            running = true; waits = 0; checks.Clear(); errors.Clear(); editorIssues.Clear(); sampledClips = 0;
            maximumLeftGripError = maximumRightGripError = maximumFootError = 0;
            Application.logMessageReceived += Log;
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Tick;
        }
        static void Log(string text, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if(stack.Contains("UnityEditor.Search.SearchDatabase")){editorIssues.Add(text+"\n"+stack);return;}
            errors.Add(text);
        }
        static void Check(bool condition, string description)
        { if (!condition) throw new InvalidOperationException(description); checks.Add(description); }
        static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("V5 verification timeout.");
                if (!EditorApplication.isPlaying) return;
                if (sequence == null)
                {
                    e = Object.FindFirstObjectByType<BossEncounter>();
                    if (!e || !e.IsActive) { if (++waits > 2000) throw new InvalidOperationException("Production launcher did not begin."); return; }
                    sequence = Verify();
                }
                if (!sequence.MoveNext()) Finish(null);
            }
            catch (Exception exception) { Finish(exception.GetBaseException().ToString()); }
        }
        static void Finish(string failure)
        {
            if (!running) return;
            if (failure != null) errors.Add(failure);
            running = false; sequence = null;
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
            inputScope?.Dispose(); inputScope = null;
            File.WriteAllText(Output + "/Verification.json", JsonUtility.ToJson(new Report { complete = errors.Count == 0, unity = Application.unityVersion, checks = checks.ToArray(), errors = errors.ToArray(), editorIssues=editorIssues.ToArray(), clips = sampledClips, maxLeftGripError = maximumLeftGripError, maxRightGripError = maximumRightGripError, maxFootGroundError = maximumFootError, method = "Real production scene and controller; explicit state/health setup and controlled battle-clock stepping. Known editor search-index exceptions are retained separately, never described as zero Console errors. No human difficulty or performance claim." }, true));
            SessionState.SetInt(Pending + ".Result", errors.Count == 0 ? 0 : 1);
            Debug.Log(errors.Count == 0 ? "KNIGHT_V5_VERIFICATION_COMPLETE" : "KNIGHT_V5_VERIFICATION_FAILED");
            EditorApplication.ExitPlaymode();
        }
        [Serializable] class Report { public bool complete; public string unity, method; public string[] checks, errors, editorIssues; public int clips; public float maxLeftGripError, maxRightGripError, maxFootGroundError; }
        static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
        static void Property(object target, string name, object value) { Set(target, "<" + name + ">k__BackingField", value); }
        static object Call(string name, params object[] arguments) { return typeof(BossEncounter).GetMethod(name, Private).Invoke(e, arguments); }
        static void Step(float dt)
        {
            if (e.IsPaused || e.IsFinished) return;
            Set(e, "combatTime", e.BattleTime + dt); Call("TickKnight", dt); Call("PresentBoss", dt); Physics.SyncTransforms();
            maximumRightGripError = Mathf.Max(maximumRightGripError, rig.RightGripError);
            if (rig.TwoHandWeight > .98f) maximumLeftGripError = Mathf.Max(maximumLeftGripError, rig.LeftGripError);
            maximumFootError = Mathf.Max(maximumFootError, rig.FootGroundError);
        }
        static IEnumerator Verify()
        {
            Check(e.KnightMode, "Production scene starts the actual Knight encounter");
            visual = e.Arena.boss.GetComponent<KnightVisuals>(); rig = visual.productionRig;
            Check(rig && rig.leftFoot && rig.rightFoot && rig.leftGrip && rig.rightGrip, "Rig, feet and grip attachments resolve");
            player = e.GetComponent<FirstPersonController>(); menu = e.GetComponent<RecoveryMenu>();
            inputScope = new BossTestInputScope(); e.enabled = false; player.enabled = false;
            e.GetComponent<KnightTrialLauncher>().enabled = false;
            player.SetGameplayInputEnabled(false); Set(e, "focusLost", false); menu.Resume();
            Property(e.Combat, "Health", 100000f); // Isolate every move; never represented as a normal-player victory.
            Call("SetState", BossEncounter.State.Observe);
            var original = AssetDatabase.LoadAssetAtPath<KnightDefinition>("Assets/Art/Bosses/Knight/Refined/KnightDefinitionRefined.asset");
            var production = AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);
            string Normalize(KnightDefinition d) => Regex.Replace(JsonUtility.ToJson(d), "\"arena\":\\{[^}]+\\}", "\"arena\":{}");
            var baseline=Object.Instantiate(production);
            baseline.moves=baseline.moves.Where(m=>(int)m.move<8).ToArray();
            baseline.streamlinedKit=original.streamlinedKit;
            Check(Normalize(original) == Normalize(baseline), "Original eight V4 moves and shared numeric combat parameters remain unchanged");
            Object.DestroyImmediate(baseline);
            Check(production.moves.Length==11,"Production configuration includes three 1.0.8 mobility moves");
            var skins = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Check(skins.Length == 2 && visual.GetComponent<LODGroup>(), "Two real skinned LOD meshes, without duplicate source renderers");
            foreach (var skin in skins)
            {
                Check(skin.sharedMesh.bindposes.Length == skin.bones.Length && skin.bones.All(b => b), skin.name + " bind poses and skeleton valid");
                Check(skin.sharedMesh.boneWeights.All(w => Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1) < .001f), skin.name + " skin weights normalized");
                Check(skin.sharedMesh.triangles.Length / 3 < 35000, skin.name + " below starting geometry ceiling");
            }
            Check(visual.sealRibbons.Length == 12 && rig.ribbons.All(r => r.bones.Length == 6), "Twelve streamers have six-segment skin chains");
            Check(visual.GetComponentsInChildren<Collider>(true).Length == 1, "Decorative armor and weapon do not add fragment colliders");
            Check(e.Arena.GetComponentsInChildren<Light>().Length == 3, "Forecourt uses three explicitly owned realtime lights");
            for (int n = 0; n < 72; n++)
            {
                float a = n * Mathf.PI * 2 / 72;
                Vector3 p = e.Arena.transform.TransformPoint(new Vector3(Mathf.Sin(a)*12.5f,.4f,Mathf.Cos(a)*12.5f));
                Check(!Physics.OverlapCapsule(p,p+Vector3.up*1.4f,.24f).Any(c => !c.isTrigger && !c.transform.IsChildOf(e.transform) && !c.transform.IsChildOf(e.Arena.boss) && c.bounds.max.y > e.Arena.transform.position.y + .05f), "Outer traversal clear " + n);
            }
            e.transform.position = e.Arena.transform.TransformPoint(new Vector3(-7,.08f,-5)); Physics.SyncTransforms();
            Call("SetState",BossEncounter.State.Intro);
            for (int n = 0; n < 45; n++) Step(1f/60);
            Capture("01_forecourt_entry", new Vector3(0,2,-10), new Vector3(0,2.7f,12), 62);
            Capture("02_knight_front", new Vector3(0,2.5f,-3.5f), new Vector3(0,2,4), 52);
            Capture("03_knight_rear", new Vector3(3.5f,2.7f,10.5f), new Vector3(0,2,4), 52);

            foreach (int phase in new[] {1,2})
            {
                Property(e, "Phase", phase);
                foreach (KnightMove move in Enum.GetValues(typeof(KnightMove)))
                {
                    e.Arena.boss.localPosition = new Vector3(0,0,4); e.Arena.boss.localRotation = Quaternion.Euler(0,180,0);
                    Call("ClearAttacks"); e.BeginKnightMove(move);
                    var tracks = new Dictionary<Transform,List<PoseSample>>();
                    foreach(var bone in skins[0].bones.Concat(new[]{visual.weapon,rig.leftFoot,rig.rightFoot}).Distinct())tracks[bone]=new List<PoseSample>();
                    float duration=e.KnightCurrentSpec.warning+e.KnightCurrentSpec.duration+e.KnightCurrentSpec.recovery+(move==KnightMove.OathCombo && phase==2?.78f:0);
                    int frames=Mathf.CeilToInt(duration*60);
                    for(int frame=0;frame<=frames;frame++)
                    {
                        Step(1f/60);
                        if(phase==1 && frame%3==0)foreach(var pair in tracks)pair.Value.Add(new PoseSample{time=frame/60f,position=pair.Key.localPosition,rotation=pair.Key.localRotation,scale=pair.Key.localScale});
                        if(frame==Mathf.CeilToInt(e.KnightCurrentSpec.warning*60)+16)
                        {
                            if(phase==2 && (move==KnightMove.OathCombo || move==KnightMove.OrbitSweep || move==KnightMove.Heavenfall))Capture("04_"+move,new Vector3(-6,3,-3),e.Arena.boss.localPosition+Vector3.up*1.6f,62);
                        }
                        if(frame%12==0)yield return null;
                    }
                    Check(!float.IsNaN(visual.weapon.position.x) && e.ActiveAttackCount < 40, "Finite production pose and bounded effects: "+phase+" "+move);
                    if(phase==1)Bake(move.ToString(),tracks,visual.transform);
                    if(move==KnightMove.OathCombo)
                    {
                        menu.ShowPage(RecoveryMenu.Page.Menu);
                        float clock=e.BattleTime,health=e.Combat.Health;var before=skins[0].bones.Select(b=>b.localToWorldMatrix).ToArray();
                        for(int n=0;n<6;n++){Call("Update");yield return null;}
                        Check(Mathf.Approximately(e.BattleTime,clock) && e.Combat.Health==health && skins[0].bones.Select((b,i)=>b.localToWorldMatrix==before[i]).All(v=>v),"Pause freezes clock, health and skinned pose in phase "+phase);
                        menu.Resume();player.SetGameplayInputEnabled(false);
                    }
                    Call("SetState",BossEncounter.State.Observe);
                }
            }
            Check(maximumRightGripError < .03f, "Right grip remains within 3 cm visual tolerance");
            Check(maximumLeftGripError < .03f, "Engaged left grip remains within 3 cm visual tolerance");
            Check(maximumFootError < .05f, "Ground constraint remains within 5 cm visual tolerance");
            Set(e,"knightStoryEnabled",true);Property(e,"BossHealth",e.Definition.health);Property(e,"Phase",1);Call("SetState",BossEncounter.State.Observe);
            e.DamageBoss(100000,false);
            Check(e.KnightStoryPending && !e.IsFinished && e.BossHealth>0,"Overkill requests story before death");
            float frozen=e.BattleTime;for(int n=0;n<5;n++){Call("Update");yield return null;}
            Check(e.BattleTime==frozen,"Story lock freezes combat clock");
            e.ResumeKnightTrial();Call("SetState",BossEncounter.State.Observe);e.DamageBoss(1,false);
            Check(!e.KnightStoryPending,"Consumed story beat does not fire twice");
            e.Finish(false);e.Retry();Check(e.BossHealth==e.Definition.health && !e.KnightStoryPending,"Retry restores full boss health and fresh story state");
            var environment=e.Arena.GetComponent<KnightArenaPresentation>();var ambientMode=environment.OriginalMode;var ambientSky=environment.OriginalSky;
            Check(e.ReturnToLibrary(),"Production Knight returns to library");yield return null;
            Check(RenderSettings.ambientMode==ambientMode && RenderSettings.ambientSkyColor==ambientSky,"Forecourt restores the library ambient lighting on return");
            Set(e,"focusLost",false);Check(e.Begin(),"Original Warden still starts after production Knight");e.enabled=false;
            Check(!e.KnightMode,"Warden configuration restored after Knight session");
            e.Finish(false);e.Retry();Check(e.IsActive && !e.IsFinished,"Original Warden failure/retry survives visual upgrade");
            Check(e.ReturnToLibrary(),"Warden return restores library");
            Check(errors.Count==0,"No runtime Console errors during controlled verification");
            AssetDatabase.SaveAssets();
        }
        struct PoseSample{public float time;public Vector3 position,scale;public Quaternion rotation;}
        static void Bake(string name,Dictionary<Transform,List<PoseSample>> tracks,Transform root)
        {
            var clip=new AnimationClip{name="Knight_"+name+"_Sampled",frameRate=20};
            foreach(var pair in tracks)
            {
                string path=AnimationUtility.CalculateTransformPath(pair.Key,root);
                for(int property=0;property<10;property++)
                {
                    int channel=property;Func<PoseSample,float> value=p=>channel<3?p.position[channel]:channel<7?p.rotation[channel-3]:p.scale[channel-7];
                    var samples=pair.Value;float first=value(samples[0]);bool varying=samples.Any(p=>Mathf.Abs(value(p)-first)>.0001f);
                    var keys=(varying?samples:new List<PoseSample>{samples[0],samples[samples.Count-1]}).Select(p=>new Keyframe(p.time,value(p))).ToArray();
                    var curve=new AnimationCurve(keys);for(int i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                    string field=property<3?"m_LocalPosition.":property<7?"m_LocalRotation.":"m_LocalScale.";int axis=property<3?property:property<7?property-3:property-7;
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),field+"xyzw"[axis]),curve);
                }
            }
            clip.EnsureQuaternionContinuity();string asset=KnightProductionBuilder.Root+"/Animations/"+clip.name+".anim";
            var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(asset);if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,asset);sampledClips++;
        }
        static void Capture(string name,Vector3 position,Vector3 target,float fieldOfView)
        {
            var cameraObject=new GameObject("V5 verification camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.028f,.032f,.045f);camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.fieldOfView=fieldOfView;
            camera.transform.position=e.Arena.transform.TransformPoint(position);camera.transform.LookAt(e.Arena.transform.TransformPoint(target));
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var active=RenderTexture.active;var texture=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var readback=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;readback.ReadPixels(new Rect(0,0,1440,900),0,0);readback.Apply();File.WriteAllBytes(Output+"/Captures/"+name+".png",readback.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(texture);Object.DestroyImmediate(readback);Object.DestroyImmediate(cameraObject);}
        }
    }
}
