using UnityEngine;
using ArcaneArchive.Core;

namespace ArcaneArchive.Visuals
{
    /// <summary>Articulated mesh rig, driven only by encounter time (including pause).</summary>
    public sealed class WardenVisuals : MonoBehaviour
    {
        public Transform floatingBody, mask, leftArm, rightArm, leftWing, rightWing, chestCover;
        public Transform[] crownSegments;
        private float time;
        public void ResetPose(){time=0;floatingBody.localScale=Vector3.one;}
        public void Present(float dt,int phase,int attack,BossEncounter.State state,float elapsed,float warningDuration)
        {
            time+=dt;float breath=Mathf.Sin(time*.85f);
            bool prepare=state==BossEncounter.State.Telegraph,release=state==BossEncounter.State.Attack;
            bool recovery=state==BossEncounter.State.Recovery,dead=state==BossEncounter.State.Won;
            float p=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.01f,warningDuration)));
            Vector3 left=new Vector3(-8,0,16),right=new Vector3(-8,0,-16),body=new Vector3(0,0,breath*1.1f);
            float height=.07f*breath,wing=phase==2?24:5,head=breath*2;
            if(prepare || release)
            {
                if(attack==0)
                {
                    // Draw back then sweep the casting arm; emission starts at 0.22 seconds.
                    float swing=release?Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.42f)):0;
                    right=Vector3.Lerp(new Vector3(25,-45,-48),new Vector3(-105,30,-8),swing);
                    left=new Vector3(-42,-18,24);body.y=release?Mathf.Lerp(-18,12,swing):-18*p;
                    if(prepare)right=Vector3.Lerp(new Vector3(-8,0,-16),right,p);head=8;wing+=release?10:3;
                }
                else if(attack==3)
                {
                    float raise=release?1:p;
                    left=Vector3.Lerp(left,new Vector3(release?-98:-128,-24,34),raise);
                    right=Vector3.Lerp(right,new Vector3(release?-98:-128,24,-34),raise);
                    body.x=-7*raise;height+=.12f*raise;head=10*raise;wing+=12*raise;
                }
                else if(attack==2)
                {
                    float raise=release?1:p;
                    left=Vector3.Lerp(new Vector3(-18,-60,32),new Vector3(-138,-35,24),raise);
                    right=Vector3.Lerp(new Vector3(-18,60,-32),new Vector3(-138,35,-24),raise);
                    height+=Mathf.Lerp(-.14f,.16f,raise);body.x=Mathf.Lerp(12,-6,raise);head=14-raise*10;
                    if(release && elapsed>1){float lower=Mathf.Clamp01((elapsed-1)/.9f);left=Vector3.Lerp(left,new Vector3(-35,0,22),lower);right=Vector3.Lerp(right,new Vector3(-35,0,-22),lower);}
                }
                else if(attack==4)
                {
                    float load=release?1:p;left=Vector3.Lerp(left,new Vector3(-80,-15,5),load);right=Vector3.Lerp(right,new Vector3(15,10,-12),load);
                    body.x=(release?24:18)*load;height-=.16f*load;head=-10;wing=-24;
                }
                else
                {
                    float push=release?Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.35f)):0;
                    left=Vector3.Lerp(new Vector3(-70,-15,65),new Vector3(-15,0,30),push);right=Vector3.Lerp(new Vector3(-70,15,-65),new Vector3(-15,0,-30),push);
                    height-=push*.12f;head=12;
                }
            }
            if(recovery){body.x=attack==4?12:5;left=new Vector3(-22,0,22);right=new Vector3(-22,0,-22);head=10;}
            if(state==BossEncounter.State.PhaseBreak){left=new Vector3(-65,-20,70);right=new Vector3(-65,20,-70);height+=.15f;wing=30;}
            float blend=1-Mathf.Exp(-dt*9);
            floatingBody.localPosition=Vector3.Lerp(floatingBody.localPosition,new Vector3(0,height,0),blend);
            floatingBody.localRotation=Quaternion.Slerp(floatingBody.localRotation,Quaternion.Euler(body),blend);
            leftArm.localRotation=Quaternion.Slerp(leftArm.localRotation,Quaternion.Euler(left),blend);
            rightArm.localRotation=Quaternion.Slerp(rightArm.localRotation,Quaternion.Euler(right),blend);
            leftWing.localRotation=Quaternion.Slerp(leftWing.localRotation,Quaternion.Euler(0,8+breath*3,wing+breath*2),blend);
            rightWing.localRotation=Quaternion.Slerp(rightWing.localRotation,Quaternion.Euler(0,-8-breath*3,-wing-breath*2),blend);
            float scan=state==BossEncounter.State.Observe?Mathf.Sin(time*.8f)*16:0;
            mask.localRotation=Quaternion.Slerp(mask.localRotation,Quaternion.Euler(head,scan,0),blend);
            chestCover.localRotation=Quaternion.Slerp(chestCover.localRotation,Quaternion.Euler(0,recovery||attack==3&&(prepare||release)?-108:-8,0),blend);
            for (int i = 0; i < crownSegments.Length; i++)
            {
                float a = i * (360f / crownSegments.Length);
                crownSegments[i].localRotation = Quaternion.Euler(0, 0, a + (prepare ? 0 : Mathf.Sin(time * .45f + i) * 4));
                crownSegments[i].localPosition = new Vector3(0, 3.9f, -.23f - (phase == 2 ? .10f * Mathf.Sin(time + i) : 0));
            }
            if (dead) floatingBody.localScale = Vector3.Lerp(floatingBody.localScale, new Vector3(.7f, .18f, .7f), dt * 1.6f);
            else floatingBody.localScale = Vector3.one;
        }
    }
}
