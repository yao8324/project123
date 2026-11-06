using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    // Deliberately simple articulated geometry. Combat owns all damage and timing.
    public sealed class KnightVisuals : MonoBehaviour
    {
        public Transform torso, rightArm, leftArm, halo, swordTip;
        public bool refinedRig;
        public Transform rightForearm,leftForearm,rightHand,leftHand,weapon,visualRoot;
        public Transform leftThigh,rightThigh,leftShin,rightShin;
        public Transform[] sealRibbons;
        public GameObject wardShield;
        public KnightRefinedEffects refinedEffects;
        public KnightRigConstraints productionRig;
        public KnightOathPresentation oathPresentation;
        Vector3 lastPosition;
        float lastClock,changeAt;
        float gaitPhase,gaitWeight;
        KnightMove lastMove;
        BossEncounter.State lastState;
        Quaternion oldTorso,oldRight,oldLeft;
        Quaternion weaponWorld;
        Quaternion recoveryWeapon;
        float ribbonTurn;
        Vector3 torsoBase,visualBase;
        Vector3 motionLean;
        bool initialized;
        public void Present(BossEncounter encounter)
        {
            if(refinedRig){PresentRefined(encounter);return;}
            if(!initialized){torsoBase=torso.localPosition;initialized=true;}
            float age=encounter.AttackElapsed;
            var state=encounter.CurrentState;
            var move=encounter.KnightCurrentMove;
            Vector3 right=new Vector3(-15,0,-12), left=new Vector3(-20,0,12);
            float lean=0, turn=0;
            if(state==BossEncounter.State.Telegraph)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.35f));
                if(move==KnightMove.OathCombo){right=Vector3.Lerp(right,new Vector3(-90,-25,-105),p);turn=-15*p;}
                if(move==KnightMove.SealThrust){right=Vector3.Lerp(right,new Vector3(-80,0,-12),p);lean=-8*p;}
                if(move==KnightMove.GateCleave)right=Vector3.Lerp(right,new Vector3(160,0,-15),p);
                if(move==KnightMove.SealBolts)right=Vector3.Lerp(right,new Vector3(-100,0,-15),p);
                if(move==KnightMove.Bastion || move==KnightMove.ShieldBash)left=new Vector3(-80,0,25);
            }
            if(state==BossEncounter.State.Attack)
            {
                if(move==KnightMove.OathCombo)
                {
                    float strike=age<.65f?Mathf.Clamp01((age-.06f)/.25f):age<1.45f?1-Mathf.Clamp01((age-.80f)/.26f):Mathf.Clamp01((age-1.56f)/.25f);
                    right=new Vector3(-90+strike*65,0,Mathf.Lerp(-105,75,strike));turn=Mathf.Lerp(-15,20,strike);
                }
                if(move==KnightMove.SealThrust){right=new Vector3(-100,0,-12);lean=12;}
                if(move==KnightMove.GateCleave)right=Vector3.Lerp(new Vector3(160,0,-15),new Vector3(-35,0,-15),Mathf.Clamp01(age/.22f));
                if(move==KnightMove.SealBolts)right=new Vector3(-100,0,-15);
                if(move==KnightMove.Bastion || move==KnightMove.ShieldBash)left=new Vector3(-95,0,25);
            }
            if(state==BossEncounter.State.Stagger){lean=25;right=new Vector3(15,0,-30);left=new Vector3(10,0,35);}
            if(state==BossEncounter.State.Won){lean=40;right=new Vector3(10,0,-15);}
            // Absolute battle-time poses freeze exactly on pause and during temporal rewards.
            torso.localPosition=torsoBase+Vector3.up*(Mathf.Sin(encounter.BattleTime*2)*.015f);
            torso.localRotation=Quaternion.Euler(lean,turn,0);
            rightArm.localRotation=Quaternion.Euler(right);leftArm.localRotation=Quaternion.Euler(left);
            halo.localRotation=Quaternion.Euler(0,0,encounter.Phase==2?15:0);
            halo.localScale=Vector3.one*(encounter.Phase==2?1.1f:1);
        }

        public void Release(BossEncounter encounter,int index){if(refinedEffects)refinedEffects.Release(encounter,index);}
        public void CancelEffects(){if(refinedEffects)refinedEffects.Clear();}
        static float Sample(float age,float[] times,float[] values)
        {
            // Hermite slopes continue through intermediate poses and stop only at reversals.
            for(int i=1;i<times.Length;i++)if(age<times[i])
            {
                int a=i-1;float span=times[i]-times[a],t=Mathf.Clamp01((age-times[a])/span);
                float slope=(values[i]-values[a])/span;
                float before=a>0?(values[a]-values[a-1])/(times[a]-times[a-1]):0;
                float after=i+1<times.Length?(values[i+1]-values[i])/(times[i+1]-times[i]):0;
                float m0=before*slope>0?Mathf.Sign(slope)*Mathf.Min(Mathf.Abs((before+slope)*.5f),3*Mathf.Abs(slope)):0;
                float m1=after*slope>0?Mathf.Sign(slope)*Mathf.Min(Mathf.Abs((after+slope)*.5f),3*Mathf.Abs(slope)):0;
                return (2*t*t*t-3*t*t+1)*values[a]+(t*t*t-2*t*t+t)*m0*span+(-2*t*t*t+3*t*t)*values[i]+(t*t*t-t*t)*m1*span;
            }
            return values[values.Length-1];
        }
        void PresentRefined(BossEncounter encounter)
        {
            if(!initialized){torsoBase=torso.localPosition;visualBase=visualRoot.localPosition;lastPosition=transform.position;lastClock=encounter.BattleTime;lastState=encounter.CurrentState;oldTorso=torso.localRotation;oldRight=rightArm.localRotation;oldLeft=leftArm.localRotation;weaponWorld=weapon.rotation;initialized=true;}
            float clock=encounter.BattleTime,age=encounter.AttackElapsed;
            var state=encounter.CurrentState;var move=encounter.KnightCurrentMove;
            float dt=Mathf.Max(0,clock-lastClock),follow=1-Mathf.Exp(-22*dt);
            if(state!=lastState || move!=lastMove){changeAt=clock;oldTorso=torso.localRotation;oldRight=rightArm.localRotation;oldLeft=leftArm.localRotation;recoveryWeapon=Quaternion.Inverse(transform.rotation)*weaponWorld;lastState=state;lastMove=move;}
            float transition=state==BossEncounter.State.Recovery?.55f:state==BossEncounter.State.Telegraph?.32f:.14f;
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((clock-changeAt)/transition));
            Vector3 right=new Vector3(-12,0,-12),left=new Vector3(-8,0,18),axis=new Vector3(-.16f,.96f,.22f);
            float lean=0,twist=0,spin=0,elbow=28;
            bool windup=state==BossEncounter.State.Telegraph,active=state==BossEncounter.State.Attack;
            if(windup)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.35f));
                right=Vector3.Lerp(right,new Vector3(-54,-8,-40),p);twist=-18*p;elbow=65;
                if(move==KnightMove.OathCombo || move==KnightMove.OrbitSweep){axis=new Vector3(.94f,.16f,-.30f);twist=20*p;}
                if(move==KnightMove.SealThrust){axis=new Vector3(.08f,.10f,1);lean=-9;right=new Vector3(-52,-8,-18);}
                if(move==KnightMove.SealBolts){axis=new Vector3(-.3f,.92f,.1f);left=new Vector3(-95,0,20);}
                if(move==KnightMove.GateCleave || move==KnightMove.Heavenfall){axis=Vector3.up;right=new Vector3(160,0,-18);left=new Vector3(-65,0,35);}
                if(move==KnightMove.LeapCleave){axis=new Vector3(0,.96f,-.28f);right=new Vector3(145,0,-18);left=new Vector3(-65,0,28);lean=12*p;elbow=75;}
                if(move==KnightMove.BlinkChase){axis=new Vector3(.92f,.2f,-.3f);right=new Vector3(-65,-12,-38);lean=9;twist=24;}
                if(move==KnightMove.BlinkBarrage){axis=new Vector3(-.22f,.95f,.12f);left=new Vector3(-95,0,30);twist=-12;}
            }
            if(active)
            {
                if(move==KnightMove.OathCombo)
                {
                    float yaw=Sample(age,new[]{0f,.18f,.43f,.64f,.95f,1.25f,1.46f,1.72f,2.03f},new[]{104f,0,-100,-105,0,100,25,0,-20});
                    axis=Quaternion.AngleAxis(yaw,Vector3.up)*Vector3.forward;
                    axis.y=age>1.46f?Mathf.Lerp(1,-.35f,Mathf.Clamp01((age-1.46f)/.32f)):.08f;
                    twist=yaw*.20f;right=new Vector3(-72,0,Mathf.Lerp(-35,25,(yaw+105)/210));left=new Vector3(-35,0,45);elbow=15;
                }
                if(move==KnightMove.OrbitSweep){spin=-Mathf.SmoothStep(0,1,Mathf.Clamp01(age/1.35f))*360;float bladeYaw=Sample(age,new[]{0f,.24f,.65f,1.06f,1.4f},new[]{95f,0,-180,-360,-395});axis=Quaternion.AngleAxis(bladeYaw,Vector3.up)*Vector3.forward;axis.y=.08f;right=new Vector3(-80,0,-18);left=new Vector3(-35,0,65);lean=8;}
                if(move==KnightMove.SealThrust){float drive=Sample(age,new[]{0f,.10f,.44f,.65f},new[]{0f,0,1,.7f});axis=new Vector3(0,.04f,1);right=Vector3.Lerp(new Vector3(-52,-8,-18),new Vector3(-95,0,-12),drive);left=new Vector3(-65,0,25);lean=Mathf.Lerp(-9,14,drive);elbow=Mathf.Lerp(65,5,drive);}
                if(move==KnightMove.SealBolts){float pulse=0;foreach(float release in encounter.KnightCurrentSpec.impacts)pulse+=Mathf.Exp(-Mathf.Pow((age-release)/.075f,2));axis=new Vector3(-.15f,.98f,.12f);left=new Vector3(-100+pulse*12,0,18);twist=-pulse*5;}
                if(move==KnightMove.GateCleave){float cut=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.15f));axis=Vector3.Lerp(Vector3.up,new Vector3(0,-.45f,1),cut);right=Vector3.Lerp(new Vector3(160,0,-18),new Vector3(-75,0,-15),cut);lean=12*cut;elbow=Mathf.Lerp(65,12,cut);}
                if(move==KnightMove.Heavenfall){axis=new Vector3(.12f,1,0);right=new Vector3(160,0,-15);left=new Vector3(-70,0,45);lean=-10;}
                if(move==KnightMove.LeapCleave)
                {
                    float cut=Sample(age,new[]{0f,.32f,.5f,.72f,.88f,1.08f},new[]{0f,0,.1f,1,1,.85f});
                    axis=Vector3.Lerp(new Vector3(0,.96f,-.28f),new Vector3(0,-.6f,1),cut);
                    right=Vector3.Lerp(new Vector3(145,0,-18),new Vector3(-72,0,-15),cut);left=new Vector3(-68,0,25);
                    lean=Mathf.Lerp(-12,28,cut);elbow=Mathf.Lerp(75,12,cut);
                }
                if(move==KnightMove.BlinkChase)
                {
                    float yaw=Sample(age,new[]{0f,.08f,.25f,.45f,.66f},new[]{100f,94,0,-100,-110});
                    axis=Quaternion.AngleAxis(yaw,Vector3.up)*Vector3.forward;axis.y=.12f;
                    right=new Vector3(-78,0,Mathf.Lerp(-38,28,(100-yaw)/210));left=new Vector3(-45,0,35);twist=yaw*.22f;lean=8;elbow=18;
                }
                if(move==KnightMove.BlinkBarrage)
                {
                    float pulse=0;foreach(float release in encounter.KnightCurrentSpec.impacts)pulse+=Mathf.Exp(-Mathf.Pow((age-release)/.1f,2));
                    axis=new Vector3(-.22f,.95f,.12f);left=new Vector3(-104+pulse*16,0,30);twist=-12-pulse*6;lean=-pulse*3;
                }
            }
            if(encounter.KnightGuarding || (move==KnightMove.ShieldBash && (windup||active)))left=new Vector3(active?-100:-82,0,12);
            if(move==KnightMove.ShieldBash && active){float push=Sample(age,new[]{0f,.20f,.34f,.50f},new[]{0f,1,.85f,0});left=new Vector3(Mathf.Lerp(-82,-110,push),0,12);lean=push*12;twist=-push*8;}
            if(state==BossEncounter.State.Stagger){lean=28;right=new Vector3(15,0,-35);left=new Vector3(20,0,30);axis=new Vector3(-.75f,.35f,.5f);}
            if(state==BossEncounter.State.Won){lean=32;axis=new Vector3(-.3f,.1f,.9f);}
            Vector3 displacement=transform.position-lastPosition;
            Vector3 velocity=dt>.00001f && displacement.magnitude<1.2f?transform.InverseTransformDirection(displacement/dt):Vector3.zero;
            motionLean=Vector3.Lerp(motionLean,Vector3.ClampMagnitude(velocity,6),1-Mathf.Exp(-8*dt));
            lean+=Mathf.Clamp(motionLean.z*.7f,-4,4);
            torso.localPosition=torsoBase+Vector3.up*Mathf.Sin(clock*2.1f)*.012f;
            torso.localRotation=Quaternion.Slerp(oldTorso,Quaternion.Euler(lean,twist,Mathf.Clamp(-motionLean.x,-5,5)),blend);
            rightArm.localRotation=Quaternion.Slerp(oldRight,Quaternion.Euler(right),blend);
            leftArm.localRotation=Quaternion.Slerp(oldLeft,Quaternion.Euler(left),blend);
            rightForearm.localRotation=Quaternion.Slerp(rightForearm.localRotation,Quaternion.Euler(-elbow,0,0),follow);
            leftForearm.localRotation=Quaternion.Slerp(leftForearm.localRotation,Quaternion.Euler(-35,0,0),follow);
            visualRoot.localRotation=Quaternion.Slerp(visualRoot.localRotation,Quaternion.Euler(0,spin,0),follow);
            float compression=0;
            if(move==KnightMove.LeapCleave && windup)compression=.25f*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.55f));
            if(move==KnightMove.LeapCleave && active && age>=.7f)compression=.3f*Mathf.Exp(-Mathf.Pow((age-.77f)/.16f,2));
            visualRoot.localPosition=Vector3.Lerp(visualRoot.localPosition,visualBase-Vector3.up*compression,follow);
            Quaternion weaponTarget=Quaternion.FromToRotation(Vector3.up,axis.normalized);
            if(state==BossEncounter.State.Recovery)weaponTarget=Quaternion.Slerp(recoveryWeapon,weaponTarget,blend);
            float weaponFollow=1-Mathf.Exp(-(active?32:state==BossEncounter.State.Recovery?12:18)*dt);
            weaponWorld=Quaternion.RotateTowards(weaponWorld,Quaternion.Slerp(weaponWorld,transform.rotation*weaponTarget,weaponFollow),1080*dt);
            weapon.rotation=weaponWorld;
            float travelled=Vector3.ProjectOnPlane(transform.position-lastPosition,Vector3.up).magnitude;
            float speed=dt>.00001f?Mathf.Min(5,travelled/dt):0;
            // A teleport is not a stride; do not drag planted feet or create a huge gait step.
            if(travelled>1.2f){travelled=0;speed=0;gaitWeight=0;}
            gaitPhase+=travelled*3.4f;gaitWeight=Mathf.Lerp(gaitWeight,Mathf.Clamp01(speed/2.5f),1-Mathf.Exp(-10*dt));
            float gait=Mathf.Sin(gaitPhase)*gaitWeight;
            Vector3 lt=new Vector3(gait*23,0,-3),rt=new Vector3(-gait*23,0,3),ls=new Vector3(Mathf.Max(0,-gait)*30,0,0),rs=new Vector3(Mathf.Max(0,gait)*30,0,0);
            if((move==KnightMove.Heavenfall || move==KnightMove.LeapCleave) && (windup||active)){float lift=Mathf.Clamp01((transform.position.y-encounter.Arena.transform.position.y)/1.15f);lt=Vector3.Lerp(lt,new Vector3(-32,0,-8),lift);rt=Vector3.Lerp(rt,new Vector3(18,0,5),lift);ls=Vector3.Lerp(ls,new Vector3(48,0,0),lift);rs=Vector3.Lerp(rs,new Vector3(25,0,0),lift);}
            if(move==KnightMove.LeapCleave && windup){float crouch=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.55f));lt.x-=crouch*18;rt.x-=crouch*18;ls.x+=crouch*30;rs.x+=crouch*30;}
            leftThigh.localRotation=Quaternion.Slerp(leftThigh.localRotation,Quaternion.Euler(lt),follow);rightThigh.localRotation=Quaternion.Slerp(rightThigh.localRotation,Quaternion.Euler(rt),follow);
            leftShin.localRotation=Quaternion.Slerp(leftShin.localRotation,Quaternion.Euler(ls),follow);rightShin.localRotation=Quaternion.Slerp(rightShin.localRotation,Quaternion.Euler(rs),follow);
            if(wardShield)wardShield.SetActive(encounter.KnightGuarding || (move==KnightMove.ShieldBash && (windup||active)));
            halo.localRotation=Quaternion.Euler(0,0,encounter.Phase==2?Mathf.Sin(clock*.5f)*5:0);
            halo.localScale=Vector3.one*(encounter.Phase==2?1.09f:1);
            ribbonTurn=Mathf.Lerp(ribbonTurn,active && move==KnightMove.OrbitSweep?22:0,1-Mathf.Exp(-5*dt));
            if(sealRibbons!=null)for(int i=0;i<sealRibbons.Length;i++)if(sealRibbons[i]){var target=Quaternion.Euler(Mathf.Sin(clock*2+i)*3+gaitWeight*6,Mathf.Sin(clock*1.6f+i)*4+ribbonTurn,Mathf.Sin(clock*1.3f+i)*2);sealRibbons[i].localRotation=Quaternion.Slerp(sealRibbons[i].localRotation,target,1-Mathf.Exp(-(5+i*.3f)*dt));}
            if(productionRig)productionRig.Apply(encounter);
            if(oathPresentation)oathPresentation.Present(encounter);
            if(refinedEffects)refinedEffects.Present(encounter);
            lastPosition=transform.position;lastClock=clock;
        }

        public static LineRenderer Line(string name,Transform parent,Material material,Vector3[] points,float width=.045f,bool loop=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
            line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;
            line.loop=loop;line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
    }
}
