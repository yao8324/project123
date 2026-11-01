using ArcaneArchive.Core;
using ArcaneArchive.UI;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Player
{
    // Left sword / right shield. Pose keys are timed to the gameplay contact frame.
    public sealed class SwordShieldView : MonoBehaviour
    {
        public Transform swordPivot, shieldPivot, bladeBase, bladeTip;
        public PlayerMelee melee;
        public Camera presentationCamera, viewCamera;
        FirstPersonController movement;
        PlayerCombat combat;
        RecoveryMenu menu;
        BossEncounter encounter;
        Renderer[] renderers;
        BladeRibbon ribbon;
        float bobClock,guardWeight,recoil,wallRetraction;
        Vector3 swordPosition,shieldPosition,shieldAngles;
        Quaternion swordRotation;
        const int PoseCount=7;
        readonly Vector3[] positions=new Vector3[PoseCount];
        readonly Quaternion[] rotations=new Quaternion[PoseCount];
        readonly float[] keyTimes=new float[PoseCount];
        Vector3 entryShield,entryShieldAngles;
        Quaternion lastLook;
        Vector2 lookLag;
        sealed class Arm
        {
            public Transform root,elbow;
            public Vector3 wrist,bindDirection;
            public float side,bindLength;
        }
        Arm[] arms;
        static readonly Vector3 SwordRest=new Vector3(-.32f,-.34f,.50f),ShieldRest=new Vector3(.37f,-.35f,.44f);
        // The imported blade runs along grip +Y; its cutting edges are +/-X, NOT its broad +Z face.
        // Aim the edge forward, then tilt the long axis inward. Rotate the grip and fingers together.
        static readonly Quaternion SwordRestRotation=BladeRotation(new Vector3(.18f,.95f,.26f),new Vector3(.12f,-.28f,.96f));
        static readonly Vector3 ShieldRestAngles=new Vector3(10,-16,10);
        static readonly Vector3 ShieldGuard=new Vector3(.24f,-.25f,.40f),ShieldGuardAngles=new Vector3(12,-8,8);
        void Start()
        {
            if(!melee)melee=GetComponentInParent<PlayerMelee>();
            movement=melee.GetComponent<FirstPersonController>();combat=melee.GetComponent<PlayerCombat>();
            menu=melee.GetComponent<RecoveryMenu>();encounter=melee.GetComponent<BossEncounter>();
            var go=new GameObject("Left sword blade light");go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            ribbon=go.AddComponent<BladeRibbon>();ribbon.Initialize(melee.loadout.goldEffect);
            renderers=GetComponentsInChildren<Renderer>(true);BindArms();ResetPose();lastLook=viewCamera.transform.rotation;
        }
        void BindArms()
        {
            var found=new System.Collections.Generic.List<Arm>();
            foreach(var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // The overlay camera must use the current wrist/elbow matrices, not the base camera's cache.
                skin.forceMatrixRecalculationPerRender=true;
                Transform elbow=skin.transform.Find("Elbow");if(!elbow)continue;
                float side=skin.name.Contains("Left")?-1:1;
                Vector3 wrist=new Vector3(side*.051f,-.091f,-.042f);
                found.Add(new Arm{root=skin.transform,elbow=elbow,wrist=wrist,bindDirection=(wrist-elbow.localPosition).normalized,bindLength=(wrist-elbow.localPosition).magnitude,side=side});
            }
            arms=found.ToArray();
        }
        public void ResetPose()
        {
            swordPosition=SwordRest;shieldPosition=ShieldRest;swordRotation=SwordRestRotation;shieldAngles=ShieldRestAngles;
            guardWeight=recoil=wallRetraction=0;lookLag=Vector2.zero;
            if(ribbon)ribbon.ResetTrail();ApplyPose();
        }
        public void BeginSwing()
        {
            if(ribbon){ribbon.ResetTrail();ribbon.Configure(melee.Action);}
            positions[0]=swordPosition;rotations[0]=swordRotation;
            entryShield=shieldPosition;entryShieldAngles=shieldAngles;
            BuildKeys(melee.Action,melee.Combo);
        }
        public void EndTrail(){if(ribbon)ribbon.ResetTrail();}
        public void Interrupt(){recoil=.65f;EndTrail();}
        public void Impact(bool parry)
        {
            recoil=parry?1:.30f;
            if(parry && melee && melee.loadout)MeleeEffects.Burst(transform,shieldPivot.position+viewCamera.transform.forward*.14f,-viewCamera.transform.forward,melee.loadout.goldEffect,encounter,true);
        }
        void ApplyPose()
        {
            if(swordPivot)swordPivot.SetLocalPositionAndRotation(swordPosition,swordRotation);
            if(shieldPivot)shieldPivot.SetLocalPositionAndRotation(shieldPosition,Quaternion.Euler(shieldAngles));
            if(arms!=null && viewCamera)foreach(var arm in arms)
            {
                // The grip follows the blade; the sleeve stays connected below the screen.
                Vector3 wrist=arm.root.TransformPoint(arm.wrist);
                Vector3 elbow=viewCamera.transform.TransformPoint(new Vector3(arm.side*.35f,-.44f,.10f));
                Quaternion rotation=Quaternion.FromToRotation(viewCamera.transform.rotation*arm.bindDirection,wrist-elbow)*viewCamera.transform.rotation;
                arm.elbow.SetPositionAndRotation(elbow,rotation);
                arm.elbow.localScale=Vector3.one*(Vector3.Distance(wrist,elbow)/(arm.bindLength*arm.root.lossyScale.x));
            }
        }
        void LateUpdate()
        {
            if(!movement || !melee.loadout)return;
            bool visible=(!menu || !menu.IsOpen) && (!encounter || !encounter.IsFinished);
            foreach(var renderer in renderers)if(renderer)renderer.enabled=visible;
            if(!visible || movement.SimulationPaused){lastLook=viewCamera.transform.rotation;return;}
            float dt=Mathf.Min(movement.PlayerDeltaTime,.05f);bobClock+=dt*(movement.IsMoving?8.5f:1.7f);recoil=Mathf.Max(0,recoil-dt*5);
            if(presentationCamera && viewCamera){presentationCamera.fieldOfView=viewCamera.fieldOfView;presentationCamera.aspect=viewCamera.aspect;}
            Quaternion delta=Quaternion.Inverse(lastLook)*viewCamera.transform.rotation;lastLook=viewCamera.transform.rotation;
            Vector2 targetLag=new Vector2(Mathf.DeltaAngle(0,delta.eulerAngles.y),Mathf.DeltaAngle(0,delta.eulerAngles.x));
            lookLag=Vector2.Lerp(lookLag,Vector2.ClampMagnitude(targetLag,4),1-Mathf.Exp(-12*dt));
            guardWeight=Mathf.Lerp(guardWeight,(combat.Guarding || melee.PracticeGuarding)?1:0,1-Mathf.Exp(-23*dt));
            float movementWeight=Mathf.Clamp01(movement.PlanarSpeed/4)*(melee.Busy?.24f:1);
            Vector3 sway=new Vector3(Mathf.Sin(bobClock)*.008f-lookLag.x*.0025f,Mathf.Cos(bobClock*2)*.005f+lookLag.y*.002f,0)*movementWeight;
            sway.y+=Mathf.Sin(bobClock*.7f)*.002f;
            Vector3 p=SwordRest,s=ShieldRest,sr=ShieldRestAngles;
            Quaternion r=SwordRestRotation;
            if(melee.Busy)
            {
                SampleKeys(melee.ActionAge,out p,out r);
                SupportPose(melee.ActionAge,ref s,ref sr);
            }
            else if(movement.IsSprinting)
            {p+=new Vector3(-.05f,-.12f,-.03f);r=Quaternion.Euler(22,-8,-14)*r;s+=new Vector3(.02f,-.08f,-.04f);sr.z+=12;}
            float dash=movement.Dash ? movement.Dash.MotionWeight : 0;
            p+=new Vector3(-.03f,-.07f,-.055f)*dash;s+=new Vector3(.02f,-.045f,-.025f)*dash;
            r=Quaternion.Euler(9*dash,0,-5*dash)*r;
            s=Vector3.Lerp(s,ShieldGuard,guardWeight);sr=Vector3.Lerp(sr,ShieldGuardAngles,guardWeight);
            p+=new Vector3(-.035f,-.02f,-.06f)*guardWeight;r=Quaternion.Slerp(r,Quaternion.Euler(-12,0,10)*r,guardWeight);
            // Keep the idle blade away from walls without retracting the committed strike.
            float obstruction=0;
            if(!melee.Busy && Physics.Raycast(viewCamera.transform.position,viewCamera.transform.forward,out var wall,.7f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) && !wall.transform.IsChildOf(melee.transform))
                obstruction=(1-wall.distance/.7f)*.09f;
            wallRetraction=Mathf.Lerp(wallRetraction,obstruction,1-Mathf.Exp(-14*dt));
            p.z-=wallRetraction;s.z-=wallRetraction;s.z-=recoil*.055f;p.z-=recoil*.018f;
            float blend=melee.Busy?1:1-Mathf.Exp(-19*dt);
            swordPosition=Vector3.Lerp(swordPosition,p+sway,blend);shieldPosition=Vector3.Lerp(shieldPosition,s+sway*.65f,blend);
            swordRotation=Quaternion.Slerp(swordRotation,r,blend);shieldAngles=Vector3.Lerp(shieldAngles,sr,blend);ApplyPose();
            bool trail=melee.Busy && melee.Action!=2 && melee.ActionAge>melee.ImpactTime*.70f && melee.ActionAge<melee.ImpactTime+.12f;
            if(bladeBase && bladeTip)ribbon.Tick(melee.InHitStop?0:dt,trail,bladeBase.position,bladeTip.position);
        }
        void BuildKeys(int action,int combo)
        {
            // Hand-traced from the user's 06:26.9 and 09:42 sword passes, mirrored for the left hand:
            // side chamber -> loaded wrist -> fast cross-body cut -> follow-through -> low recovery.
            // Contact remains on the existing damage frame. Entry is the previous swing's actual pose.
            float impact=melee.ImpactTime,tail=melee.Duration-impact;
            keyTimes[0]=0;keyTimes[1]=impact*.43f;keyTimes[2]=impact*.74f;keyTimes[3]=impact;
            keyTimes[4]=impact+Mathf.Min(.085f,tail*.28f);keyTimes[5]=impact+tail*.48f;keyTimes[6]=melee.Duration;
            positions[6]=SwordRest;rotations[6]=SwordRestRotation;
            if(action==2)
            {
                for(int i=1;i<6;i++){positions[i]=SwordRest+new Vector3(-.06f,-.035f,-.08f)*Mathf.Sin(i*Mathf.PI/6);rotations[i]=SwordRestRotation;}
            }
            else if(action==3)
            {
                Key(1,new Vector3(-.38f,-.34f,.27f),new Vector3(.2f,.04f,1),Vector3.right);
                Key(2,new Vector3(-.25f,-.24f,.41f),new Vector3(.1f,.02f,1),Vector3.right);
                Key(3,new Vector3(-.035f,-.14f,.91f),Vector3.forward,Vector3.right);
                Key(4,new Vector3(-.02f,-.13f,.96f),Vector3.forward,Vector3.right);
                Key(5,new Vector3(-.23f,-.27f,.53f),new Vector3(.1f,.30f,.9f),Vector3.right);
            }
            else if(action==1)
            {
                Key(1,new Vector3(-.32f,.04f,.31f),new Vector3(-.3f,.8f,-.55f),Vector3.forward);
                Key(2,new Vector3(-.20f,.09f,.40f),new Vector3(-.12f,.94f,.3f),Vector3.forward);
                Key(3,new Vector3(-.04f,-.16f,.77f),new Vector3(.1f,-.08f,1),Vector3.down);
                Key(4,new Vector3(.10f,-.44f,.66f),new Vector3(.2f,-.90f,.35f),Vector3.back);
                Key(5,new Vector3(.06f,-.47f,.45f),new Vector3(.4f,-.8f,.1f),Vector3.back);
            }
            else if(action==0 && combo==1)
            {
                Key(1,new Vector3(.27f,-.36f,.40f),new Vector3(.85f,-.46f,-.12f),Vector3.forward);
                Key(2,new Vector3(.25f,-.29f,.50f),new Vector3(.92f,-.2f,.25f),Vector3.forward);
                Key(3,new Vector3(-.06f,-.16f,.76f),new Vector3(-.10f,.12f,.99f),Vector3.left);
                Key(4,new Vector3(-.43f,-.14f,.55f),new Vector3(-.92f,.24f,.13f),Vector3.back);
                Key(5,new Vector3(-.43f,-.26f,.34f),new Vector3(-.78f,.48f,-.25f),Vector3.back);
            }
            else
            {
                bool diagonal=action==0 && combo==2;
                Key(1,new Vector3(-.46f,diagonal?-.04f:-.23f,.29f),new Vector3(-.8f,.52f,-.23f),Vector3.forward);
                Key(2,new Vector3(-.40f,diagonal?.01f:-.15f,.43f),new Vector3(-.87f,.35f,.20f),Vector3.forward);
                Key(3,new Vector3(-.06f,-.18f,.73f),new Vector3(.12f,diagonal?-.12f:.04f,.99f),new Vector3(1,diagonal?-.65f:-.22f,0));
                Key(4,new Vector3(.28f,-.32f,.59f),new Vector3(.91f,-.36f,.14f),Vector3.back);
                Key(5,new Vector3(.24f,-.39f,.40f),new Vector3(.77f,-.55f,-.18f),Vector3.back);
                if(action==4)
                {
                    positions[1]+=new Vector3(-.04f,-.04f,-.04f);
                    positions[4]+=new Vector3(.09f,.04f,0);
                    Key(5,new Vector3(.35f,-.28f,.34f),new Vector3(.65f,-.3f,-.70f),Vector3.back);
                }
            }
        }
        static Quaternion BladeRotation(Vector3 blade,Vector3 edge)
        {
            blade.Normalize();edge=Vector3.ProjectOnPlane(edge,blade).normalized;
            return Quaternion.LookRotation(Vector3.Cross(edge,blade),blade);
        }
        void Key(int i,Vector3 grip,Vector3 blade,Vector3 edge)
        {
            positions[i]=grip;
            Quaternion pose=BladeRotation(blade,edge),otherEdge=pose*Quaternion.AngleAxis(180,Vector3.up);
            // Both sides of this sword are sharpened. Use the closer cutting edge rather than rolling
            // the wrist by 180 degrees to change slash direction.
            rotations[i]=Quaternion.Angle(rotations[i-1],pose)<=Quaternion.Angle(rotations[i-1],otherEdge)?pose:otherEdge;
        }
        void SupportPose(float time,ref Vector3 p,ref Vector3 r)
        {
            float drive=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/melee.ImpactTime));
            float release=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-melee.ImpactTime)/(melee.Duration-melee.ImpactTime)));
            float weight=drive*(1-release);
            p=Vector3.Lerp(entryShield,ShieldRest,Mathf.Clamp01(time/.16f));
            r=Vector3.Lerp(entryShieldAngles,ShieldRestAngles,Mathf.Clamp01(time/.16f));
            if(melee.Action==2)
            {p=Vector3.Lerp(p,new Vector3(.07f,-.13f,.94f),weight);r=Vector3.Lerp(r,new Vector3(-4,-3,-4),weight);}
            else
            {p+=new Vector3(.035f,-.025f,-.085f)*weight;r+=new Vector3(6,-8,12)*weight;}
        }
        void SampleKeys(float time,out Vector3 p,out Quaternion r)
        {
            int i=0;while(i<PoseCount-2 && time>keyTimes[i+1])i++;
            float span=keyTimes[i+1]-keyTimes[i],t=Mathf.Clamp01((time-keyTimes[i])/span);
            p=Hermite(positions[i],positions[i+1],Tangent(positions,i)*span,Tangent(positions,i+1)*span,t);
            // Spherical interpolation avoids Euler flips when the blade passes across the body.
            r=Quaternion.Slerp(rotations[i],rotations[i+1],i==2 || i==3?t:Mathf.SmoothStep(0,1,t));
        }
        Vector3 Tangent(Vector3[] keys,int i)
        {
            if(i==0 || i==PoseCount-1)return Vector3.zero;
            float weight=i==3?1f:.35f;
            return (keys[i+1]-keys[i-1])/(keyTimes[i+1]-keyTimes[i-1])*weight;
        }
        static Vector3 Hermite(Vector3 a,Vector3 b,Vector3 ta,Vector3 tb,float t)
        {float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*ta+(-2*t3+3*t2)*b+(t3-t2)*tb;}
    }
}
