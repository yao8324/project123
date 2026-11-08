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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Real frame/input tests. Only attack selection is seeded; timings and damage remain production values.</summary>
    public static class WardenFlowAcceptance
    {
        const string Output="Docs/BossDesign/ArcaneEffects20261027/FlowRegression";
        static BossEncounter e;static FirstPersonController player;static RecoveryMenu menu;static BossTestInputScope scope;
        static bool running,background;static int step,lastFrame,attackIndex;
        static double started;static float since,clock,recoveryDuration,hold,maxHold,maxSpeed,observeStart,sampledBattleTime;
        static string stillPoseClip;
        static Transform[] bones;static Quaternion[] rotations;static Vector3[] locations;
        static readonly List<string> checks=new List<string>();static string failure;
        static readonly int[] Patterns={0,1,3,4};static readonly int[] Attacks={0,3,4,1};
        [Serializable] class Report {public int step;public string failure,longestStillPoseClip;public string[] checks;public float maximumStillPoseBattleSeconds,maximumBoneDegreesPerBattleSecond;}
        public static string Status=>JsonUtility.ToJson(new Report{step=step,failure=failure,checks=checks.ToArray(),maximumStillPoseBattleSeconds=maxHold,maximumBoneDegreesPerBattleSecond=maxSpeed,longestStillPoseClip=stillPoseClip},true);
        public static string Start()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
            WardenCombatV2Acceptance.Stop();BossCombatPolishAcceptance.Stop();BossLaserGuardAcceptance.Stop();BossAcceptance.Stop();Stop();
            e=UnityEngine.Object.FindFirstObjectByType<BossEncounter>();player=e.GetComponent<FirstPersonController>();menu=e.GetComponent<RecoveryMenu>();
            background=Application.runInBackground;Application.runInBackground=true;scope=new BossTestInputScope();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            running=true;started=EditorApplication.timeSinceStartup;lastFrame=-1;attackIndex=0;maxSpeed=maxHold=hold=0;failure=null;checks.Clear();
            Session(Patterns[0]);Step(40);EditorApplication.update+=Tick;return Status;
        }
        public static void Stop()
        {EditorApplication.update-=Tick;Keys();scope?.Dispose();scope=null;if(running)Application.runInBackground=background;running=false;}
        static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add("PASS: "+text);}
        static void Step(int next){step=next;since=Time.time;Keys();}
        static void Keys(params Key[] keys){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));}
        static void Session(int pattern)
        {
            Keys();if(e.IsActive)Check(e.ReturnToLibrary(),"Return between scenarios");Check(e.Begin(),"Enter current rigged encounter");
            typeof(BossEncounter).GetField("pattern",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(e,pattern);
            hold=0;observeStart=-1;sampledBattleTime=e.BattleTime;
            bones=e.Arena.model.animatedRig.GetComponentsInChildren<Transform>().Where(t=>new[]{"pelvis","spine","chest","head","upper_arm.L","upper_arm.R","forearm.L","forearm.R","hand.L","hand.R","staff"}.Contains(t.name)).ToArray();
            rotations=bones.Select(t=>t.localRotation).ToArray();locations=bones.Select(t=>t.localPosition).ToArray();
        }
        static void Aim(bool orbit=false)
        {
            Vector3 target=orbit?e.Arena.transform.position+Vector3.up*1.1f:e.Arena.boss.position+Vector3.up*2.7f;
            var look=Quaternion.LookRotation(target-e.View.transform.position);player.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);
            float pitch=Mathf.DeltaAngle(0,look.eulerAngles.x);
            typeof(FirstPersonController).GetField("pitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(player,pitch);
            ((Transform)typeof(FirstPersonController).GetField("cameraPivot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player)).localRotation=Quaternion.Euler(pitch,0,0);
            player.SetCursorLocked(true);
        }
        static void Monitor()
        {
            if(!e.IsActive || e.IsPaused || e.IsFinished)return;
            // Editor callbacks and PNG capture do not advance gameplay uniformly. Sample only actual battle updates.
            float dt=e.BattleTime-sampledBattleTime;if(dt<=0)return;sampledBattleTime=e.BattleTime;
            var d=e.Arena.model.animatedRig;
            if(Mathf.Abs(d.BlendWeightSum-1)>.002f)throw new InvalidOperationException("Animation mixer weights lost normalization");
            float amount=0;
            for(int i=0;i<bones.Length;i++)
            {
                // Quaternion.Angle rounds tiny breathing rotations to zero; atan2 preserves that motion.
                var delta=Quaternion.Inverse(rotations[i])*bones[i].localRotation;
                float angle=2*Mathf.Atan2(new Vector3(delta.x,delta.y,delta.z).magnitude,Mathf.Abs(delta.w))*Mathf.Rad2Deg;
                amount+=angle+Vector3.Distance(locations[i],bones[i].localPosition)*100;
                if(dt>.004f)maxSpeed=Mathf.Max(maxSpeed,angle/dt);
                rotations[i]=bones[i].localRotation;locations[i]=bones[i].localPosition;
            }
            hold=amount<.001f?hold+dt:0;if(hold>maxHold){maxHold=hold;stillPoseClip=d.CurrentClip+" / "+e.CurrentState;}
            if(e.CurrentState==BossEncounter.State.Observe && observeStart<0)observeStart=e.BattleTime;
        }
        static void Capture(string name,Vector3? target=null)
        {
            Directory.CreateDirectory(Output+"/Captures");var cam=e.View;var old=cam.targetTexture;var active=RenderTexture.active;var rotation=cam.transform.rotation;
            var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var tex=new Texture2D(1440,810,TextureFormat.RGB24,false);
            try{if(target.HasValue)cam.transform.LookAt(target.Value);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,810),0,0);tex.Apply();File.WriteAllBytes(Output+"/Captures/"+name+".png",tex.EncodeToPNG());}
            finally{cam.transform.rotation=rotation;cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying){Stop();return;}if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup-started>200)throw new TimeoutException("Flow test timed out at "+step);
                if(e.IsPaused && !e.IsFinished){e.SendMessage("OnApplicationFocus",true);menu.Resume();}
                if(e.IsActive && !e.IsPaused)Aim();Monitor();float t=Time.time-since;
                switch(step)
                {
                    case 40:
                        if(e.CurrentState==BossEncounter.State.Telegraph)
                        {
                            Check(e.CurrentAttack==Attacks[attackIndex],"Selected attack starts: "+Attacks[attackIndex]);
                            Check(e.BattleTime-observeStart<1.6f,"Observation no longer leaves a long dead interval");Step(41);
                        }break;
                    case 41:
                        if((e.CurrentState==BossEncounter.State.Attack && e.AttackElapsed>(e.CurrentAttack==4?.025f:.18f)) || (e.CurrentAttack==4 && e.CurrentState==BossEncounter.State.Recovery && e.LastRushDuration>0))
                        {Capture("action_"+e.CurrentAttack);Step(42);}break;
                    case 42:
                        if(e.CurrentState==BossEncounter.State.Recovery)
                        {clock=e.BattleTime;recoveryDuration=e.RecoveryDuration;Step(43);}break;
                    case 43:
                        if(e.CurrentState==BossEncounter.State.Observe)
                        {
                            Check(e.BattleTime-clock<=recoveryDuration+.15f,"Recovery ends on time without waiting for projectiles: "+Attacks[attackIndex]);
                            attackIndex++;
                            if(attackIndex<Patterns.Length){Session(Patterns[attackIndex]);Step(40);}
                            else{e.DamageBoss(e.Definition.health,false);Step(50);}
                        }break;
                    case 50:
                        // UI creation can stall Editor frames; presentation advances using capped dt.
                        if(e.Arena.model.animatedRig.SampleTime>=3.19f || t>8)
                        {
                            Check(e.CurrentState==BossEncounter.State.Won && e.Arena.model.animatedRig.SampleTime>=3.19f,"Defeat still completes after battle stops");
                            e.Retry();rotations=bones.Select(b=>b.localRotation).ToArray();locations=bones.Select(b=>b.localPosition).ToArray();sampledBattleTime=e.BattleTime;hold=0;Step(51);
                        }break;
                    case 51:
                        if(t>.3f)
                        {
                            Check(e.CurrentState==BossEncounter.State.Intro && e.Phase==1 && e.Combat.Health==e.Definition.playerHealth,"Retry restores health, phase and entrance animation");
                            Check(maxHold<.35f,"No measured live pose freeze longer than 0.35 battle seconds");
                            Check(maxSpeed<1500,"No extreme frame-to-frame skeletal snap in the sampled bones");
                            Check(e.ReturnToLibrary(),"Return to library after repeated sequences");Step(60);
                        }break;
                    case 60:
                        if(t>.3f)
                        {
                            Check(UnityEngine.Object.FindObjectsByType<GroundFractureVisuals>(FindObjectsSortMode.None).Length==0 && UnityEngine.Object.FindObjectsByType<WardenAnimationDriver>(FindObjectsSortMode.None).Length==0,"No rifts or animated Boss survive return");
                            step=100;Write();Stop();
                        }break;
                }
                if(e.CurrentState==BossEncounter.State.Lost)throw new InvalidOperationException("Unexpected defeat at step "+step);
            }
            catch(Exception ex){failure=ex.ToString();step=-1;Write();Stop();if(menu)menu.Open();Debug.LogError("[Warden flow acceptance] "+ex.Message);}
        }
        static void Write(){Directory.CreateDirectory(Output);File.WriteAllText(Output+"/MotionFlowAcceptance.json",Status);Debug.Log("[Warden flow acceptance] "+Status);}
    }
}
