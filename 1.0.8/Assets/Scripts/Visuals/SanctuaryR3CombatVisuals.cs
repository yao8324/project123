using ArcaneArchive.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace ArcaneArchive.Visuals
{
    // Combat owns all movement, hitboxes and timing. These stripped clips only pose the visual rig.
    public sealed class SanctuaryR3CombatVisuals : MonoBehaviour
    {
        public Animator actor;
        public AnimationClip[] clips;
        public int CurrentClip { get; private set; }
        public float SampleTime { get; private set; }
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] nodes;
        float[] weights=new float[3];
        float lastClock;
        bool initialized, overlayApplied;
        Transform spine, swordArm, swordForearm, swordHand, shieldArm, shieldForearm, shieldHand;
        Vector3 bladeAxis;
        Quaternion spineSample, armSample, forearmSample, handSample, shieldArmSample, shieldForearmSample, shieldHandSample;
        Transform Bone(string name)
        {
            foreach(var bone in actor.GetComponentsInChildren<Transform>())if(bone.name==name)return bone;
            throw new System.InvalidOperationException("R3 combat rig is missing bone: "+name);
        }
        void Initialize()
        {
            if(initialized)return;
            spine=Bone("Spine");swordArm=Bone("UpperArm.R");swordForearm=Bone("Forearm.R");swordHand=Bone("Hand.R");shieldArm=Bone("UpperArm.L");shieldForearm=Bone("Forearm.L");shieldHand=Bone("Hand.L");
            // Derive the rigid blade axis in hand-bone space from the imported bind pose.
            // Do not assume that Blender and Unity share a wrist forward axis.
            foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!skin.name.StartsWith("S01_DiamondSectionBlade"))continue;
                int boneIndex=System.Array.IndexOf(skin.bones,swordHand);
                if(boneIndex<0)throw new System.InvalidOperationException("R3 sword is not bound to Hand.R");
                Vector3 tip=Vector3.zero,heel=Vector3.zero;float far=-1,near=float.MaxValue;
                var bind=skin.sharedMesh.bindposes[boneIndex];
                foreach(var vertex in skin.sharedMesh.vertices){var point=bind.MultiplyPoint3x4(vertex);float d=point.sqrMagnitude;if(d>far){far=d;tip=point;}if(d<near){near=d;heel=point;}}
                bladeAxis=(tip-heel).normalized;
                var effects=GetComponent<KnightVisuals>().refinedEffects;
                if(effects)
                {
                    effects.bladeTip=Socket("R3_BladeTip",tip);effects.bladeBase=Socket("R3_BladeBase",heel);
                    if(effects.readyBlades!=null)foreach(var ready in effects.readyBlades)if(ready)
                        foreach(var renderer in ready.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
                }
                break;
            }
            if(bladeAxis.sqrMagnitude<.5f)throw new System.InvalidOperationException("R3 sword blade axis is missing");
            actor.applyRootMotion=false;actor.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create("R3 combat visual only");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,3);nodes=new AnimationClipPlayable[3];
            for(int i=0;i<3;i++){nodes[i]=AnimationClipPlayable.Create(graph,clips[i]);nodes[i].SetSpeed(0);graph.Connect(nodes[i],0,mixer,i);}
            var output=AnimationPlayableOutput.Create(graph,"R3 pose",actor);output.SetSourcePlayable(mixer);graph.Play();initialized=true;
        }
        public void Present(BossEncounter encounter)
        {
            Initialize();
            // Restore the sampled clip pose before evaluating again, including paused
            // frames where the graph may reuse its previous sample.
            if(overlayApplied){spine.localRotation=spineSample;swordArm.localRotation=armSample;swordForearm.localRotation=forearmSample;swordHand.localRotation=handSample;shieldArm.localRotation=shieldArmSample;shieldForearm.localRotation=shieldForearmSample;shieldHand.localRotation=shieldHandSample;overlayApplied=false;}
            float t=encounter.AttackElapsed;var state=encounter.CurrentState;var move=encounter.KnightCurrentMove;
            int index=0;float time=state==BossEncounter.State.Observe?encounter.BattleTime%1.1f:0;
            if(move==KnightMove.SealThrust && (state==BossEncounter.State.Telegraph||state==BossEncounter.State.Attack||state==BossEncounter.State.Recovery)){
                index=1;time=state==BossEncounter.State.Telegraph?Mathf.Clamp(t,0,.18f):state==BossEncounter.State.Attack?.18f+Mathf.Clamp(t,0,.5f):.68f+Mathf.Clamp(t,0,.27f);
            }
            if(move==KnightMove.LeapCleave && (state==BossEncounter.State.Telegraph||state==BossEncounter.State.Attack||state==BossEncounter.State.Recovery)){
                index=2;time=state==BossEncounter.State.Telegraph?Mathf.Clamp(t,0,.2f):state==BossEncounter.State.Attack?.2f+Mathf.Clamp(t,0,.9f):1.1f+Mathf.Clamp(t,0,.5f);
            }
            float dt=Mathf.Max(0,encounter.BattleTime-lastClock);lastClock=encounter.BattleTime;
            if(weights[0]+weights[1]+weights[2]<.01f)weights[index]=1;
            for(int i=0;i<3;i++){weights[i]=Mathf.MoveTowards(weights[i],i==index?1:0,dt/.16f);mixer.SetInputWeight(i,weights[i]);}
            nodes[index].SetTime(time);graph.Evaluate(0);ApplyOathCombo(encounter);ApplyOtherMoves(encounter);CurrentClip=index;SampleTime=time;
        }
        static float Blend(float time,float start,float end)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,time));
        void ApplyOathCombo(BossEncounter encounter)
        {
            var state=encounter.CurrentState;
            if(encounter.KnightCurrentMove!=KnightMove.OathCombo ||
                (state!=BossEncounter.State.Telegraph && state!=BossEncounter.State.Attack && state!=BossEncounter.State.Recovery))return;
            var spec=encounter.KnightCurrentSpec;float age=encounter.AttackElapsed;
            Vector3 windup=new Vector3(.16f,.02f,.26f),first=new Vector3(-.28f,-.14f,.43f),second=new Vector3(.22f,-.17f,.38f);
            Vector3 overhead=new Vector3(-.06f,.30f,.22f),finish=new Vector3(-.10f,-.24f,.44f),arm=windup;
            float twist=-12,weight=1;Vector3 blade=Quaternion.AngleAxis(100,Vector3.up)*Vector3.forward;
            if(state==BossEncounter.State.Telegraph)weight=Blend(age,0,Mathf.Min(.55f,spec.warning));
            else
            {
                float clock=state==BossEncounter.State.Recovery?spec.duration+(encounter.Phase==2?.78f:0):age;
                float impact0=spec.impacts[0],impact1=spec.impacts[1];
                float cut=Blend(clock,Mathf.Max(0,impact0-.12f),impact0+.12f);
                arm=Vector3.Lerp(windup,first,cut);twist=Mathf.Lerp(-12,12,cut);float bladeYaw=Mathf.Lerp(100,-100,cut);
                if(clock>=impact1-.14f){cut=Blend(clock,impact1-.14f,impact1+.12f);arm=Vector3.Lerp(first,second,cut);twist=Mathf.Lerp(12,-12,cut);bladeYaw=Mathf.Lerp(-100,100,cut);}
                blade=Quaternion.AngleAxis(bladeYaw,Vector3.up)*Vector3.forward;
                // Phase-two third impact is 1.72s in ResolveKnightImpacts.
                if(encounter.Phase==2 && clock>1.28f)
                {
                    float lift=Blend(clock,1.28f,1.54f),down=Blend(clock,1.60f,1.84f);
                    arm=Vector3.Lerp(Vector3.Lerp(second,overhead,lift),finish,down);twist=Mathf.Lerp(-12,0,lift);
                    blade=Vector3.Slerp(Vector3.Slerp(blade,Vector3.up,lift),new Vector3(0,-.6f,1).normalized,down);
                }
                if(state==BossEncounter.State.Recovery)weight=1-Blend(age,0,Mathf.Min(.55f,spec.recovery));
            }
            ApplyUpperPose(encounter,arm,blade,twist,weight,false);
        }
        Transform Socket(string name,Vector3 position)
        {
            var socket=new GameObject(name).transform;socket.SetParent(swordHand,false);socket.localPosition=position;return socket;
        }
        void ApplyOtherMoves(BossEncounter encounter)
        {
            var move=encounter.KnightCurrentMove;var state=encounter.CurrentState;
            if(move!=KnightMove.OrbitSweep && move!=KnightMove.BlinkChase && move!=KnightMove.Heavenfall && move!=KnightMove.BlinkBarrage)return;
            if(state!=BossEncounter.State.Telegraph && state!=BossEncounter.State.Attack && state!=BossEncounter.State.Recovery)return;
            var spec=encounter.KnightCurrentSpec;float age=encounter.AttackElapsed;
            float weight=state==BossEncounter.State.Telegraph?Blend(age,0,Mathf.Min(.55f,spec.warning)):state==BossEncounter.State.Recovery?1-Blend(age,0,Mathf.Min(.55f,spec.recovery)):1;
            float clock=state==BossEncounter.State.Telegraph?0:state==BossEncounter.State.Recovery?spec.duration:age;
            Vector3 arm=new Vector3(.16f,.02f,.28f),blade=Vector3.up;float twist=0;
            if(move==KnightMove.OrbitSweep || move==KnightMove.BlinkChase)
            {
                float cut=Blend(clock,Mathf.Max(0,spec.impacts[0]-.12f),spec.impacts[0]+.12f);
                arm=Vector3.Lerp(arm,new Vector3(-.26f,-.14f,.45f),cut);float yaw=Mathf.Lerp(100,-100,cut);twist=Mathf.Lerp(-15,15,cut);
                if(move==KnightMove.OrbitSweep && clock>=spec.impacts[1]-.14f)
                {
                    float back=Blend(clock,spec.impacts[1]-.14f,spec.impacts[1]+.12f);
                    arm=Vector3.Lerp(arm,new Vector3(.23f,-.12f,.42f),back);yaw=Mathf.Lerp(-100,100,back);twist=Mathf.Lerp(15,-15,back);
                }
                blade=Quaternion.AngleAxis(yaw,Vector3.up)*Vector3.forward;
            }
            else if(move==KnightMove.Heavenfall)
            {
                float pulse=0;foreach(float impact in spec.impacts)pulse=Mathf.Max(pulse,1-Mathf.Clamp01(Mathf.Abs(clock-impact)/.12f));
                arm=new Vector3(-.04f,.28f-pulse*.035f,.25f);blade=Vector3.up;twist=-5;
            }
            else {arm=new Vector3(.10f,.10f,.26f);blade=new Vector3(.15f,1,.10f).normalized;twist=-10;}
            ApplyUpperPose(encounter,arm,blade,twist,weight,move==KnightMove.BlinkBarrage);
        }
        void ApplyUpperPose(BossEncounter encounter,Vector3 arm,Vector3 blade,float twist,float weight,bool cast)
        {
            if(weight<=0)return;
            spineSample=spine.localRotation;armSample=swordArm.localRotation;forearmSample=swordForearm.localRotation;handSample=swordHand.localRotation;
            shieldArmSample=shieldArm.localRotation;shieldForearmSample=shieldForearm.localRotation;shieldHandSample=shieldHand.localRotation;
            spine.localRotation=spineSample*Quaternion.Euler(0,twist*weight,0);
            var boss=encounter.Arena.boss;
            Vector3 side=boss.right*(Vector3.Dot(swordArm.position-boss.position,boss.right)>=0?1:-1);
            Vector3 target=swordArm.position+side*arm.x+Vector3.up*arm.y+boss.forward*arm.z;
            SolveArm(swordArm,swordForearm,swordHand,target,side-Vector3.up*.4f);
            swordArm.localRotation=Quaternion.Slerp(armSample,swordArm.localRotation,weight);
            swordForearm.localRotation=Quaternion.Slerp(forearmSample,swordForearm.localRotation,weight);
            Vector3 desired=encounter.Arena.boss.TransformDirection(blade);
            var alignment=Quaternion.FromToRotation(swordHand.TransformDirection(bladeAxis),desired);
            swordHand.rotation=Quaternion.Slerp(Quaternion.identity,alignment,weight)*swordHand.rotation;
            if(cast)
            {
                Vector3 shieldSide=-side;
                Vector3 shieldTarget=shieldArm.position+shieldSide*.04f+boss.forward*.42f-Vector3.up*.10f;
                SolveArm(shieldArm,shieldForearm,shieldHand,shieldTarget,shieldSide-Vector3.up*.5f);
                shieldArm.localRotation=Quaternion.Slerp(shieldArmSample,shieldArm.localRotation,weight);
                shieldForearm.localRotation=Quaternion.Slerp(shieldForearmSample,shieldForearm.localRotation,weight);
            }
            overlayApplied=true;
        }
        static void SolveArm(Transform swordArm,Transform swordForearm,Transform swordHand,Vector3 target,Vector3 elbowHint)
        {
            Vector3 shoulder=swordArm.position;
            float upper=Vector3.Distance(shoulder,swordForearm.position),lower=Vector3.Distance(swordForearm.position,swordHand.position);
            Vector3 axis=(target-shoulder).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(shoulder,target),Mathf.Abs(upper-lower)+.001f,(upper+lower)*.98f);
            target=shoulder+axis*distance;
            Vector3 bend=Vector3.ProjectOnPlane(elbowHint,axis).normalized;
            if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.up,axis).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 elbow=shoulder+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            swordArm.rotation=Quaternion.FromToRotation(swordForearm.position-shoulder,elbow-shoulder)*swordArm.rotation;
            swordForearm.rotation=Quaternion.FromToRotation(swordHand.position-swordForearm.position,target-swordForearm.position)*swordForearm.rotation;
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
