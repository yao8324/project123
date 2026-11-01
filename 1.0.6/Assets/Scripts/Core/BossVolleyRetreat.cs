using System.Collections.Generic;
using ArcaneArchive.Visuals;
using UnityEngine;
namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        readonly List<WardenBlinkVisual> blinkEffects=new List<WardenBlinkVisual>();
        bool retreatMoved;Vector3 retreatOrigin,retreatDirection;
        public float LastRetreatDistance {get;private set;}
        void SpawnBlink(Vector3 position,bool arrival)
        {
            var go=new GameObject(arrival?"Warden arrival seal":"Warden departure seal");go.transform.SetParent(arena.effectRoot,false);
            var v=go.AddComponent<WardenBlinkVisual>();v.Initialize(definition.spellRibbon,definition.spellParticle,position,arena.boss.forward,combatTime,arrival);blinkEffects.Add(v);
        }
        void BeginVolleyRetreat()
        {
            retreatMoved=false;LastRetreatDistance=0;retreatOrigin=arena.boss.position;
            retreatDirection=arena.boss.position-transform.position;retreatDirection.y=0;
            retreatDirection=retreatDirection.sqrMagnitude>.01f?retreatDirection.normalized:-arena.boss.forward;
            SetState(State.Retreat);BossIntent="裂隙后闪";SpawnBlink(retreatOrigin,false);
            Feedback("裂隙后闪 · 禁页即将齐射",definition.volleyWarning+.3f);
        }
        void TickVolleyRetreat()
        {
            if(!retreatMoved && stateTime>=.1f)
            {
                retreatMoved=true;LastRetreatDistance=SweepBoss(retreatDirection,definition.volleyRetreatDistance,false);
                if(rushWake)Destroy(rushWake.gameObject);
                rushWake=Instantiate(arena.rushWakeEffect,arena.effectRoot);rushWake.Configure(.65f,definition.bossScale);
                rushWake.SetPath(retreatOrigin,retreatDirection,LastRetreatDistance,combatTime);rushWake.Finish(combatTime);
                SpawnBlink(arena.boss.position,true);
                lockTarget=transform.position+Vector3.up;
                var facing=transform.position-arena.boss.position;facing.y=0;
                if(facing.sqrMagnitude>.01f)arena.boss.rotation=Quaternion.LookRotation(facing);
            }
            if(stateTime>=.3f){SetState(State.Telegraph);BeginVolleyCrown();PlaySound(definition.warningSound,.35f);}
        }
        void TickBlinkEffects()
        {
            for(int i=blinkEffects.Count-1;i>=0;i--)
                if(!blinkEffects[i] || !blinkEffects[i].Present(combatTime))
                {if(blinkEffects[i])Destroy(blinkEffects[i].gameObject);blinkEffects.RemoveAt(i);}
        }
        void ClearBlinkEffects()
        {
            foreach(var v in blinkEffects)if(v)Destroy(v.gameObject);
            blinkEffects.Clear();retreatMoved=false;
        }
    }
}
