using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Presentation only. The encounter owns charge time, beam clipping and damage.</summary>
    public sealed class LaserBeamVisuals : MonoBehaviour
    {
        public LineRenderer outerBeam,coreBeam;
        public Transform chargeRing,chargeCore,impactCore;
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
        }
    }
}
