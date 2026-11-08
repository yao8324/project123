using UnityEngine;
using UnityEngine.Rendering;
namespace ArcaneArchive.Visuals
{
    // All simulation is advanced by the encounter clock, including particles.
    public static class BattleVfx
    {
        public static readonly Color Violet=new Color(.62f,.25f,1,.8f), Gold=new Color(1,.64f,.22f,.8f), Ice=new Color(.7f,.87f,1,.9f);
        public static LineRenderer Line(Transform parent,string name,Material material,int count,float width,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
            line.positionCount=count;line.widthMultiplier=width;line.startColor=line.endColor=color;
            line.numCornerVertices=3;line.numCapVertices=3;line.shadowCastingMode=ShadowCastingMode.Off;
            line.receiveShadows=false;line.lightProbeUsage=LightProbeUsage.Off;return line;
        }
        public static void Ring(LineRenderer line,Vector3 center,Vector3 right,Vector3 up,float radius,float rotation,float arc=1)
        {
            for(int i=0;i<line.positionCount;i++)
            {
                float a=rotation+i/(float)(line.positionCount-1)*Mathf.PI*2*arc;
                line.SetPosition(i,center+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius);
            }
        }
        public static void Tint(LineRenderer line,Color color,float alpha){color.a*=alpha;line.startColor=line.endColor=color;}
        public static ParticleSystem Particles(Transform parent,Material material,int capacity)
        {
            var go=new GameObject("Arcane motes");go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=capacity;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.7f;main.startSpeed=0;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var em=ps.emission;em.enabled=false;var shape=ps.shape;shape.enabled=false;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.12f,1),new Keyframe(1,0)));
            var col=ps.colorOverLifetime;col.enabled=true;var g=new Gradient();
            g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.8f,.45f),new GradientAlphaKey(0,1)});col.color=g;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return ps;
        }
        public static void Emit(ParticleSystem ps,Vector3 position,Vector3 velocity,Color color,float size,float lifetime)
        {
            if(!ps)return;
            var e=new ParticleSystem.EmitParams{position=position,velocity=velocity,startColor=color,startSize=size,startLifetime=lifetime,rotation=Random.Range(0,360)};
            ps.Emit(e,1);
        }
        public static void Tick(ParticleSystem ps,float dt){if(ps && dt>0)ps.Simulate(Mathf.Min(dt,.1f),false,false,false);}
    }
}
