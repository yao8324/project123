using UnityEngine;
using UnityEngine.UI;

namespace ArcaneArchive.Contest
{
    // Vector geometry remains crisp and does not depend on a font containing a dot glyph.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ContestReticle : MaskableGraphic
    {
        public bool Ready { get; private set; }
        public bool Blocked { get; private set; }
        protected override void Awake(){base.Awake();raycastTarget=false;}
        public void Present(bool ready,bool blocked)
        {
            if(Ready==ready && Blocked==blocked)return;
            Ready=ready;Blocked=blocked;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius=Ready?10:Blocked?8:6;
            var foreground=Ready?new Color(1,.86f,.48f):Blocked?new Color(.86f,.70f,.43f):Color.white;
            Ring(mesh,radius+1.6f,radius-1.8f,new Color(0,0,0,.85f));
            Ring(mesh,radius,radius-1.15f,foreground);
            Ring(mesh,3.5f,0,new Color(0,0,0,.9f));
            Ring(mesh,2.1f,0,foreground);
        }
        static void Ring(VertexHelper mesh,float outer,float inner,Color tint)
        {
            const int segments=48;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                int start=mesh.currentVertCount;
                mesh.AddVert(new Vector3(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer),tint,Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(b)*outer,Mathf.Sin(b)*outer),tint,Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(b)*inner,Mathf.Sin(b)*inner),tint,Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner),tint,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
