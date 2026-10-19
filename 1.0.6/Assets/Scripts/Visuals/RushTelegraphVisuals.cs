using UnityEngine;
namespace ArcaneArchive.Visuals
{
    public sealed class RushTelegraphVisuals : MonoBehaviour
    {
        public LineRenderer left,right,arrow;
        public void Present(Vector3 start,Vector3 end,float charge)
        {
            var direction=(end-start).normalized;var side=Vector3.Cross(Vector3.up,direction)*.95f;
            left.SetPosition(0,start-side);left.SetPosition(1,end-side);right.SetPosition(0,start+side);right.SetPosition(1,end+side);
            arrow.SetPosition(0,end-direction*.9f-side*.7f);arrow.SetPosition(1,end);arrow.SetPosition(2,end-direction*.9f+side*.7f);
            float width=Mathf.Lerp(.025f,.07f,Mathf.Clamp01(charge));left.startWidth=left.endWidth=right.startWidth=right.endWidth=arrow.startWidth=arrow.endWidth=width;
        }
    }
}
