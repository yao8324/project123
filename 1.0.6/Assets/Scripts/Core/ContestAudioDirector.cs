using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneArchive.Contest
{
    public sealed class ContestAudioDirector : MonoBehaviour
    {
        public enum Cue { Ui,PaperOpen,PaperClose,Cloth,Hardware,Node,Door,PowerStart,PowerSuccess,Overload,Bundle,Transfer,Impact,Alarm }
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly AudioSource[] ambience=new AudioSource[2];
        readonly float[] weights=new float[2];
        AudioSource effects,ui,power;
        int destination;
        float effectsVolume=.75f,environmentVolume=.65f,powerStrength;
        bool initialized,duck;
        public string CurrentAmbienceKey { get; private set; }

        public void Initialize(float effectsLevel,float environmentLevel)
        {
            if(initialized)return;
            effects=Source("Story effects",false);ui=Source("Interface and paper",false);ui.ignoreListenerPause=true;
            power=Source("Power guidance",true);power.clip=Clip("PowerLoop");
            ambience[0]=Source("Environment A",true);ambience[1]=Source("Environment B",true);
            SetVolumes(effectsLevel,environmentLevel);initialized=true;
            ChangeAmbience("Calm",true);
        }
        AudioSource Source(string name,bool loop)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.volume=0;
            return source;
        }
        AudioClip Clip(string key)
        {
            if(clips.TryGetValue(key,out var clip))return clip;
            clip=Resources.Load<AudioClip>("ContestAudio/"+key);clips[key]=clip;
            if(!clip)Debug.LogWarning("[ContestAudio] Missing bundled clip: "+key);
            return clip;
        }
        public static float SafeVolume(float value,float fallback)
            =>float.IsNaN(value) || float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
        public void SetVolumes(float effectsLevel,float environmentLevel)
        {
            effectsVolume=SafeVolume(effectsLevel,.75f);environmentVolume=SafeVolume(environmentLevel,.65f);
            ApplyVolumes();
        }
        public void Play(Cue cue,bool interfaceSound=false)
        {
            if(!initialized)return;
            var clip=Clip(cue.ToString());if(!clip)return;
            var source=interfaceSound?ui:effects;
            source.clip=clip;source.Play();
        }
        public void SetContext(ContestStoryState state,bool inHall,bool mainMenu,bool powerActive,float strength,bool reading)
        {
            if(!initialized)return;
            string key=mainMenu?"Calm":state.stage>=7?"Ending":state.stage==0?"Calm":state.stage<=2?"Tension":
                inHall?(state.nodes>=3?"Restored":"Maintenance"):state.stage>=6?"Rescue":state.stage>=4?"Investigation":"Tension";
            ChangeAmbience(key,false);duck=reading;powerStrength=Mathf.Clamp01(strength/100);
            if(powerActive && !mainMenu)
            {if(!power.isPlaying && !AudioListener.pause && power.clip)power.Play();}
            else if(power.isPlaying || power.timeSamples>0)power.Stop();
            ApplyVolumes();
        }
        void ChangeAmbience(string key,bool immediate)
        {
            if(CurrentAmbienceKey==key)return;
            var clip=Clip(key);if(!clip)return;
            int index=Array.FindIndex(ambience,s=>s && s.clip==clip);
            if(index<0)
            {
                index=weights[0]<=weights[1]?0:1;
                ambience[index].Stop();ambience[index].volume=0;weights[index]=0;ambience[index].clip=clip;ambience[index].Play();
            }
            destination=index;CurrentAmbienceKey=key;
            if(immediate){weights[index]=1;weights[1-index]=0;}
        }
        void Update()
        {
            if(!initialized)return;
            if(!AudioListener.pause)
                for(int i=0;i<2;i++)weights[i]=Mathf.MoveTowards(weights[i],i==destination?1:0,Time.unscaledDeltaTime/1.2f);
            ApplyVolumes();
        }
        void ApplyVolumes()
        {
            if(effects)effects.volume=effectsVolume*.85f;
            if(ui)ui.volume=effectsVolume*.48f;
            if(power){power.volume=effectsVolume*(.12f+.22f*powerStrength);power.pitch=Mathf.Lerp(.8f,1.2f,powerStrength);}
            for(int i=0;i<2;i++)if(ambience[i])ambience[i].volume=weights[i]*environmentVolume*(duck?.58f:1);
        }
        void OnDisable()
        {
            if(effects)effects.Stop();if(ui)ui.Stop();if(power)power.Stop();
            foreach(var source in ambience)if(source)source.Stop();
        }
    }
}
