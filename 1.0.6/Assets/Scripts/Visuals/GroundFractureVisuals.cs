using UnityEngine;
namespace ArcaneArchive.Visuals
{
    public sealed class GroundFractureVisuals : MonoBehaviour
    {
        public Transform[] slabs;
        public Vector2[] boundary;
        public Renderer glowingCracks;
        Vector3[] rest;MaterialPropertyBlock properties;
        void Awake(){rest=new Vector3[slabs.Length];for(int i=0;i<slabs.Length;i++)rest[i]=slabs[i].localPosition;properties=new MaterialPropertyBlock();}
        public void Present(float warning,float eruption)
        {
            if(rest==null)Awake();
            float lift=eruption<0?0:Mathf.Sin(Mathf.Clamp01(eruption/1.6f)*Mathf.PI);
            float fade=eruption<1.5f?1:1-Mathf.Clamp01((eruption-1.5f)/.5f);
            float brightness=eruption<0?Mathf.Lerp(.22f,1.05f,Mathf.Clamp01(warning)):1.5f*fade;
            properties.SetColor("_BaseColor",new Color(1.5f,.025f,.58f,1)*brightness);glowingCracks.SetPropertyBlock(properties);
            for(int i=0;i<slabs.Length;i++){slabs[i].localPosition=rest[i]+Vector3.up*lift*(.07f+(i%5)*.031f);slabs[i].localRotation=Quaternion.Euler(lift*((i%3)-1)*8,0,lift*((i%4)-1.5f)*5);}
        }
        public bool Contains(Vector3 point,float margin=0)
        {
            var p=transform.InverseTransformPoint(point);var q=new Vector2(p.x,p.z);
            float localMargin=margin/Mathf.Max(.01f,Mathf.Min(transform.lossyScale.x,transform.lossyScale.z));bool inside=false;
            for(int i=0,j=boundary.Length-1;i<boundary.Length;j=i++)
            {
                var a=boundary[j];var b=boundary[i];var edge=b-a;float t=Mathf.Clamp01(Vector2.Dot(q-a,edge)/Mathf.Max(.0001f,edge.sqrMagnitude));
                if(Vector2.Distance(q,a+edge*t)<=localMargin)return true;
                if((a.y>q.y)!=(b.y>q.y) && q.x<(b.x-a.x)*(q.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
    }
}
