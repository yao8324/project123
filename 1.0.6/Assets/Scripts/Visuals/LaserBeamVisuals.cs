using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Presentation only. The encounter owns charge time, beam clipping and damage.</summary>
    public sealed class LaserBeamVisuals : MonoBehaviour
    {
        public LineRenderer outerBeam,coreBeam;
        public Transform chargeRing,chargeCore,impactCore;
        public Transform ornateGate, violetOrbit;
        public LineRenderer goldHelix, violetHelix;
        public void Present(Vector3 start,Vector3 end,float charge,bool firing,float radius,float time)
        {
            var direction=(end-start).normalized;
            outerBeam.SetPosition(0,start);outerBeam.SetPosition(1,end);
            coreBeam.SetPosition(0,start);coreBeam.SetPosition(1,end);
            outerBeam.startWidth=outerBeam.endWidth=firing?radius*2:.035f+.025f*charge;
            coreBeam.enabled=firing;coreBeam.startWidth=coreBeam.endWidth=radius*.55f;
            chargeRing.position=start;
            chargeRing.rotation=Quaternion.LookRotation(direction)*Quaternion.Euler(90,0,time*28);
            chargeRing.localScale=Vector3.one*(firing?.65f:Mathf.Lerp(.2f,.65f,charge));
            chargeCore.position=start;chargeCore.localScale=Vector3.one*(firing?.25f:.08f+.23f*charge);
            impactCore.gameObject.SetActive(firing);impactCore.position=end;impactCore.localScale=Vector3.one*radius*.25f;
            if(ornateGate)
            {
                ornateGate.SetPositionAndRotation(start,Quaternion.LookRotation(direction)*Quaternion.Euler(0,0,time*13));
                ornateGate.localScale=Vector3.one*(firing?.8f:Mathf.Lerp(.15f,.8f,charge));
                // Mesh local XZ plane is converted by its FBX parent to Unity XY.
                if(violetOrbit)violetOrbit.localRotation=Quaternion.Euler(0,-time*31,0);
            }
            Helix(goldHelix,start,end,radius*.72f,time,0,firing);
            Helix(violetHelix,start,end,radius*.9f,time,Mathf.PI,firing);
        }
        static void Helix(LineRenderer line,Vector3 start,Vector3 end,float radius,float time,float phase,bool active)
        {
            if(!line)return;line.enabled=active;if(!active)return;
            var direction=(end-start).normalized;var right=Vector3.Cross(direction,Vector3.up).normalized;
            if(right.sqrMagnitude<.1f)right=Vector3.right;var up=Vector3.Cross(right,direction).normalized;
            for(int i=0;i<line.positionCount;i++)
            {
                float p=i/(float)(line.positionCount-1),a=p*Vector3.Distance(start,end)*3-time*4+phase;
                line.SetPosition(i,Vector3.Lerp(start,end,p)+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius*Mathf.Sin(p*Mathf.PI));
            }
        }
    }
}
