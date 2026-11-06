using UnityEngine;
namespace ArcaneArchive.Visuals
{
    public sealed class WardenBlinkVisual : MonoBehaviour
    {
        LineRenderer[] rings;ParticleSystem motes;float born,previous;bool arrival;
        public void Initialize(Material lineMaterial,Material particleMaterial,Vector3 position,Vector3 forward,float time,bool appearing)
        {
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(forward));born=previous=time;arrival=appearing;
            rings=new LineRenderer[6];
            for(int i=0;i<6;i++)rings[i]=BattleVfx.Line(transform,"Torn seal "+i,lineMaterial,49,i<2?.055f:.022f,i%2==0?BattleVfx.Gold:BattleVfx.Violet);
            motes=BattleVfx.Particles(transform,particleMaterial,80);
            for(int i=0;i<64;i++)
            {
                float a=i*2.39996f;var radial=transform.right*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);
                BattleVfx.Emit(motes,position+Vector3.up*1.55f+radial*(appearing?.15f:1.1f),radial*(appearing?3.4f:-1.5f)+forward*Random.Range(-1f,1f),i%3==0?BattleVfx.Gold:BattleVfx.Violet,Random.Range(.05f,.14f),.6f);
            }
        }
        public bool Present(float time)
        {
            float age=time-born;BattleVfx.Tick(motes,time-previous);previous=time;
            float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.65f));
            for(int i=0;i<rings.Length;i++)
            {
                float radius=arrival?Mathf.Lerp(.15f,1.6f,Mathf.Clamp01(age/.28f)):Mathf.Lerp(1.3f,.1f,Mathf.Clamp01(age/.32f));
                Vector3 up=i<4?Vector3.up:transform.forward;
                Vector3 center=transform.position+Vector3.up*(i<4?1.55f:.07f);
                BattleVfx.Ring(rings[i],center,transform.right,up,radius*(1+i*.1f),age*(i%2==0?5:-4)+i,.78f);
                BattleVfx.Tint(rings[i],i%2==0?BattleVfx.Gold:BattleVfx.Violet,fade);
            }
            return age<.75f;
        }
    }
}
