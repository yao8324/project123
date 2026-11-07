using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using Object=UnityEngine.Object;
namespace ArcaneArchive.Visuals.Editor
{
    [InitializeOnLoad]
    public static class SanctuaryR3CombatCheck
    {
        const string Key="R3CombatCheck";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static BossEncounter e;static IEnumerator run;static BossTestInputScope input;static double deadline;
        static List<string> checks=new List<string>(),errors=new List<string>();
        static SanctuaryR3CombatCheck(){EditorApplication.playModeStateChanged+=Changed;}
        public static void RefreshAllAndRun(){SanctuaryR3Import.Build();SanctuaryR3SurfacePass.Run();BuildAndRun();}
        public static void BuildAndRun(){SanctuaryR3CombatBuilder.Build();Run();}
        public static void Run(){SessionState.SetInt(Key+".AA",QualitySettings.antiAliasing);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(SanctuaryR3CombatBuilder.Scene);EditorApplication.isPlaying=true;}
        static void Changed(PlayModeStateChange state){if(!SessionState.GetBool(Key,false))return;if(state==PlayModeStateChange.EnteredPlayMode){checks.Clear();errors.Clear();run=null;e=null;deadline=EditorApplication.timeSinceStartup+160;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}}
        static void Log(string text,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&!stack.Contains("UnityEditor.Search"))errors.Add(text+"\n"+stack);}
        static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("R3 combat check timed out");if(run==null){e=Object.FindFirstObjectByType<BossEncounter>();if(!e||!e.IsActive)return;run=Review();}if(!run.MoveNext())Finish(null);}catch(Exception ex){Finish(ex.GetBaseException().ToString());}}
        static void Set(string field,object value){typeof(BossEncounter).GetField(field,Flags).SetValue(e,value);}
        static object Call(string method,params object[] args){return typeof(BossEncounter).GetMethod(method,Flags).Invoke(e,args);}
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);}
        static void Step(float dt){Set("combatTime",e.BattleTime+dt);Call("TickKnight",dt);Call("TickShots",dt);Call("PresentBoss",dt);Physics.SyncTransforms();}
        static void Reset(){Call("ClearAttacks");e.Arena.boss.localPosition=Vector3.zero;e.Arena.boss.localRotation=Quaternion.identity;e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,20));Call("SetState",BossEncounter.State.Observe);Physics.SyncTransforms();}
        static IEnumerator Review()
        {
            input=new BossTestInputScope();e.enabled=false;e.GetComponent<FirstPersonController>().enabled=false;e.GetComponent<KnightTrialLauncher>().enabled=false;e.GetComponent<RecoveryMenu>().Resume();Set("focusLost",false);
            typeof(PlayerCombat).GetField("<Health>k__BackingField",Flags).SetValue(e.Combat,100000f);
            Check(e.KnightMode,"Actual KnightTrialLauncher starts R3 combat");
            var lighting=e.Arena.GetComponent<ArcaneArchive.Visuals.KnightSanctuaryLighting>();if(lighting){Check(QualitySettings.renderPipeline==lighting.pipeline,"R3 scoped pipeline active in combat");Check(e.Arena.GetComponentsInChildren<ReflectionProbe>().Any(p=>p.mode==UnityEngine.Rendering.ReflectionProbeMode.Custom && p.customBakedTexture),"Baked room reflection available in live arena");}Check(e.Arena.radius==28,"Gameplay radius is 28m");
            Check(Vector3.Distance(e.Arena.boss.localPosition,new Vector3(0,0,25))<.01f,"Boss reset uses 25m spawn marker");Check(Mathf.Abs(e.Arena.spawn.localPosition.z+22)<.01f,"Player spawn is -22m");
            Check(e.Arena.GetComponentsInChildren<BoxCollider>().Count(c=>c.name.StartsWith("R3_CombatBoundary_"))==96,"Player boundary uses new 28m perimeter, not old 14m colliders");
            var visuals=e.Arena.boss.GetComponent<SanctuaryR3CombatVisuals>();Check(visuals&&visuals.actor,"Live boss has the R3 skinned actor adapter");
            var root=visuals.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Root");
            foreach(var clip in visuals.clips)Check(!AnimationUtility.GetCurveBindings(clip).Any(b=>(b.path=="Root"||b.path.EndsWith("/Root"))&&b.propertyName.StartsWith("m_LocalPosition")),clip.name+": no duplicate animation displacement");
            foreach(var move in new[]{KnightMove.SealThrust,KnightMove.LeapCleave}){
                Reset();e.BeginKnightMove(move);float peak=0,travel=0;int selected=-1;
                checks.Add("START "+move+" state="+e.CurrentState+" paused="+e.IsPaused);
                var cap=e.Arena.boss.GetComponent<CapsuleCollider>();var center=cap.transform.TransformPoint(cap.center);float half=cap.height*.5f-cap.radius;
                foreach(var hit in Physics.CapsuleCastAll(center-Vector3.up*half,center+Vector3.up*half,cap.radius,Vector3.forward,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(e.Arena.boss))checks.Add("SWEEP hit="+hit.collider.name+" distance="+hit.distance+" bounds="+hit.collider.bounds);

                for(int frame=0;frame<(move==KnightMove.SealThrust?56:95);frame++){
                    Step(1/60f);peak=Mathf.Max(peak,e.Arena.boss.localPosition.y);travel=Mathf.Max(travel,new Vector2(e.Arena.boss.localPosition.x,e.Arena.boss.localPosition.z).magnitude);selected=Mathf.Max(selected,visuals.CurrentClip);
                    Check(root.localPosition.magnitude<.02f,"Root remains in-place during "+move+" frame "+frame);
                    if(frame%15==0){checks.Add("TRACE "+move+" frame="+frame+" state="+e.CurrentState+" age="+e.AttackElapsed+" pos="+e.Arena.boss.localPosition);yield return null;}
                }
                float expected=move==KnightMove.SealThrust?8:10;Check(Mathf.Abs(travel-expected)<.12f,move+" actual controller travel="+travel);
                Check(selected==(move==KnightMove.SealThrust?1:2),move+" selects correct clip");if(move==KnightMove.LeapCleave)Check(Mathf.Abs(peak-4)<.04f,"Actual leap controller apex="+peak);
            }
            Reset();e.Arena.boss.localPosition=new Vector3(0,0,26);e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,27.5f));Physics.SyncTransforms();e.BeginKnightMove(KnightMove.SealThrust);
            for(int i=0;i<60;i++){Step(1/60f);if(i%15==0)yield return null;}
            Check(new Vector2(e.Arena.boss.localPosition.x,e.Arena.boss.localPosition.z).magnitude<28,"Dash respects expanded arena boundary");
            e.Finish(false);yield return null;e.Retry();yield return null;Check(Vector3.Distance(e.Arena.boss.localPosition,new Vector3(0,0,25))<.01f,"Retry restores R3 boss spawn");
            var oathReview=CheckOathMotion();while(oathReview.MoveNext())yield return oathReview.Current;
            var remaining=CheckRemainingMoves();while(remaining.MoveNext())yield return remaining.Current;
            var damageLoop=DamageAndResults();while(damageLoop.MoveNext())yield return damageLoop.Current;
            Check(e.ReturnToLibrary(),"R3 combat returns to the original library");yield return null;Check(QualitySettings.antiAliasing==SessionState.GetInt(Key+".AA",0),"Return restores previous anti-aliasing setting");
            Check(errors.Count==0,"No encounter/adapter runtime errors (editor SearchDatabase excluded)");
        }
        static IEnumerator CheckOathMotion()
        {
            Reset();e.BeginKnightMove(KnightMove.OathCombo);
            var visual=e.Arena.boss.GetComponent<SanctuaryR3CombatVisuals>();
            var arm=visual.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="UpperArm.R");
            var hand=visual.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="Hand.R");
            Set("stateTime",.89f);visual.Present(e);yield return null;yield return null;CaptureOath("Windup");var windup=arm.localRotation;var handStart=hand.position;
            Call("SetState",BossEncounter.State.Attack);Set("stateTime",.30f);visual.Present(e);
            Check(Quaternion.Angle(windup,arm.localRotation)>25 && Vector3.Distance(handStart,hand.position)>.15f,"R3 ordinary sword combo visibly moves sword arm from windup through cut");
            Check(Vector3.Dot(hand.position-e.Arena.boss.position,e.Arena.boss.forward)>.15f,"Sword hand stays in front of torso during the cut");
            yield return null;yield return null;CaptureOath("FirstCut");var firstCut=arm.localRotation;Set("stateTime",1.07f);visual.Present(e);yield return null;yield return null;CaptureOath("ReturnCut");
            Check(Quaternion.Angle(firstCut,arm.localRotation)>25,"R3 return slash has a distinct arm pose");
            var stable=arm.localRotation;for(int i=0;i<20;i++)visual.Present(e);
            Check(Quaternion.Angle(stable,arm.localRotation)<.02f,"Repeated presentation at same combat time does not accumulate rotations");
            typeof(BossEncounter).GetField("<Phase>k__BackingField",Flags).SetValue(e,2);
            Set("stateTime",1.54f);visual.Present(e);Check(Vector3.Dot(BladeDirection(visual),Vector3.up)>.75f,"Overhead finisher raises actual blade above the knight");yield return null;yield return null;CaptureOath("Overhead");var raised=arm.localRotation;
            Set("stateTime",1.84f);visual.Present(e);yield return null;yield return null;CaptureOath("ThirdCut");
            Check(Quaternion.Angle(raised,arm.localRotation)>45,"Phase-two finisher visibly cuts down at its impact window");
            Call("SetState",BossEncounter.State.Recovery);Set("stateTime",.6f);visual.Present(e);var recovered=arm.localRotation;
            Call("SetState",BossEncounter.State.Intro);visual.Present(e);
            Check(Quaternion.Angle(recovered,arm.localRotation)<.02f,"Combo recovery clears the procedural arm pose");
            typeof(BossEncounter).GetField("<Phase>k__BackingField",Flags).SetValue(e,1);
            Reset();
        }
        static IEnumerator CheckRemainingMoves()
        {
            Reset();e.BeginKnightMove(KnightMove.OathCombo);Set("stateTime",.5f);Call("PresentBoss",0f);
            var visual=e.Arena.boss.GetComponent<SanctuaryR3CombatVisuals>();
            var fx=e.Arena.boss.GetComponent<KnightVisuals>().refinedEffects;
            var charge=e.Arena.effectRoot.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l=>l.name=="Gathering oath at blade tip");
            Check(charge,"R3 telegraph presents the existing blade-tip charge effect");
            Check(fx.bladeTip.IsChildOf(visual.actor.transform),"Charge and trail bind to new knight skeleton rather than hidden source actor");
            yield return null;yield return null;
            checks.Add("Socket diagnostic socket="+fx.bladeTip.position.ToString("F5")+" mesh="+BladeTipPosition(visual).ToString("F5")+" actorScale="+visual.actor.transform.lossyScale);
            Check(Vector3.Distance(fx.bladeTip.position,BladeTipPosition(visual))<.01f,"VFX socket matches the actual deformed blade tip");
            var center=Vector3.zero;for(int i=0;i<charge.positionCount-1;i++)center+=charge.GetPosition(i);center/=charge.positionCount-1;
            Check(Vector3.Distance(center,fx.bladeTip.position)<.01f,"Charge ring follows current blade tip");
            Call("SetState",BossEncounter.State.Attack);
            for(int i=0;i<5;i++){Set("stateTime",.08f+i*.04f);Set("combatTime",e.BattleTime+.04f);Call("PresentBoss",.04f);yield return null;}
            var trail=e.Arena.effectRoot.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l=>l.name=="Golden oath blade ribbon");
            Check(trail && Vector3.Distance(trail.GetPosition(trail.positionCount-1),fx.bladeTip.position)<.01f,"Slash trail ends at the current R3 blade tip");
            e.Arena.boss.GetComponent<KnightVisuals>().Release(e,0);yield return null;
            Check(e.Arena.effectRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="Knight release: OathCombo"),"Actual strike creates its release effect");
            Call("SetState",BossEncounter.State.Recovery);Set("stateTime",1f);Set("combatTime",e.BattleTime+2);Call("PresentBoss",0f);
            int expiryFrame=Time.frameCount;do{EditorApplication.QueuePlayerLoopUpdate();yield return null;}while(Time.frameCount<=expiryFrame+1);
            checks.Add("Remaining effect roots: "+string.Join(",",e.Arena.effectRoot.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Knight release") || t.name.Contains("blade ribbon") || t.name.Contains("Gathering oath")).Select(t=>t.name)));
            Check(!e.Arena.effectRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="Knight release: OathCombo" || t.name=="Golden oath blade ribbon" || t.name=="Gathering oath at blade tip"),"R3 effects expire after attack instead of accumulating forever");
            var arm=visual.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="UpperArm.R");
            foreach(var move in new[]{KnightMove.OrbitSweep,KnightMove.BlinkChase})
            {
                Reset();e.BeginKnightMove(move);Set("stateTime",e.KnightCurrentSpec.warning-.01f);Call("PresentBoss",0f);var before=arm.localRotation;
                Call("SetState",BossEncounter.State.Attack);Set("stateTime",e.KnightCurrentSpec.impacts[0]+.12f);Call("PresentBoss",0f);
                Check(Quaternion.Angle(before,arm.localRotation)>20,move+" visibly swings on the imported skeleton");
                yield return null;yield return null;CaptureOath("R14_"+move);
            }
            Reset();e.BeginKnightMove(KnightMove.Heavenfall);Set("stateTime",1.5f);Call("PresentBoss",0f);
            Check(Vector3.Dot(BladeDirection(visual),Vector3.up)>.75f,"Lightning telegraph raises the R3 sword toward the sky");
            yield return null;yield return null;CaptureOath("R14_Heavenfall");
            Reset();e.BeginKnightMove(KnightMove.BlinkBarrage);Call("SetState",BossEncounter.State.Intro);Call("PresentBoss",0f);
            var shieldArm=visual.actor.GetComponentsInChildren<Transform>().First(t=>t.name=="UpperArm.L");var rest=shieldArm.localRotation;
            Call("SetState",BossEncounter.State.Telegraph);Set("stateTime",1f);Call("PresentBoss",0f);
            Check(Quaternion.Angle(rest,shieldArm.localRotation)>20,"Barrage has a distinct shield-arm casting pose");
            Check(fx.readyBlades!=null && fx.readyBlades.Any(g=>g && g.activeInHierarchy && g.GetComponentsInChildren<Renderer>().Any(r=>r.enabled)),"Barrage ready-blade renderers are visible on R3 arena");
            var stable=shieldArm.localRotation;for(int i=0;i<20;i++)Call("PresentBoss",0f);
            Check(Quaternion.Angle(stable,shieldArm.localRotation)<.02f,"Repeated casting presentation does not accumulate shield-arm rotation");
            yield return null;yield return null;CaptureOath("R14_Barrage");
            Call("SetState",BossEncounter.State.Recovery);Set("stateTime",.6f);Call("PresentBoss",0f);var recovered=shieldArm.localRotation;
            Call("SetState",BossEncounter.State.Intro);Call("PresentBoss",0f);
            Check(Quaternion.Angle(recovered,shieldArm.localRotation)<.02f,"Barrage recovery restores the shield-arm clip pose");Reset();
        }
        static Vector3 BladeTipPosition(SanctuaryR3CombatVisuals visual)
        {
            var skin=visual.actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("S01_DiamondSectionBlade"));
            var hand=visual.actor.GetComponentsInChildren<Transform>().First(x=>x.name=="Hand.R");
            // Compensate the imported FBX renderer scale before TransformPoint.
            var baked=new Mesh();skin.BakeMesh(baked,true);
            var tip=baked.vertices.Select(v=>skin.transform.TransformPoint(v)).OrderByDescending(v=>(v-hand.position).sqrMagnitude).First();
            Object.DestroyImmediate(baked);return tip;
        }
        static Vector3 BladeDirection(SanctuaryR3CombatVisuals visual)
        {
            var skin=visual.actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(x=>x.name.StartsWith("S01_DiamondSectionBlade"));
            var hand=visual.actor.GetComponentsInChildren<Transform>().First(x=>x.name=="Hand.R");
            // Compensate the imported FBX renderer scale before TransformPoint.
            var baked=new Mesh();skin.BakeMesh(baked,true);Vector3 tip=Vector3.zero,heel=Vector3.zero;float far=-1,near=float.MaxValue;
            foreach(var vertex in baked.vertices){var world=skin.transform.TransformPoint(vertex);float d=(world-hand.position).sqrMagnitude;if(d>far){far=d;tip=world;}if(d<near){near=d;heel=world;}}
            Object.DestroyImmediate(baked);return (tip-heel).normalized;
        }
        static void CaptureOath(string label)
        {
            var go=new GameObject("R13 pose review camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(e.View);
            camera.transform.position=e.Arena.boss.position+new Vector3(3.6f,2.5f,6);
            camera.transform.LookAt(e.Arena.boss.position+Vector3.up*(label.StartsWith("R14_")?1.8f:1.25f));camera.fieldOfView=label.StartsWith("R14_")?46:34;
            var target=RenderTexture.GetTemporary(1400,1000,24);var previous=RenderTexture.active;
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1400,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1400,1000),0,0);texture.Apply();
            File.WriteAllBytes("Docs/SanctuaryR3Build/Unity_OathR13_"+label+".png",texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(texture);Object.DestroyImmediate(go);
        }
        // These tests catch missing hit geometry, broken damage/phase transitions and
        // broken results/retry wiring. AI is controlled; this is not a human playthrough.
        static void PlaceForSword(float distance,bool faceTarget=true)
        {
            e.Arena.boss.localPosition=Vector3.zero;e.Arena.boss.localRotation=Quaternion.identity;
            e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,-distance));
            var target=e.Arena.boss.position+Vector3.up*1.3f;
            e.View.transform.rotation=Quaternion.LookRotation((faceTarget?target-e.View.transform.position:e.View.transform.position-target).normalized);
            Physics.SyncTransforms();
        }
        static IEnumerator DamageAndResults()
        {
            var melee=e.GetComponent<PlayerMelee>();var menu=e.GetComponent<RecoveryMenu>();
            var player=e.GetComponent<FirstPersonController>();
            Check(melee && melee.loadout,"R3 scene supplies the real sword loadout");
            Check(e.Combat.Health==100 && e.BossHealth==1400,"Retry restores configured 100/1400 health without test overrides");
            Call("SetState",BossEncounter.State.Observe);
            foreach(var miss in new[]{0,1})
            {
                PlaceForSword(miss==0?10:2,miss==0);float before=e.BossHealth;uint hits=melee.HitSequence;
                Check(melee.TryLightAttack(),"Sword accepts controlled miss attempt "+miss);
                for(int i=0;i<90;i++)e.Combat.Tick(1/30f);
                Check(e.BossHealth==before && melee.HitSequence==hits,miss==0?"Out-of-range sword swing does not damage knight":"Facing-away sword swing does not damage knight");
                yield return null;
            }
            int landed=0;bool sawPhaseTwo=false;
            while(!e.IsFinished && landed<70)
            {
                if(e.CurrentState==BossEncounter.State.PhaseBreak)
                {
                    for(int i=0;i<140 && e.CurrentState==BossEncounter.State.PhaseBreak;i++)Step(1/60f);
                    Check(e.CurrentState!=BossEncounter.State.PhaseBreak,"Phase transition resumes combat");
                }
                // Hold a neutral target between swings; retain actual health, stamina,
                // damage, phase and result logic throughout the sequence.
                Call("ClearAttacks");Call("SetState",BossEncounter.State.Observe);PlaceForSword(2);
                float before=e.BossHealth;uint hits=melee.HitSequence;
                Check(melee.TryLightAttack(),"Sword begins live swing "+landed);
                for(int i=0;i<90 && !e.IsFinished;i++)e.Combat.Tick(1/30f);
                Check(e.BossHealth<before && melee.HitSequence==hits+1,"One sword swing resolves one real hit "+landed);
                sawPhaseTwo|=e.Phase==2;landed++;if(landed%5==0)yield return null;
            }
            Check(sawPhaseTwo,"Sword damage crosses the knight phase threshold");
            Check(e.CurrentState==BossEncounter.State.Won && e.BossHealth==0,"Repeated real sword hits defeat full-health knight; hits="+landed);
            Check(menu.CurrentPage==RecoveryMenu.Page.BattleResult && !player.GameplayInputEnabled,"Victory opens results and suppresses gameplay input");
            Check(!melee.TryLightAttack(),"Victory prevents further sword attacks");
            e.Retry();yield return null;
            Check(e.CurrentState==BossEncounter.State.Intro && e.Combat.Health==100 && e.BossHealth==1400 && menu.CurrentPage==RecoveryMenu.Page.Explore && player.GameplayInputEnabled,"Retry after victory restores health, intro, UI and controls");
            int attacks=0;float initial=e.Combat.Health;
            while(!e.IsFinished && attacks<12)
            {
                Reset();e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,2));Physics.SyncTransforms();
                e.BeginKnightMove(KnightMove.OathCombo);
                for(int i=0;i<240 && !e.IsFinished;i++){e.Combat.Tick(1/60f);Step(1/60f);if(i%60==0)yield return null;}
                attacks++;
            }
            Check(e.Combat.Health<initial && e.KnightImpactCount>0,"Knight attack hit areas damage actual player health");
            Check(e.Combat.Health==0 && e.CurrentState==BossEncounter.State.Lost,"Repeated knight attacks cause player defeat");
            Check(menu.CurrentPage==RecoveryMenu.Page.BattleResult && !player.GameplayInputEnabled,"Defeat opens results and suppresses gameplay input");
            e.Retry();yield return null;
            Check(e.CurrentState==BossEncounter.State.Intro && e.Combat.Health==100 && e.BossHealth==1400 && player.GameplayInputEnabled,"Retry after actual defeat restores a playable challenge");
        }
        static void Finish(string error)
        {
            input?.Dispose();input=null;Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SessionState.SetBool(Key,false);
            string text="Controlled actual Play Mode combat checks; not full gameplay or visual acceptance.\n"+string.Join("\n",checks)+"\nErrors:\n"+string.Join("\n",errors)+"\nFailure: "+(error??"none");File.WriteAllText("Docs/SanctuaryR3Build/CombatRuntimeCheck.txt",text);if(error==null)Debug.Log("R3_COMBAT_CHECK_COMPLETE");else Debug.LogError(error);EditorApplication.Exit(error==null?0:1);
        }
    }
}
