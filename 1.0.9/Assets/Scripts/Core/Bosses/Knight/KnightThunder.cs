using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        readonly Vector3[] knightThunderTargets=new Vector3[3];
        readonly bool[] knightThunderLocked=new bool[3];
        GameObject knightShockwave;
        LineRenderer knightShockEdge,knightShockGlow;
        LineRenderer[] knightShockArcs;
        Vector3 knightShockCenter,knightShockPreviousPlayer;
        float knightShockAge,knightShockRadius,knightShockLimit;
        bool knightShockContact;
        const float ShockDelay=.3f,ShockSpeed=12f,ShockJumpHeight=.6f,ShockHalfWidth=.6f;
        public float KnightShockRadius=>knightShockRadius;
        public bool KnightShockActive=>knightShockwave;

        Vector3 KnightThunderGroundTarget()
        {
            Vector3 local=arena.transform.InverseTransformPoint(transform.position);local.y=0;
            return arena.transform.TransformPoint(Vector3.ClampMagnitude(local,arena.radius-knightSpec.reach-.25f));
        }
        void ResetKnightThunderTargets()
        {
            for(int i=0;i<3;i++){knightThunderLocked[i]=false;knightThunderTargets[i]=KnightThunderGroundTarget();}
        }
        void TickKnightThunderTargets()
        {
            if(knightMove!=KnightMove.Heavenfall || (CurrentState!=State.Telegraph && CurrentState!=State.Attack))return;
            float elapsed=CurrentState==State.Telegraph?stateTime:knightSpec.warning+stateTime;
            for(int i=0;i<3;i++)
            {
                if(knightThunderLocked[i])continue;
                knightThunderTargets[i]=KnightThunderGroundTarget();
                if(elapsed>=knightSpec.warning+knightSpec.impacts[i]-.25f)knightThunderLocked[i]=true;
            }
            UpdateKnightWarning();
        }
        void UpdateKnightFallingThunder(int lineIndex)
        {
            int index=Mathf.Clamp(knightImpact,0,2);
            float until=knightSpec.impacts[index]+(CurrentState==State.Telegraph?knightSpec.warning:0)-stateTime;
            float fall=1-Mathf.Clamp01(until/.25f);
            Vector3 target=knightThunderTargets[index];
            var points=new Vector3[13];
            for(int i=0;i<points.Length;i++)
            {
                float t=i/12f;
                points[i]=target+Vector3.up*(Mathf.Lerp(7.5f,0,fall)+t*2.4f)+Vector3.right*(Mathf.Sin(i*7.3f)*.12f*Mathf.Sin(t*Mathf.PI));
            }
            Material material=knightVisual && knightVisual.refinedEffects?knightVisual.refinedEffects.gold:knightDefinition.blockable;
            SetKnightWarningLine(lineIndex,"Descending tracked lightning",material,points,.08f,false);
            knightLines[lineIndex].enabled=knightImpact<3 && until<=.25f;
            if(knightVisual && knightVisual.refinedEffects)
            {
                var palette=knightVisual.refinedEffects;
                SetKnightWarningLine(lineIndex+1,"Descending lightning corona",palette.softGold,points,.2f,false);
                SetKnightWarningLine(lineIndex+2,"Descending ivory filament",palette.ivory?palette.ivory:palette.gold,points,.022f,false);
                knightLines[lineIndex+1].enabled=knightLines[lineIndex+2].enabled=knightLines[lineIndex].enabled;
            }
        }
        void StartKnightShockwave()
        {
            ClearKnightShockwave();
            knightShockCenter=arena.boss.position;knightShockCenter.y=arena.transform.position.y;
            knightShockPreviousPlayer=transform.position;knightShockAge=knightShockRadius=0;knightShockContact=false;
            knightShockLimit=arena.radius+HorizontalDistance(knightShockCenter,arena.transform.position)+1;
            knightShockwave=new GameObject("Jump over golden lightning wave");knightShockwave.transform.SetParent(arena.effectRoot,false);
            Material gold=knightVisual && knightVisual.refinedEffects?knightVisual.refinedEffects.gold:knightDefinition.blockable;
            Material glow=knightVisual && knightVisual.refinedEffects?knightVisual.refinedEffects.softGold:gold;
            knightShockEdge=KnightVisuals.Line("Ground lightning edge",knightShockwave.transform,gold,new Vector3[97],.08f);
            knightShockGlow=KnightVisuals.Line("Ground lightning glow",knightShockwave.transform,glow,new Vector3[97],.2f);
            knightShockArcs=new LineRenderer[12];
            for(int i=0;i<knightShockArcs.Length;i++)
                knightShockArcs[i]=KnightVisuals.Line("Crawling radial current",knightShockwave.transform,gold,new Vector3[5],.032f);
            PresentKnightShockwave();
            Feedback("金雷扩散 · 起跳越过，盾防与地面突进无法避开",2);
        }
        void TickKnightShockwave(float dt)
        {
            if(!knightShockwave)return;
            float oldRadius=knightShockRadius;
            knightShockAge+=dt;knightShockRadius=Mathf.Min(knightShockLimit,Mathf.Max(0,knightShockAge-ShockDelay)*ShockSpeed);
            if(knightShockAge>=ShockDelay && !knightShockContact)
            {
                // Sweep relative radius as well as player movement: a ground dash cannot tunnel through.
                float before=HorizontalDistance(knightShockPreviousPlayer,knightShockCenter)-oldRadius;
                float after=HorizontalDistance(transform.position,knightShockCenter)-knightShockRadius;
                if(Mathf.Min(before,after)<=ShockHalfWidth && Mathf.Max(before,after)>=-ShockHalfWidth)
                {
                    float crossing=Mathf.Abs(before-after)>.0001f?Mathf.Clamp01(before/(before-after)):1;
                    float height=Mathf.Lerp(knightShockPreviousPlayer.y,transform.position.y,crossing)-arena.transform.position.y;
                    knightShockContact=true;
                    if(height<ShockJumpHeight)combat.ReceiveHit(20,knightShockCenter+Vector3.up*.2f,false,-1,false,true);
                }
            }
            if(!knightShockwave)return; // A lethal hit synchronously clears the encounter.
            knightShockPreviousPlayer=transform.position;
            PresentKnightShockwave();
            if(knightShockRadius>=knightShockLimit)ClearKnightShockwave();
        }
        void PresentKnightShockwave()
        {
            var points=new Vector3[97];
            float radius=Mathf.Max(.2f,knightShockRadius);
            for(int i=0;i<points.Length;i++)
            {
                float angle=i*Mathf.PI*2/96;
                float jitter=Mathf.Sin(i*2.3f+knightShockAge*38)*.11f;
                points[i]=knightShockCenter+new Vector3(Mathf.Cos(angle)*(radius+jitter),.12f+Mathf.Abs(Mathf.Sin(i*3.1f+knightShockAge*30))*.22f,Mathf.Sin(angle)*(radius+jitter));
            }
            points[96]=points[0];knightShockEdge.SetPositions(points);knightShockGlow.SetPositions(points);
            for(int i=0;i<knightShockArcs.Length;i++)
            {
                float angle=i*Mathf.PI/6+knightShockAge*.22f;
                Vector3 outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),side=Vector3.Cross(Vector3.up,outward);
                var arc=new Vector3[5];
                for(int n=0;n<5;n++)arc[n]=knightShockCenter+outward*Mathf.Max(0,radius-.45f+n*.18f)+Vector3.up*(.12f+Mathf.Sin(n*Mathf.PI/4)*.24f)+side*Mathf.Sin(n*8+i+knightShockAge*30)*.12f;
                knightShockArcs[i].SetPositions(arc);
            }
        }
        void ClearKnightShockwave()
        {
            if(knightShockwave)Destroy(knightShockwave);
            knightShockwave=null;knightShockEdge=knightShockGlow=null;knightShockRadius=0;
            knightShockArcs=null;
        }
    }
}
