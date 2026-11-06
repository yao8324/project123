using ArcaneArchive.Core;
using ArcaneArchive.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    // Cast-space ribbons, engraved sigils and actual world-space particles share one paused clock.
    // One bounded burst per skill; no automatic emitters or changes to hit detection.
    public sealed class MeleeSkillVfx : MonoBehaviour
    {
        BossEncounter encounter;
        RecoveryMenu menu;
        LineRenderer[] ribbons;
        ParticleSystem motes, sparks;
        Material ribbonMaterial;
        Vector3 origin;
        Quaternion basis;
        Color accent,secondary;
        int skill;
        bool gathering,startedInCombat;
        float age,lifetime;
        public float Age => age;
        public int ParticleCount => (motes?motes.particleCount:0)+(sparks?sparks.particleCount:0);
        static readonly AnimationCurve Taper=new AnimationCurve(new Keyframe(0,0),new Keyframe(.12f,.65f),new Keyframe(.40f,1),new Keyframe(.78f,.48f),new Keyframe(1,0));

        public static void Spawn(Transform parent,Vector3 point,Quaternion facing,int number,MeleeLoadout loadout,BossEncounter owner,bool cast)
        {
            if(!parent || !loadout || number<1 || number>4)return;
            var go=new GameObject(cast?"Gathering weapon constellation":"Ornate melee release "+number);
            go.transform.SetParent(parent,false);go.transform.position=point;go.layer=parent.gameObject.layer;
            var fx=go.AddComponent<MeleeSkillVfx>();fx.origin=point;fx.basis=facing;fx.skill=number;fx.gathering=cast;
            fx.encounter=owner;fx.startedInCombat=owner && owner.IsActive;
            fx.menu=owner?owner.GetComponent<RecoveryMenu>():Object.FindFirstObjectByType<RecoveryMenu>();
            fx.lifetime=cast?loadout.skills[number-1].impactTime:(number==4?.92f:.78f);
            fx.accent=loadout.skills[number-1].iconAccent;
            fx.secondary=number==1?new Color(.72f,.34f,1):number==4?new Color(1,.72f,.29f):new Color(1,.86f,.53f);
            fx.ribbonMaterial=new Material(loadout.goldEffect){name="Skill ribbon neutral HDR"};
            fx.ribbonMaterial.SetColor("_BaseColor",new Color(2.1f,2.1f,2.1f,1));
            fx.ribbons=new LineRenderer[cast?6:18];
            for(int i=0;i<fx.ribbons.Length;i++)
            {
                int count=!cast && i>=8 && i<12?5:65;
                var line=MeleeEffects.CreateLine(i<3?"Layered luminous ribbon":"Engraved light filament",go.transform,fx.ribbonMaterial,.02f,count);
                line.widthCurve=Taper;fx.ribbons[i]=line;
            }
            fx.motes=fx.Particles("Suspended seal dust",loadout.skillParticleEffect,false,cast?40:144);
            fx.sparks=fx.Particles("Directional blade sparks",loadout.skillParticleEffect,true,cast?16:56);
            fx.Emit(cast?32:number==4?132:96,false);
            fx.Emit(cast?10:number==4?44:32,true);
            fx.Draw(0);
        }

        ParticleSystem Particles(string name,Material material,bool streaks,int capacity)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.layer=gameObject.layer;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=1;main.maxParticles=capacity;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=lifetime;
            main.gravityModifier=gathering?0:streaks?.14f:.025f;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(.1f,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.85f,.35f),new GradientAlphaKey(0,1)});color.color=gradient;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.12f,1),new Keyframe(1,0)));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material?material:ribbonMaterial;
            renderer.renderMode=streaks?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
            renderer.lengthScale=streaks?1.8f:1;renderer.velocityScale=streaks?.10f:0;
            renderer.maxParticleSize=.06f;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        void Emit(int count,bool streaks)
        {
            var random=new System.Random(620+skill*37+(streaks?1:0));
            var ps=streaks?sparks:motes;
            for(int i=0;i<count;i++)
            {
                float u=(i+(float)random.NextDouble())/count;
                float phi=i*2.399963f;
                Vector3 p,v;
                if(gathering)
                {
                    Vector3 center=new Vector3(skill==2?.28f:-.30f,-.04f,.85f);
                    Vector3 radial=new Vector3(Mathf.Cos(phi),Mathf.Sin(phi),Mathf.Sin(phi*2)*.2f);
                    p=center+radial*(.18f+.22f*u);v=(center-p)/Mathf.Max(.12f,lifetime)*.85f;
                }
                else
                {
                    p=Curve(i%3,u,0);
                    Vector3 radial=skill==3?new Vector3(Mathf.Cos(phi),Mathf.Sin(phi),2):skill==1?new Vector3(Mathf.Cos(phi)*.65f,Mathf.Sin(phi)*.6f-.25f,.2f):new Vector3(p.x,p.y+.15f,skill==2?.75f:p.z-.4f);
                    v=radial.normalized*(streaks?1.3f+u*2.0f:.18f+u*.55f);
                    p+=new Vector3(Mathf.Cos(phi),Mathf.Sin(phi),0)*.06f;
                }
                Color tint=i%4==0?new Color(1,.96f,.80f):i%3==0?secondary:accent;
                var particle=new ParticleSystem.EmitParams{position=World(p),velocity=basis*v,startColor=tint,
                    startSize=(streaks?.022f:.035f)+(float)random.NextDouble()*(streaks?.022f:.045f),
                    startLifetime=lifetime*(.55f+.4f*(float)random.NextDouble()),rotation=(float)random.NextDouble()*180};
                ps.Emit(particle,1);
            }
            // Simulate(0) populates renderer buffers without starting Unity's autonomous clock.
            ps.Simulate(0,false,false,false);
        }
        void Update(){AdvanceVisual(Mathf.Min(Time.deltaTime,.1f));}
        public void AdvanceVisual(float dt)
        {
            if(encounter && (encounter.IsFinished || (startedInCombat && !encounter.IsActive))){Destroy(gameObject);return;}
            if((encounter && encounter.IsPaused) || (menu && menu.IsOpen))return;
            dt=Mathf.Max(0,dt);age+=dt;
            if(age>=lifetime){Destroy(gameObject);return;}
            if(motes)motes.Simulate(dt,false,false,false);if(sparks)sparks.Simulate(dt,false,false,false);
            Draw(age/lifetime);
        }
        Vector3 World(Vector3 p)=>origin+basis*p;
        Vector3 Circle(Vector3 center,float radius,float angle,Vector3 a,Vector3 b)
        {return center+radius*(a*Mathf.Cos(angle)+b*Mathf.Sin(angle));}

        // Each skill has a different silhouette. Small engraved fragments sit outside the main light band.
        Vector3 Curve(int layer,float u,float t)
        {
            if(gathering)
            {
                float radius=(.22f+layer*.027f)*(1-t*.67f);
                float angle=(u*1.6f+.15f*layer+t*(layer%2==0?.65f:-.45f))*Mathf.PI;
                return Circle(new Vector3(skill==2?.28f:-.30f,-.04f,.85f),radius,angle,Vector3.right,Vector3.up);
            }
            if(layer>=8 && layer<12)
            {
                int n=layer-8;float phase=n*Mathf.PI*.5f+t*.45f;
                Vector3 center=skill==1?new Vector3((n-1.5f)*.28f,-.85f,1.9f):skill==3?new Vector3(Mathf.Cos(phase)*.34f,Mathf.Sin(phase)*.34f,2.45f+t*.3f):skill==2?new Vector3(.16f+Mathf.Cos(phase)*.66f,Mathf.Sin(phase)*.66f,1.4f+t*.35f):new Vector3(Mathf.Cos(phase)*1.9f,-.18f, .5f+Mathf.Sin(phase)*1.9f);
                Vector3 a=skill==4?Vector3.right:Vector3.right,b=skill==4?Vector3.forward:Vector3.up;
                float angle=u*2*Mathf.PI+Mathf.PI*.25f;
                return Circle(center,.075f*(1+t*.4f),angle,a,b);
            }
            if(skill==1)
            {
                if(layer>=12)
                {
                    float groundAngle=(u*1.5f+(layer-12)*.32f)*Mathf.PI;
                    return Circle(new Vector3(0,-1.05f,1.85f),(.42f+(layer-12)*.11f)*(1+t*.8f),groundAngle,Vector3.right,Vector3.forward);
                }
                float angle=Mathf.Lerp(154,-44,u)*Mathf.Deg2Rad-t*.36f;
                Vector3 p=Circle(new Vector3(-.04f,-.05f,1.6f+t*.18f),1.1f+layer*.025f,angle,new Vector3(.12f,.99f,0),new Vector3(0,0,.68f));
                if(layer>=3)p+=new Vector3(Mathf.Sin(u*18+layer)*.024f+(layer-4)*.035f,0,Mathf.Cos(u*15+layer)*.025f);
                return p;
            }
            if(skill==2)
            {
                Vector3 center=new Vector3(.16f,-.02f,1.32f+t*.52f);
                if(layer>=12)
                {
                    // Upper/lower eyelids and the central diamond form the shield's eye seal.
                    float x=Mathf.Lerp(-.36f,.36f,u),sign=layer%2==0?1:-1;
                    float y=layer<16?Mathf.Sin(u*Mathf.PI)*.18f*sign:Mathf.Sin(u*2*Mathf.PI)*.07f;
                    return center+new Vector3(x,y,0);
                }
                float radius=(layer<3?.45f+layer*.015f:.54f+(layer-3)*.055f)*(1+t*.72f);
                float angle=(u*1.77f+layer*.28f+t*(layer%2==0?.6f:-.5f))*Mathf.PI;
                return Circle(center,radius,angle,Vector3.right,Vector3.up);
            }
            if(skill==3)
            {
                if(layer>=12)
                {
                    float tipAngle=(u*1.65f+layer*.33f+t)*Mathf.PI;
                    return Circle(new Vector3(0,0,2.4f+t*.5f),.18f+(layer-12)*.045f,tipAngle,Vector3.right,Vector3.up);
                }
                float z=Mathf.Lerp(.62f,3.15f,u)+t*.35f;
                float radius=(layer<3?.10f:.22f+(layer-3)*.025f)*Mathf.Sin(u*Mathf.PI)*(.8f+t*.4f);
                float angle=u*9.4f+layer*2.399963f-t*2;
                return new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z);
            }
            float phi=Mathf.Lerp(-142,142,u)*Mathf.Deg2Rad+t*.75f+layer*.065f;
            float rad=(layer<3?1.95f:layer>=12?1.55f+(layer-12)*.065f:2.03f+(layer-3)*.035f)*(1+t*.13f);
            return new Vector3(Mathf.Sin(phi)*rad,-.18f+Mathf.Sin(phi*2+layer*.4f)*(layer%2==0?.16f:-.16f),.45f+Mathf.Cos(phi)*rad);
        }
        void Draw(float t)
        {
            float envelope=gathering?Mathf.Sin((.08f+t*.92f)*Mathf.PI):Mathf.Pow(1-t,1.5f)*Mathf.Clamp01((t+.05f)*12);
            for(int i=0;i<ribbons.Length;i++)
            {
                var line=ribbons[i];Color c=i==1?new Color(1,.97f,.83f):i%3==2?secondary:accent;
                c.a=envelope*(i==0?.32f:i==1?.95f:i>=8?.62f:.52f);
                line.startColor=line.endColor=c;
                line.widthMultiplier=gathering?(i==0?.026f:.008f):i==0?.15f:i==1?.025f:i==2?.048f:i<8?.008f:i<12?.012f:.006f;
                for(int p=0;p<line.positionCount;p++)line.SetPosition(p,World(Curve(i,p/(float)(line.positionCount-1),t)));
            }
        }
        void OnDestroy(){if(ribbonMaterial)Destroy(ribbonMaterial);}
    }
}
