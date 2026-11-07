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
using Object=UnityEngine.Object;

namespace ArcaneArchive.Visuals.Editor
{
    public static class KnightReferenceReview
    {
        static BossEncounter e;static KnightVisuals visual;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly List<string> checks=new List<string>(),events=new List<string>();
        static int spawned;
        static void Set(object o,string name,object value)=>o.GetType().GetField(name,Private).SetValue(o,value);
        static void Property(object o,string name,object value)=>Set(o,"<"+name+">k__BackingField",value);
        static object Call(string method,params object[] args)=>typeof(BossEncounter).GetMethod(method,Private).Invoke(e,args);
        static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        static void Step(float dt)
        {
            Set(e,"combatTime",e.BattleTime+dt);e.Combat.Tick(dt);
            var shots=(System.Collections.IList)typeof(BossEncounter).GetField("shots",Private).GetValue(e);int before=shots.Count;
            Call("TickKnight",dt);spawned=shots.Count-before;Call("TickShots",dt);Call("PresentBoss",dt);Physics.SyncTransforms();
        }
        static void Reset(int phase)
        {
            Call("ClearAttacks");Call("SetState",BossEncounter.State.Observe);Property(e,"Phase",phase);Property(e.Combat,"Health",100000f);
            e.Arena.boss.localPosition=new Vector3(0,0,4);e.Arena.boss.localRotation=Quaternion.Euler(0,180,0);
            e.transform.position=e.Arena.transform.TransformPoint(new Vector3(0,.08f,-5));Physics.SyncTransforms();
        }
        [Serializable] class Report {public bool complete;public string failure;public string[] checks,events;public int[] triangles;public string scope;}
        public static string Run()
        {
            checks.Clear();events.Clear();e=Object.FindFirstObjectByType<BossEncounter>();if(!e||!e.IsActive)throw new InvalidOperationException("Enter the production test scene first.");
            visual=e.Arena.boss.GetComponent<KnightVisuals>();
            e.enabled=false;e.GetComponent<KnightTrialLauncher>().enabled=false;e.GetComponent<FirstPersonController>().enabled=false;
            var menu=e.GetComponent<RecoveryMenu>();menu.Resume();Set(e,"focusLost",false);
            string failure=null;
            using(var input=new BossTestInputScope())try
            {
                var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Check(skins.Length==2,"Two skinned LODs loaded by the actual production arena");
                foreach(var skin in skins){Check(skin.bones.Length==100 && skin.sharedMesh.bindposes.Length==100,skin.name+": 100-bone binding");Check(skin.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1-1)<.0001f),skin.name+": normalized weights");Check(skin.sharedMaterials.Length==skin.sharedMesh.subMeshCount,skin.name+": all materials assigned");}
                foreach(int phase in new[]{1,2})foreach(float dt in new[]{1f/60,1f/15,.19f})
                {
                    Reset(phase);int impacts=e.KnightImpactCount;int blinks=0;int[] waves=new int[3];int previousImpact=impacts;
                    e.BeginKnightMove(KnightMove.BlinkBarrage);int frame=0;var last=e.Arena.boss.position;
                    while(e.CurrentState!=BossEncounter.State.Recovery && frame++<900)
                    {
                        Step(dt);var now=e.Arena.boss.position;
                        if(Vector3.Distance(last,now)>.2f){blinks++;events.Add("P"+phase+" dt="+dt+" blink round="+e.KnightBarrageRound+" position="+now);last=now;}
                        if(e.KnightImpactCount!=previousImpact){waves[e.KnightBarrageRound-1]++;events.Add("P"+phase+" dt="+dt+" release round="+e.KnightBarrageRound+" projectiles="+spawned);Check(blinks==e.KnightBarrageRound && spawned==5,"Five projectiles follow their own blink, round "+e.KnightBarrageRound);previousImpact=e.KnightImpactCount;}
                    }
                    Check(e.CurrentState==BossEncounter.State.Recovery && blinks==3 && waves.All(n=>n==1) && e.KnightImpactCount-impacts==3,"P"+phase+" dt="+dt+": exactly 3 blinks and one volley per blink");
                    for(int i=0;i<4;i++)Step(.2f);Check(e.KnightImpactCount-impacts==3,"No fourth volley during recovery");
                }
                // Cancellation between rounds must terminate the sequence immediately.
                Reset(1);e.BeginKnightMove(KnightMove.BlinkBarrage);for(int i=0;i<120;i++)Step(1f/60);
                Call("DamageKnight",e.BossHealth*.5f,false);int stopped=e.KnightImpactCount;
                for(int i=0;i<60;i++)Step(1f/60);Check(e.CurrentState==BossEncounter.State.PhaseBreak && e.KnightImpactCount==stopped,"Phase transition cancels the remaining rounds");
                foreach(var move in new[]{KnightMove.OathCombo,KnightMove.OrbitSweep,KnightMove.LeapCleave,KnightMove.BlinkChase,KnightMove.Heavenfall})
                {
                    Reset(2);e.BeginKnightMove(move);int frames=0;float grip=0;
                    while(e.CurrentState!=BossEncounter.State.Recovery && frames++<600){Step(1f/60);grip=Mathf.Max(grip,visual.productionRig.RightGripError);}
                    Check(e.CurrentState==BossEncounter.State.Recovery && grip<.03f,move+": reaches recovery with sword grip error below 3 cm");
                }
                Reset(1);Set(e,"knightMove",KnightMove.BlinkBarrage);
                foreach(Transform child in e.Arena.effectRoot)child.gameObject.SetActive(false);
                for(int i=0;i<90;i++){Set(e,"combatTime",e.BattleTime+1f/60);Call("PresentBoss",1f/60);}
                Capture("Hero",new Vector3(-3.7f,2.9f,-2.7f),new Vector3(0,1.75f,4),39);
                Capture("Front",new Vector3(0,2.25f,-3),new Vector3(0,1.8f,4),37);
                Capture("Back",new Vector3(2.6f,2.8f,10.5f),new Vector3(0,1.85f,4),39);
                e.BeginKnightMove(KnightMove.BlinkBarrage);for(int i=0;i<95;i++)Step(1f/60);Capture("Barrage",new Vector3(-7,4,-8),new Vector3(0,1.6f,2),65);
                var pos=e.Arena.boss.position;float clock=e.BattleTime;int count=e.KnightImpactCount;menu.ShowPage(RecoveryMenu.Page.Menu);Call("Update");
                Check(e.IsPaused && e.BattleTime==clock && e.Arena.boss.position==pos && e.KnightImpactCount==count,"Pause freezes movement, clock and releases");menu.Resume();Set(e,"focusLost",false);
                e.Finish(false);Check(e.ActiveAttackCount==0,"Defeat clears all hazards");
                var config=AssetDatabase.LoadAssetAtPath<KnightDefinition>(KnightProductionBuilder.Config);Check(e.ReturnToLibrary(),"Return to library succeeds");Check(e.BeginKnight(config),"Re-entry with updated model succeeds");
            }
            catch(Exception ex){failure=ex.GetBaseException().ToString();}
            var report=new Report{complete=failure==null,failure=failure,checks=checks.ToArray(),events=events.ToArray(),triangles=visual?visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s=>s.sharedMesh.triangles.Length/3).ToArray():new int[0],scope="Controlled production-scene checks using explicit battle time and high player health. No standalone build or human playtest."};
            Directory.CreateDirectory(KnightReferenceBuilder.Output);File.WriteAllText(KnightReferenceBuilder.Output+"/Verification.json",JsonUtility.ToJson(report,true));return JsonUtility.ToJson(report);
        }
        public static void Capture(string name,Vector3 position,Vector3 target,float fov)
        {
            Directory.CreateDirectory(KnightReferenceBuilder.Output+"/Captures");var go=new GameObject("Reference review camera");var c=go.AddComponent<Camera>();c.enabled=false;c.nearClipPlane=.05f;c.farClipPlane=100;c.fieldOfView=fov;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.04f,.045f,.055f);
            c.transform.position=e.Arena.transform.TransformPoint(position);c.transform.LookAt(e.Arena.transform.TransformPoint(target));go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var lightObject=new GameObject("Temporary review soft key");var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.intensity=22;light.range=15;light.color=new Color(1,.9f,.77f);light.transform.position=e.Arena.transform.TransformPoint(new Vector3(-2,4,-1));
            var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1200,1400,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var image=new Texture2D(1200,1400,TextureFormat.RGB24,false);
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,1400),0,0);image.Apply();File.WriteAllBytes(KnightReferenceBuilder.Output+"/Captures/"+name+".png",image.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(go);Object.DestroyImmediate(lightObject);}
        }
        [Serializable] class GripReport
        {
            public bool complete;public string failure;public string[] checks;
            public float maxRightPalmError,maxLeftPalmError,maxRightWristAngle,maxLeftWristAngle,minCutPlaneAlignment=1;
        }
        public static string RunGrip()
        {
            checks.Clear();e=Object.FindFirstObjectByType<BossEncounter>();
            if(!e||!e.IsActive)throw new InvalidOperationException("Start the production encounter first.");
            if(e.IsFinished)e.Retry();visual=e.Arena.boss.GetComponent<KnightVisuals>();
            e.enabled=false;var launcher=e.GetComponent<KnightTrialLauncher>();if(launcher)launcher.enabled=false;
            e.GetComponent<FirstPersonController>().enabled=false;var menu=e.GetComponent<RecoveryMenu>();menu.Resume();Set(e,"focusLost",false);
            var report=new GripReport();var rig=visual.productionRig;Vector3 palm=new Vector3(0,-.145f,-.045f);
            using(var input=new BossTestInputScope())try
            {
                Check(visual.sealedGreatswordGrip,"Greatsword grip enabled on production prefab");
                Check(rig.rightGrip.localPosition.y>rig.leftGrip.localPosition.y && rig.rightGrip.localPosition.y<.82f && rig.leftGrip.localPosition.y>.1f,"Main hand below guard; support hand above pommel");
                Reset(1);for(int i=0;i<90;i++){Set(e,"combatTime",e.BattleTime+1f/60);Call("PresentBoss",1f/60);}
                Capture("Grip_Idle",new Vector3(3.4f,2.8f,-2.8f),new Vector3(0,1.7f,4),38);
                Capture("Grip_Idle_Close",new Vector3(1.7f,1.7f,1.3f),new Vector3(.5f,1.45f,4),25);
                foreach(int phase in new[]{1,2})foreach(var move in new[]{KnightMove.OathCombo,KnightMove.OrbitSweep,KnightMove.LeapCleave,KnightMove.BlinkChase,KnightMove.Heavenfall,KnightMove.BlinkBarrage})
                {
                    Reset(phase);e.BeginKnightMove(move);int frames=0,oldImpact=e.KnightImpactCount;float maxR=0,maxL=0;
                    while(e.CurrentState!=BossEncounter.State.Recovery && frames++<800)
                    {
                        Step(1f/60);
                        float r=Vector3.Distance(visual.rightHand.TransformPoint(palm),visual.weapon.TransformPoint(new Vector3(0,.70f,0)));
                        maxR=Mathf.Max(maxR,r);report.maxRightPalmError=Mathf.Max(report.maxRightPalmError,r);
                        report.maxRightWristAngle=Mathf.Max(report.maxRightWristAngle,Quaternion.Angle(visual.rightHand.rotation,rig.rightGrip.rotation));
                        if(rig.TwoHandWeight>.999f)
                        {
                            float l=Vector3.Distance(visual.leftHand.TransformPoint(palm),visual.weapon.TransformPoint(new Vector3(0,.32f,0)));
                            maxL=Mathf.Max(maxL,l);report.maxLeftPalmError=Mathf.Max(report.maxLeftPalmError,l);
                            report.maxLeftWristAngle=Mathf.Max(report.maxLeftWristAngle,Quaternion.Angle(visual.leftHand.rotation,rig.leftGrip.rotation));
                        }
                        if(e.KnightImpactCount!=oldImpact)
                        {
                            bool horizontal=move==KnightMove.OrbitSweep || move==KnightMove.BlinkChase || move==KnightMove.OathCombo && e.AttackElapsed<1.46f;
                            bool overhead=move==KnightMove.LeapCleave || move==KnightMove.OathCombo && e.AttackElapsed>=1.46f;
                            if(horizontal||overhead)
                            {
                                Vector3 target=Vector3.ProjectOnPlane(horizontal?Vector3.up:e.Arena.boss.right,visual.weapon.up).normalized;
                                float alignment=Mathf.Abs(Vector3.Dot(target,visual.weapon.forward));report.minCutPlaneAlignment=Mathf.Min(report.minCutPlaneAlignment,alignment);
                                Check(alignment>.9f,"P"+phase+" "+move+": blade face follows cutting plane at impact");
                            }
                            if(phase==1 && (move==KnightMove.OathCombo||move==KnightMove.LeapCleave))
                            {
                                foreach(Transform child in e.Arena.effectRoot)child.gameObject.SetActive(false);
                                Capture("Grip_"+move+"_"+e.AttackElapsed.ToString("F2"),new Vector3(3.4f,3,-2.8f),new Vector3(0,1.9f,4),43);
                            }
                            oldImpact=e.KnightImpactCount;
                        }
                    }
                    Check(e.CurrentState==BossEncounter.State.Recovery && maxR<.03f && maxL<.03f,"P"+phase+" "+move+": palms stay on handle throughout move");
                    for(int i=0;i<30;i++)Step(1f/60);
                }
                Check(report.maxRightWristAngle<.2f && report.maxLeftWristAngle<.2f,"Both gripping wrists match their oriented sockets");
                Reset(1);e.BeginKnightMove(KnightMove.BlinkBarrage);for(int i=0;i<25;i++)Step(1f/60);
                Check(rig.TwoHandWeight<.001f && Quaternion.Angle(visual.leftHand.localRotation,Quaternion.identity)<.2f,"Casting releases the support hand to its neutral wrist frame");
                var rotation=visual.weapon.rotation;var position=visual.weapon.position;menu.ShowPage(RecoveryMenu.Page.Menu);Call("Update");
                Check(visual.weapon.rotation==rotation && visual.weapon.position==position,"Pause preserves sword pose");menu.Resume();
                report.complete=true;
            }
            catch(Exception ex){report.failure=ex.GetBaseException().ToString();}
            report.checks=checks.ToArray();File.WriteAllText(KnightReferenceBuilder.Output+"/GripVerification.json",JsonUtility.ToJson(report,true));return JsonUtility.ToJson(report);
        }
    }
}
