using UnityEngine;
using ArcaneArchive.Core;
using System.Collections.Generic;
namespace ArcaneArchive.Visuals
{
    // Presentation follows the chain/crystal states owned by BossPowerNetwork.
    public sealed class CrystalArenaVisuals : MonoBehaviour
    {
        public Transform bossTarget;
        public Mesh linkFragmentMesh,crystalFragmentMesh;
        public Material ironFragmentMaterial,crystalFragmentMaterial;
        readonly List<GameObject> bursts=new List<GameObject>();
        readonly float[] pulses=new float[3];
        float visualTime;
        public void Pulse(int i){if(i>=0 && i<3)pulses[i]=.25f;}
        void Burst(int i,bool crystal)
        {
            if(!Application.isPlaying)return;
            var go=new GameObject(crystal?"Crystal_Shatter":"Broken_Chain_Links");go.transform.SetParent(transform);go.transform.position=crystals[i].position+Vector3.up*1.7f;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=.2f;main.startLifetime=new ParticleSystem.MinMaxCurve(.6f,1.25f);main.startSpeed=new ParticleSystem.MinMaxCurve(1,2.7f);main.startSize=crystal?.18f:1f;main.gravityModifier=.7f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=24;
            var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.5f;
            var r=go.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=crystal?crystalFragmentMesh:linkFragmentMesh;r.sharedMaterial=crystal?crystalFragmentMaterial:ironFragmentMaterial;
            bursts.Add(go);ps.Play();Destroy(go,1.5f);
        }
        public Transform[] crystals, chains, runes;
        public GameObject[] intactCrystals, brokenCrystals;
        public Light[] crystalLights;
        public LineRenderer[] supply;
        public bool[] chainBroken = new bool[3], crystalBroken = new bool[3];
        public void SetChainBroken(int index)
        {
            if(index<0 || index>=3)return;
            if(chainBroken[index])return;chainBroken[index]=true;Burst(index,false);ApplyState(index);
        }
        public void SetCrystalBroken(int index)
        {
            if(index<0 || index>=3 || !chainBroken[index])return;
            if(crystalBroken[index])return;crystalBroken[index]=true;Burst(index,true);ApplyState(index);
        }
        public void ResetVisuals()
        {
            foreach(var burst in bursts)if(burst)Destroy(burst);bursts.Clear();
            for(int i=0;i<3;i++){pulses[i]=0;chainBroken[i]=false;crystalBroken[i]=false;ApplyState(i);}
        }
        void ApplyState(int i)
        {
            chains[i].gameObject.SetActive(!chainBroken[i]);runes[i].gameObject.SetActive(!chainBroken[i]);
            intactCrystals[i].SetActive(!crystalBroken[i]);brokenCrystals[i].SetActive(crystalBroken[i]);
            crystalLights[i].enabled=!crystalBroken[i];supply[i].enabled=!crystalBroken[i];
        }
        void Update(){var network=GetComponent<BossPowerNetwork>();if(network&&network.PresentationPaused)return;visualTime+=Time.deltaTime;Animate(visualTime);}
        public void Animate(float time)
        {
            if(!bossTarget)return;
            for(int i=0;i<3;i++)
            {
                chains[i].localRotation=Quaternion.Euler(0,time*9+i*120,0);
                runes[i].localRotation=Quaternion.Euler(0,-time*13+i*120,0);
                for(int j=0;j<runes[i].childCount;j++)
                {
                    var glyph=runes[i].GetChild(j);var p=glyph.localPosition;
                    p.y=.65f+Mathf.Repeat(j*.173f+time*.19f,2.4f);glyph.localPosition=p;
                }
                if(crystalBroken[i])continue;
                Vector3 start=crystals[i].position+Vector3.up*1.65f,end=bossTarget.position+Vector3.up*2.1f;
                Vector3 side=Vector3.Cross((end-start).normalized,Vector3.up);
                for(int k=0;k<supply[i].positionCount;k++)
                {
                    float t=k/(float)(supply[i].positionCount-1);
                    var p=Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.65f);
                    p+=side*(Mathf.Sin(t*18-time*2+i)*.08f*Mathf.Sin(t*Mathf.PI));supply[i].SetPosition(k,p);
                }
                pulses[i]=Mathf.Max(0,pulses[i]-Time.deltaTime);
                crystalLights[i].intensity=1.15f+Mathf.Sin(time*1.4f+i)*.12f+pulses[i]*14;
            }
        }
    }
}
