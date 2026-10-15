using UnityEngine;
using UnityEngine.UI;


namespace ArcaneArchive.UI
{
    // Vector filigree keeps the seal crisp without imported textures.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GildedFiligree : MaskableGraphic
    {
        private Color stroke;
        private Button owner;
        private Color lastTint;
        protected override void Awake() { base.Awake(); owner=GetComponentInParent<Button>(); }
        private void Update()
        {
            // Follow the button's unscaled-time hover fade, including a paused menu.
            if(owner && owner.targetGraphic) {
                Color tint=owner.targetGraphic.canvasRenderer.GetColor();
                if(tint!=lastTint) {lastTint=tint;SetVerticesDirty();}
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float w=rectTransform.rect.width*.5f, h=rectTransform.rect.height*.5f;
            float glow=owner ? Mathf.InverseLerp(.065f,.24f,owner.targetGraphic.canvasRenderer.GetColor().r) : 0f;
            Color gold=Color.Lerp(new Color(.72f,.51f,.22f),new Color(1f,.82f,.43f),glow);
            Color violet=Color.Lerp(color,new Color(.88f,.57f,1f),glow);
            stroke=gold;
            // Broken double border with pointed, claw-like corners.
            for(int side=-1;side<=1;side+=2)
            {
                for(int top=-1;top<=1;top+=2)
                {
                    Line(vh,new Vector2(side*8,top*(h-5)),new Vector2(side*(w-18),top*(h-5)),.8f);
                    Line(vh,new Vector2(side*(w-18),top*(h-5)),new Vector2(side*(w-4),top*(h-1)),1.2f);
                    Line(vh,new Vector2(side*(w-4),top*(h-1)),new Vector2(side*(w-10),top*(h-17)),1.2f);
                    stroke=violet;
                    Curve(vh,new Vector2(side*25,top*(h-9)),new Vector2(side*57,top*(h-20)),new Vector2(side*79,top*(h+4)),new Vector2(side*(w-5),top*7),1.1f);
                    stroke=gold;
                }
                float x=side*(w-24);
                stroke=violet;
                Curve(vh,new Vector2(x,-25),new Vector2(x-side*25,-10),new Vector2(x+side*24,14),new Vector2(x-side*3,23),1.5f);
                Curve(vh,new Vector2(x,25),new Vector2(x-side*30,8),new Vector2(x+side*21,-16),new Vector2(x-side*8,-21),.9f);
                for(int i=0;i<4;i++)
                {
                    float y=-18+i*12;
                    Line(vh,new Vector2(x,y),new Vector2(x+side*(i%2==0?13:9),y+7),1f);
                }
                stroke=gold;
                // Watchful side eyes, slit pupils and hanging needle marks.
                Curve(vh,new Vector2(x-13,0),new Vector2(x-5,10),new Vector2(x+5,10),new Vector2(x+13,0),1.1f);
                Curve(vh,new Vector2(x-13,0),new Vector2(x-5,-8),new Vector2(x+5,-8),new Vector2(x+13,0),1.1f);
                stroke=violet;
                Line(vh,new Vector2(x,-5),new Vector2(x,6),2.3f);
                stroke=gold;
                Line(vh,new Vector2(x,-9),new Vector2(x-side*3,-17),.8f);
            }
            // Small occult diamond seals above and below the wording.
            for(int sign=-1;sign<=1;sign+=2)
            {
                float y=sign*(h-8);
                stroke=violet;
                Line(vh,new Vector2(-5,y),new Vector2(0,y+5),1f);
                Line(vh,new Vector2(0,y+5),new Vector2(5,y),1f);
                Line(vh,new Vector2(5,y),new Vector2(0,y-5),1f);
                Line(vh,new Vector2(0,y-5),new Vector2(-5,y),1f);
                stroke=gold;
                Line(vh,new Vector2(-13,y),new Vector2(-8,y),1f);
                Line(vh,new Vector2(8,y),new Vector2(13,y),1f);
            }
        }
        private void Curve(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,float thickness)
        {
            Vector2 previous=a;
            for(int i=1;i<=24;i++)
            {
                float t=i/24f, u=1f-t;
                Vector2 next=u*u*u*a+3f*u*u*t*b+3f*u*t*t*c+t*t*t*d;
                Line(vh,previous,next,thickness);previous=next;
            }
        }
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float thickness)
        {
            Vector2 offset=new Vector2(-(b-a).y,(b-a).x).normalized*thickness*.5f;
            int start=vh.currentVertCount;
            vh.AddVert(a-offset,stroke,Vector2.zero);vh.AddVert(a+offset,stroke,Vector2.zero);
            vh.AddVert(b+offset,stroke,Vector2.zero);vh.AddVert(b-offset,stroke,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
    }
}
