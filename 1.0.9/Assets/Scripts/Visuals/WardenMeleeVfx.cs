using UnityEngine;
namespace ArcaneArchive.Visuals
{
    /// <summary>Two curved energy sheets follow sampled blade motion; never a detached projectile.</summary>
    public sealed class WardenMeleeVfx : MonoBehaviour
    {
        const int Samples=56;
        readonly Vector3[] roots=new Vector3[Samples],tips=new Vector3[Samples];
        readonly float[] ages=new float[Samples];
        ArcaneEnergyMesh upper,lower;
        LineRenderer[] filaments,chargeThreads;
        LineRenderer bladeCore;
        ParticleSystem motes;
        Vector3 previousRoot,previousTip;
        float clock,emission;
        int count;
        bool wasSlashing,flashed;
        public int ParticleCount=>motes?motes.particleCount:0;
        public int TrailSamples=>count;
        public void Initialize(Material surface,Material line,Material particle)
        {
            upper=new ArcaneEnergyMesh(transform,"Upper crescent veil",surface,Samples-1,9);
            lower=new ArcaneEnergyMesh(transform,"Lower crescent veil",surface,Samples-1,9);
            filaments=new LineRenderer[2];
            for(int i=0;i<2;i++)
            {
                filaments[i]=BattleVfx.Line(transform,"Blade edge filament "+i,line,Samples,i==0?.035f:.018f,i==0?BattleVfx.Gold:BattleVfx.Violet);
                filaments[i].widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.4f,.8f),new Keyframe(1,0));
                var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(i==0?new Color(1,.86f,.48f):new Color(.7f,.3f,1),0),new GradientColorKey(new Color(.5f,.12f,1),1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(.6f,.3f),new GradientAlphaKey(0,1)});filaments[i].colorGradient=g;
            }
            chargeThreads=new LineRenderer[3];
            for(int i=0;i<3;i++)chargeThreads[i]=BattleVfx.Line(transform,"Blade gathering filament "+i,line,40,.022f,i==0?BattleVfx.Gold:BattleVfx.Violet);
            bladeCore=BattleVfx.Line(transform,"Blade luminous edge",line,2,.065f,BattleVfx.Gold);
            motes=BattleVfx.Particles(transform,particle,220);ResetEffect();
        }
        public void ResetEffect()
        {
            count=0;clock=emission=0;wasSlashing=flashed=false;
            if(upper==null)return;upper.Hide();lower.Hide();bladeCore.enabled=false;
            foreach(var line in filaments)line.enabled=false;foreach(var line in chargeThreads)line.enabled=false;
            motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void Sample(Vector3 root,Vector3 tip,float age)
        {
            for(int i=Samples-1;i>0;i--){roots[i]=roots[i-1];tips[i]=tips[i-1];ages[i]=ages[i-1];}
            roots[0]=root;tips[0]=tip;ages[0]=age;count=Mathf.Min(Samples,count+1);
        }
        void Surface(ArcaneEnergyMesh sheet,float side)
        {
            for(int i=0;i<Samples;i++)
            {
                int slot=Mathf.Min(i,Mathf.Max(0,count-1));float fade=i<count?Mathf.Clamp01(1-ages[slot]/.32f):0;
                Vector3 blade=tips[slot]-roots[slot];
                for(int j=0;j<sheet.rows;j++)
                {
                    float v=j/(float)(sheet.rows-1);
                    Vector3 point=roots[slot]+blade*Mathf.Lerp(.02f,1.12f,v);
                    point+=Vector3.up*(Mathf.Sin(v*Mathf.PI)*.19f*side*Mathf.Min(1,blade.magnitude));
                    sheet.vertices[i*sheet.rows+j]=transform.InverseTransformPoint(point);
                    sheet.colors[i*sheet.rows+j]=new Color(1,1,1,fade*Mathf.Sqrt(fade)*Mathf.SmoothStep(0,1,i/3f));
                }
            }
            sheet.Commit(clock,count>1?1:0);
        }
        public void Tick(float dt,Vector3 root,Vector3 tip,float preparation,bool slashing,float elapsed,Vector3 hitCenter,float reach)
        {
            if(upper==null || dt<=0)return;
            Vector3 physicalTip=tip;
            if(slashing && reach>0)
            {
                // Extend the magical crescent to the configured ground-space attack radius.
                Vector3 bladeRay=(tip-root).normalized;
                Vector3 horizontal=Vector3.ProjectOnPlane(bladeRay,Vector3.up);
                Vector3 offset=Vector3.ProjectOnPlane(root-hitCenter,Vector3.up);
                float a=horizontal.sqrMagnitude,b=Vector3.Dot(offset,horizontal);
                if(a>.15f)
                {
                    float length=(-b+Mathf.Sqrt(Mathf.Max(0,b*b+a*(reach*reach-offset.sqrMagnitude))))/a;
                    tip=root+bladeRay*Mathf.Max(Vector3.Distance(root,tip),length);
                }
            }
            dt=Mathf.Min(dt,.1f);clock+=dt;emission+=dt;BattleVfx.Tick(motes,dt);
            for(int i=0;i<count;i++)ages[i]+=dt;
            if(slashing)
            {
                if(!wasSlashing){count=0;flashed=false;Sample(root,tip,0);}
                else
                {
                    int steps=Mathf.Clamp(Mathf.CeilToInt(dt/.006f),1,18);
                    for(int s=1;s<=steps;s++)
                    {
                        float u=s/(float)steps;var r=Vector3.Lerp(previousRoot,root,u);
                        Sample(r,r+Vector3.Slerp(previousTip-previousRoot,tip-root,u),dt*(1-u));
                    }
                }
                previousRoot=root;previousTip=tip;
            }
            wasSlashing=slashing;
            if(count>0 && ages[0]>.34f)count=0;
            Surface(upper,1);Surface(lower,-1);
            for(int j=0;j<2;j++)
            {
                filaments[j].enabled=count>1;
                for(int i=0;i<Samples;i++){int slot=Mathf.Min(i,Mathf.Max(0,count-1));Vector3 b=tips[slot]-roots[slot];filaments[j].SetPosition(i,tips[slot]+b*(j*.055f)+Vector3.up*(j==0?.025f:-.055f));}
                filaments[j].widthMultiplier=(j==0?.035f:.018f)*(count>0?Mathf.Clamp01(1-ages[0]/.32f):0);
            }
            Vector3 direction=(tip-root).normalized,side=Vector3.Cross(direction,Vector3.up).normalized;
            if(side.sqrMagnitude<.1f)side=Vector3.right;Vector3 up=Vector3.Cross(side,direction);
            float charge=Mathf.SmoothStep(0,1,Mathf.Clamp01((preparation-.22f)/.78f));
            for(int i=0;i<chargeThreads.Length;i++)
            {
                var line=chargeThreads[i];line.enabled=preparation>0;
                for(int j=0;j<line.positionCount;j++)
                {
                    float u=j/(float)(line.positionCount-1),a=u*11-clock*9+i*Mathf.PI*2/3;
                    line.SetPosition(j,Vector3.Lerp(root,tip,u)+(side*Mathf.Cos(a)+up*Mathf.Sin(a))*(.06f+.09f*charge)*Mathf.Sin(u*Mathf.PI));
                }
                BattleVfx.Tint(line,i==0?BattleVfx.Gold:BattleVfx.Violet,charge*.85f);
            }
            bladeCore.enabled=preparation>0||slashing;
            bladeCore.SetPosition(0,Vector3.Lerp(root,tip,.2f));bladeCore.SetPosition(1,physicalTip);
            bladeCore.widthMultiplier=slashing?.11f:.025f+.04f*charge;
            BattleVfx.Tint(bladeCore,BattleVfx.Gold,slashing?.9f:charge*.6f);
            if(slashing&&!flashed&&elapsed>=.22f)
            {
                flashed=true;
                Vector3 tangent=Vector3.Cross(Vector3.up,direction);
                for(int i=0;i<48;i++)
                    BattleVfx.Emit(motes,Vector3.Lerp(root,tip,Random.Range(.3f,1.08f)),tangent*Random.Range(1.2f,3.7f)+Vector3.up*Random.Range(-.3f,1.5f),i%3==0?BattleVfx.Gold:BattleVfx.Violet,Random.Range(.045f,.13f),Random.Range(.25f,.55f));
            }
            if(emission>=.025f && (slashing||charge>.2f))
            {
                emission=0;
                for(int i=0;i<(slashing?8:3);i++)
                    BattleVfx.Emit(motes,Vector3.Lerp(root,tip,Random.Range(.25f,1.05f)),slashing?Vector3.up*Random.Range(.3f,1.3f):direction*.5f,i%3==0?BattleVfx.Gold:BattleVfx.Violet,slashing?.06f:.04f,.35f);
            }
        }
        void OnDestroy(){if(upper!=null)upper.Dispose();if(lower!=null)lower.Dispose();}
    }
}
