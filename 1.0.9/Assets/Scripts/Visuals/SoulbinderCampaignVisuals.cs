using System.Linq;
using ArcaneArchive.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ArcaneArchive.Visuals
{
    public sealed class SoulbinderCampaignVisuals : MonoBehaviour
    {
        public Animator actor;
        public AnimationClip idle, invocation;
        BossEncounter encounter;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable rest, cast;
        float clock, weight;
        Vector3 origin;
        void Start()
        {
            encounter=FindFirstObjectByType<BossEncounter>();origin=actor.transform.localPosition;
            if(!actor || !idle || !invocation)return;
            graph=PlayableGraph.Create("Soulbinder local actions");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,2);rest=AnimationClipPlayable.Create(graph,idle);cast=AnimationClipPlayable.Create(graph,invocation);
            graph.Connect(rest,0,mixer,0);graph.Connect(cast,0,mixer,1);
            AnimationPlayableOutput.Create(graph,"Soulbinder",actor).SetSourcePlayable(mixer);graph.Play();
        }
        void LateUpdate()
        {
            if(!encounter || !encounter.IsActive || (encounter.IsPaused && !encounter.IsFinished) || !graph.IsValid())return;
            float dt=Time.deltaTime;clock+=dt;
            bool casting=encounter.CurrentState==BossEncounter.State.Telegraph || encounter.CurrentState==BossEncounter.State.Attack;
            weight=Mathf.MoveTowards(weight,casting?1:0,dt*5);
            rest.SetTime(Mathf.Repeat(clock,Mathf.Max(.1f,idle.length)));cast.SetTime(Mathf.Repeat(clock,Mathf.Max(.1f,invocation.length)));
            mixer.SetInputWeight(0,1-weight);mixer.SetInputWeight(1,weight);graph.Evaluate(0);
            actor.transform.localPosition=origin+Vector3.up*(Mathf.Sin(clock*1.6f)*.035f);
            if(encounter.CurrentState==BossEncounter.State.Won)actor.transform.localRotation=Quaternion.Slerp(actor.transform.localRotation,Quaternion.Euler(-65,0,0),dt*2);
            else actor.transform.localRotation=Quaternion.identity;
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
