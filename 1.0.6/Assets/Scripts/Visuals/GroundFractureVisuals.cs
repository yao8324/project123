using UnityEngine;
namespace ArcaneArchive.Visuals
{
    /// <summary>Layered ground rift; no autonomous clock, collider or runtime material allocation.</summary>
    public sealed class GroundFractureVisuals : MonoBehaviour
    {
        public Transform[] slabs, embers;
        public Vector2[] boundary;
        public Renderer glowingCracks, scar, warningRim, halo, wisps;
        public MeshFilter[] crackLayers;
        public Mesh[] crackVariants;
        public Light riftLight;
        Vector3[] rest;
        MaterialPropertyBlock properties;
        float fadeTime=1.25f;
        int seed;
        public float EruptionAge { get; private set; }
        public float WarningProgress { get; private set; }
        public Vector3 StartPoint=>transform.position;
        public Vector3 EndPoint=>transform.TransformPoint(Vector3.forward);
        void Awake()
        {
            rest=new Vector3[slabs.Length];
            for(int i=0;i<slabs.Length;i++)rest[i]=slabs[i].localPosition;
            properties=new MaterialPropertyBlock();
        }
        public void Configure(int variant,float lifetime)
        {
            if(rest==null)Awake();seed=variant;fadeTime=Mathf.Max(.8f,lifetime);
            if(crackVariants!=null && crackVariants.Length>0)
                foreach(var layer in crackLayers)if(layer)layer.sharedMesh=crackVariants[Mathf.Abs(variant)%crackVariants.Length];
            Present(0,-1);
        }
        void Layer(Renderer renderer,float strength,float reveal,float fade,float phase)
        {
            if(!renderer)return;
            properties.Clear();properties.SetFloat("_Strength",strength);properties.SetFloat("_Reveal",reveal);
            properties.SetFloat("_Fade",fade);properties.SetFloat("_Phase",phase);renderer.SetPropertyBlock(properties);
        }
        public void Present(float warning,float eruption)
        {
            if(rest==null)Awake();EruptionAge=eruption;WarningProgress=warning;
            bool firing=eruption>=0;
            float fade=firing?1-Mathf.SmoothStep(0,1,Mathf.Clamp01((eruption-.32f)/(fadeTime-.32f))):1;
            float impulse=firing?Mathf.Exp(-eruption*7):0;
            float reveal=firing?1:Mathf.Lerp(.14f,1,Mathf.SmoothStep(0,1,warning));
            float phase=firing?1+eruption:warning;
            Layer(glowingCracks,firing?1.0f+impulse*2.0f:.35f+warning*.55f,reveal,fade,phase);
            Layer(scar,1,reveal,fade,phase);
            Layer(warningRim,.8f,1,firing?Mathf.Max(0,1-eruption*5):Mathf.SmoothStep(0,1,warning*3),phase);
            Layer(halo,firing?.7f+impulse:.25f+warning*.15f,1,fade,phase);
            Layer(wisps,1.2f,1,firing?fade*Mathf.Sin(Mathf.Clamp01(eruption/.75f)*Mathf.PI):0,phase);
            if(wisps)wisps.transform.localScale=new Vector3(1,firing?.25f+Mathf.Clamp01(eruption/.25f)*.75f:.01f,1);
            if(riftLight){riftLight.enabled=firing && fade>.01f && seed%4==0;riftLight.intensity=(1.2f*impulse+.22f)*fade;}
            for(int i=0;i<slabs.Length;i++)
            {
                slabs[i].gameObject.SetActive(firing && fade>.01f);if(!firing)continue;
                float t=Mathf.Clamp01(eruption/(.6f+(i%3)*.1f));
                float height=Mathf.Sin(t*Mathf.PI)*(.18f+(i%4)*.055f);
                slabs[i].localPosition=rest[i]+Vector3.up*height;
                slabs[i].localRotation=Quaternion.Euler(t*(24+i*3),i*47+seed*17,t*19);
                slabs[i].localScale=Vector3.one*fade;
            }
            if(embers!=null)for(int i=0;i<embers.Length;i++)
            {
                float t=eruption-i*.012f;bool show=firing && t>=0 && t<.95f;
                embers[i].gameObject.SetActive(show);if(!show)continue;
                float angle=i*2.39996f+seed;
                embers[i].localPosition=new Vector3(Mathf.Cos(angle)*.24f*(1+t*.3f),.05f+t*(.65f+(i%4)*.17f)-.2f*t*t,.10f+i*.15f);
                embers[i].localScale=new Vector3(.012f,.045f,.012f)*(1-t/.95f);
                embers[i].localRotation=Quaternion.Euler(15+t*100,angle*Mathf.Rad2Deg,35);
            }
        }
        public bool Contains(Vector3 point,float margin=0)
        {
            var q=new Vector2(point.x,point.z);bool inside=false;
            for(int i=0,j=boundary.Length-1;i<boundary.Length;j=i++)
            {
                var wa=transform.TransformPoint(new Vector3(boundary[j].x,0,boundary[j].y));
                var wb=transform.TransformPoint(new Vector3(boundary[i].x,0,boundary[i].y));
                var a=new Vector2(wa.x,wa.z);var b=new Vector2(wb.x,wb.z);var edge=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(q-a,edge)/Mathf.Max(.0001f,edge.sqrMagnitude));
                if(Vector2.Distance(q,a+edge*t)<=margin)return true;
                if((a.y>q.y)!=(b.y>q.y) && q.x<(b.x-a.x)*(q.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
    }
}
