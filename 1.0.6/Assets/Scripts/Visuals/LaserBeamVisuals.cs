using UnityEngine;
namespace ArcaneArchive.Visuals
{
    /// <summary>The encounter owns aim, locking, collision and time.</summary>
    public sealed class LaserBeamVisuals : MonoBehaviour
    {
        public LineRenderer outerBeam,coreBeam,goldHelix,violetHelix;
        public Transform chargeRing,chargeCore,impactCore,ornateGate,violetOrbit;
        public Material detailMaterial,particleMaterial;
        LineRenderer[] seals;LineRenderer impactRing;
        ParticleSystem motes;
        float previousTime=-1,emissionTime;
        bool wasFiring,wasLocked;
        void Ensure()
        {
            if(seals!=null)return;
            seals=new LineRenderer[16];
            for(int i=0;i<seals.Length;i++)seals[i]=BattleVfx.Line(transform,"Rotating sigil "+i,detailMaterial,i<4?65:4,i<4?.018f:.026f,i%2==0?BattleVfx.Gold:BattleVfx.Violet);
            impactRing=BattleVfx.Line(transform,"Impact iris",detailMaterial,49,.045f,BattleVfx.Ice);
            motes=BattleVfx.Particles(transform,particleMaterial,220);
            foreach(var l in new[]{outerBeam,coreBeam,goldHelix,violetHelix})if(l)l.sharedMaterial=detailMaterial;
        }
        public void Present(Vector3 start,Vector3 end,float charge,bool firing,float radius,float time,bool locked=false)
        {
            Ensure();charge=firing?1:Mathf.Clamp01(charge);float dt=previousTime<0?0:Mathf.Max(0,time-previousTime);previousTime=time;
            BattleVfx.Tick(motes,dt);emissionTime+=dt;
            Vector3 delta=end-start;var direction=delta.sqrMagnitude>.0001f?delta.normalized:Vector3.forward;
            var right=Vector3.Cross(direction,Vector3.up).normalized;if(right.sqrMagnitude<.1f)right=Vector3.right;
            var up=Vector3.Cross(right,direction).normalized;
            float pulse=locked?1+.08f*Mathf.Sin(time*32):1;
            outerBeam.SetPosition(0,start);outerBeam.SetPosition(1,end);
            coreBeam.SetPosition(0,start);coreBeam.SetPosition(1,end);
            outerBeam.widthMultiplier=1;coreBeam.widthMultiplier=1;
            outerBeam.startWidth=outerBeam.endWidth=firing?radius*2:.027f+(locked?.023f:.008f)*pulse;
            BattleVfx.Tint(outerBeam,locked||firing?BattleVfx.Gold:BattleVfx.Violet,firing?.55f:.72f);
            coreBeam.enabled=firing;coreBeam.startWidth=coreBeam.endWidth=radius*.32f;BattleVfx.Tint(coreBeam,BattleVfx.Ice,.9f);
            chargeRing.SetPositionAndRotation(start,Quaternion.LookRotation(direction)*Quaternion.Euler(90,0,time*28));
            chargeRing.localScale=Vector3.one*(firing?.7f:Mathf.Lerp(.2f,.7f,charge));
            chargeCore.position=start;chargeCore.localScale=Vector3.one*(firing?.26f:.08f+.19f*charge);
            impactCore.gameObject.SetActive(firing);impactCore.position=end;impactCore.localScale=Vector3.one*radius*.35f;
            if(ornateGate)
            {
                ornateGate.SetPositionAndRotation(start,Quaternion.LookRotation(direction)*Quaternion.Euler(0,0,time*13));
                ornateGate.localScale=Vector3.one*(firing?.9f:Mathf.Lerp(.18f,.9f,charge));
                if(violetOrbit)violetOrbit.localRotation=Quaternion.Euler(0,-time*31,0);
            }
            float sealRadius=(.28f+.58f*charge)*pulse;
            for(int i=0;i<4;i++)
            {
                float r=sealRadius*(.67f+i*.18f);
                BattleVfx.Ring(seals[i],start+direction*(i*.045f-.12f),right,up,r,time*(i%2==0?.6f:-.8f)+i, i==3?.78f:1);
                BattleVfx.Tint(seals[i],i%2==0?BattleVfx.Gold:BattleVfx.Violet,.7f);
            }
            for(int i=4;i<seals.Length;i++)
            {
                float a=(i-4)*Mathf.PI/6-time*.4f;
                Vector3 radial=right*Mathf.Cos(a)+up*Mathf.Sin(a),tangent=-right*Mathf.Sin(a)+up*Mathf.Cos(a);
                Vector3 c=start+radial*sealRadius*1.32f;
                seals[i].SetPosition(0,c-radial*.09f);seals[i].SetPosition(1,c+tangent*.047f);
                seals[i].SetPosition(2,c+radial*.1f);seals[i].SetPosition(3,c-tangent*.047f);
                BattleVfx.Tint(seals[i],locked?BattleVfx.Gold:BattleVfx.Violet,.35f+.6f*charge);
            }
            Helix(goldHelix,start,end,right,up,radius*.72f,time,0,firing);
            Helix(violetHelix,start,end,right,up,radius*1.18f,time,Mathf.PI,firing);
            impactRing.enabled=firing||locked;
            BattleVfx.Ring(impactRing,end-direction*.04f,right,up,(firing?.45f:.23f)*pulse,time*2);
            BattleVfx.Tint(impactRing,locked?BattleVfx.Gold:BattleVfx.Ice,.8f);
            if((locked&&!wasLocked)||(firing&&!wasFiring))
                for(int i=0;i<36;i++){float a=i*Mathf.PI*2/36;var radial=right*Mathf.Cos(a)+up*Mathf.Sin(a);BattleVfx.Emit(motes,start+radial*sealRadius,radial*(firing?2:1)+direction*.3f,i%2==0?BattleVfx.Gold:BattleVfx.Violet,.09f,.5f);}
            if(emissionTime>=.035f)
            {
                emissionTime=0;
                for(int i=0;i<(firing?7:3);i++)
                {
                    float a=Random.value*Mathf.PI*2;var radial=right*Mathf.Cos(a)+up*Mathf.Sin(a);
                    BattleVfx.Emit(motes,start+radial*sealRadius,radial*(-.8f)+direction*.3f,BattleVfx.Violet,.045f,.45f);
                    if(firing)BattleVfx.Emit(motes,Vector3.Lerp(start,end,Random.value)+radial*radius,radial*.6f-direction*2,BattleVfx.Gold,.055f,.35f);
                }
            }
            wasFiring=firing;wasLocked=locked;
        }
        static void Helix(LineRenderer line,Vector3 start,Vector3 end,Vector3 right,Vector3 up,float radius,float time,float phase,bool active)
        {
            if(!line)return;line.enabled=active;if(!active)return;
            line.positionCount=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(start,end)/.13f),48,192);
            line.startWidth=line.endWidth=.035f;BattleVfx.Tint(line,phase==0?BattleVfx.Gold:BattleVfx.Violet,.85f);
            for(int i=0;i<line.positionCount;i++)
            {
                float p=i/(float)(line.positionCount-1),a=p*Vector3.Distance(start,end)*3-time*12+phase;
                line.SetPosition(i,Vector3.Lerp(start,end,p)+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius*Mathf.Sin(p*Mathf.PI));
            }
        }
    }
}
