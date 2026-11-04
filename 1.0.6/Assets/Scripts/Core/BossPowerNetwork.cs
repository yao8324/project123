using System;
using UnityEngine;
using ArcaneArchive.Visuals;
namespace ArcaneArchive.Core
{
    [DisallowMultipleComponent]
    public sealed class BossPowerNetwork : MonoBehaviour
    {
        [Min(1)] public float chainHealth=60, crystalHealth=80;
        [Range(0,0.02f)] public float regenFractionPerCrystal=.0015f;
        [Min(.1f)] public float staggerDuration=2;
        public CrystalArenaVisuals visuals;
        public CrystalCombatTarget[] targets;
        BossEncounter owner;
        public bool PresentationPaused=>owner && (owner.IsPaused || owner.IsFinished);
        readonly float[] chains=new float[3], crystals=new float[3];
        public int LiveCount {get;private set;}=3;
        public float DamageReduction=>LiveCount*.1f;
        public bool AllBrokenTriggered {get;private set;}
        public int AllBrokenCount {get;private set;}
        public float ChainRemaining(int i)=>i>=0&&i<3?chains[i]:0;
        public float CrystalRemaining(int i)=>i>=0&&i<3?crystals[i]:0;
        public bool IsBroken(int i)=>i<0||i>=3||crystals[i]<=0;
        public void Initialize(BossEncounter encounter){owner=encounter;ResetNetwork();}
        public void ResetNetwork()
        {
            LiveCount=3;AllBrokenTriggered=false;AllBrokenCount=0;
            for(int i=0;i<3;i++){chains[i]=Mathf.Max(1,chainHealth);crystals[i]=Mathf.Max(1,crystalHealth);}
            if(visuals)visuals.ResetVisuals();
            if(targets!=null)foreach(var target in targets)if(target)target.gameObject.SetActive(true);
        }
        public float Regeneration(float maxHealth,float deltaTime)
        {return owner && owner.CanFight?Mathf.Max(0,maxHealth)*regenFractionPerCrystal*LiveCount*Mathf.Max(0,deltaTime):0;}
        public bool Hit(int i,float damage)
        {
            if(!owner || !owner.CanFight || IsBroken(i) || float.IsNaN(damage) || float.IsInfinity(damage) || damage<=0)return false;
            if(chains[i]>0)
            {
                visuals.Pulse(i);chains[i]=Mathf.Max(0,chains[i]-damage);
                if(chains[i]<=0){if(owner.SelectedAudio)owner.SelectedAudio.BreakChain(i);visuals.SetChainBroken(i);owner.Feedback("锁链已断 · 继续击碎水晶",1.6f);}else if(owner.SelectedAudio)owner.SelectedAudio.HitChain();
            }
            else
            {
                visuals.Pulse(i);crystals[i]=Mathf.Max(0,crystals[i]-damage);
                if(crystals[i]<=0)
                {
                    LiveCount--;if(owner.SelectedAudio)owner.SelectedAudio.BreakCrystal(i);visuals.SetCrystalBroken(i);if(targets!=null && i<targets.Length && targets[i])targets[i].gameObject.SetActive(false);
                    owner.Feedback("水晶破碎 · 供能剩余 "+LiveCount+" / 3",1.8f);
                    if(LiveCount==0 && !AllBrokenTriggered){AllBrokenTriggered=true;AllBrokenCount++;owner.OnCrystalPowerExhausted();}
                }
            }
            return true;
        }
    }
}
