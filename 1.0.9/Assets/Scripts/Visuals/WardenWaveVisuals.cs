using UnityEngine;
using UnityEngine.Rendering;
namespace ArcaneArchive.Visuals
{
    /// <summary>Inward charge and expanding jumpable wave. Radius and time come from combat.</summary>
    public sealed class WardenWaveVisuals : MonoBehaviour
    {
        ArcaneEnergyMesh crest,veil;
        LineRenderer[] rings,streams;
        ParticleSystem motes;
        Mesh glyphMesh;MeshRenderer glyphRenderer;
        readonly Vector3[] glyphVertices=new Vector3[32*3*4];
        readonly Color[] glyphColors=new Color[32*3*4];
        float previous=-1,emission;
        bool burst;
        public float PresentedRadius {get;private set;}
        public float ChargeProgress {get;private set;}
        public int ParticleCount=>motes?motes.particleCount:0;
        public void Initialize(Material surface,Material line,Material particle,bool charge)
        {
            crest=new ArcaneEnergyMesh(transform,"Sculpted energy crest",surface,192,9);
            veil=new ArcaneEnergyMesh(transform,"Trailing violet veil",surface,192,7);
            rings=new LineRenderer[3];
            for(int i=0;i<3;i++)rings[i]=BattleVfx.Line(transform,"Inscribed wave edge "+i,line,193,i==0?.032f:.018f,i==0?BattleVfx.Gold:BattleVfx.Violet);
            streams=new LineRenderer[charge?12:0];
            for(int i=0;i<streams.Length;i++){streams[i]=BattleVfx.Line(transform,"Inward spiral "+i,line,25,.028f,i%3==0?BattleVfx.Gold:BattleVfx.Violet);streams[i].widthCurve=new AnimationCurve(new Keyframe(0,.1f),new Keyframe(.7f,1),new Keyframe(1,0));}
            motes=BattleVfx.Particles(transform,particle,charge?180:256);
            BuildGlyphs(line);
        }
        void BuildGlyphs(Material material)
        {
            var go=new GameObject("Orbiting engraved runes");go.transform.SetParent(transform,false);
            glyphMesh=new Mesh{name="32 engraved tri-stroke runes"};glyphMesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=glyphMesh;
            glyphRenderer=go.AddComponent<MeshRenderer>();glyphRenderer.sharedMaterial=material;
            glyphRenderer.shadowCastingMode=ShadowCastingMode.Off;glyphRenderer.receiveShadows=false;
            var uv=new Vector2[glyphVertices.Length];var tris=new int[32*3*6];
            for(int i=0;i<96;i++){int v=i*4,t=i*6;uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(0,1);uv[v+2]=new Vector2(1,0);uv[v+3]=new Vector2(1,1);tris[t]=v;tris[t+1]=v+1;tris[t+2]=v+2;tris[t+3]=v+2;tris[t+4]=v+1;tris[t+5]=v+3;}
            glyphMesh.vertices=glyphVertices;glyphMesh.colors=glyphColors;glyphMesh.uv=uv;glyphMesh.triangles=tris;
        }
        void GlyphStroke(int index,Vector3 a,Vector3 b,float alpha)
        {
            Vector3 side=Vector3.Cross(Vector3.up,b-a).normalized*.016f;
            int v=index*4;glyphVertices[v]=a-side;glyphVertices[v+1]=a+side;glyphVertices[v+2]=b-side;glyphVertices[v+3]=b+side;
            Color c=BattleVfx.Gold;c.a*=alpha;for(int j=0;j<4;j++)glyphColors[v+j]=c;
        }
        void Glyphs(float radius,float clock,float fade)
        {
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16+clock*.045f;
                Vector3 r=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),t=new Vector3(-r.z,0,r.x);
                Vector3 c=r*radius+Vector3.up*.07f;float size=Mathf.Min(.21f,radius*.09f);
                GlyphStroke(i*3,c-r*size,c+t*size*.55f,fade);
                GlyphStroke(i*3+1,c+t*size*.55f,c+r*size,fade);
                GlyphStroke(i*3+2,c-t*size*.5f-r*size*.35f,c-t*size*.5f+r*size*.35f,fade*.7f);
            }
            glyphMesh.vertices=glyphVertices;glyphMesh.colors=glyphColors;glyphMesh.RecalculateBounds();glyphRenderer.enabled=fade>.01f;
        }
        void Annulus(ArcaneEnergyMesh mesh,float radius,float width,float height,float clock,float strength)
        {
            for(int i=0;i<mesh.columns;i++)
            {
                float a=i/(float)(mesh.columns-1)*Mathf.PI*2;
                Vector3 outward=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                for(int j=0;j<mesh.rows;j++)
                {
                    float v=j/(float)(mesh.rows-1);
                    float flute=(Mathf.Sin(a*19-clock*2)*.5f+Mathf.Sin(a*37+clock)*.25f)*Mathf.Sin(v*Mathf.PI);
                    float r=Mathf.Max(.05f,radius+(v-.79f)*width+flute*.025f);
                    mesh.vertices[i*mesh.rows+j]=outward*r+Vector3.up*(.025f+Mathf.Sin(v*Mathf.PI)*height+flute*.025f);
                    mesh.colors[i*mesh.rows+j]=new Color(1,1,1,1);
                }
            }
            mesh.Commit(clock,strength);
        }
        float Advance(float clock)
        {
            float dt=previous<0?0:Mathf.Max(0,clock-previous);previous=clock;emission+=dt;BattleVfx.Tick(motes,dt);return dt;
        }
        public void PresentCharge(Vector3 origin,float progress,bool active,float clock)
        {
            Advance(clock);transform.position=origin;ChargeProgress=progress;
            if(!active)
            {
                crest.Hide();veil.Hide();glyphRenderer.enabled=false;
                foreach(var r in rings)r.enabled=false;foreach(var l in streams)l.enabled=false;return;
            }
            progress=Mathf.Clamp01(progress);
            float eased=Mathf.SmoothStep(0,1,progress),radius=Mathf.Lerp(3.7f,.48f,eased);
            PresentedRadius=radius;
            float opacity=Mathf.SmoothStep(0,1,progress*6);
            Annulus(crest,radius,.68f,.12f,clock,opacity);
            Annulus(veil,radius+.42f,.9f,.035f,clock,opacity*.35f);
            Glyphs(radius+.32f,clock,opacity*(1-progress*.7f));
            for(int i=0;i<rings.Length;i++)
            {
                rings[i].enabled=true;float r=Mathf.Lerp(3.4f+i*.32f,.38f+i*.05f,eased);
                BattleVfx.Ring(rings[i],origin+Vector3.up*(.04f+i*.035f),Vector3.right,Vector3.forward,r,0);
                BattleVfx.Tint(rings[i],i==0?BattleVfx.Gold:BattleVfx.Violet,opacity*(.8f-i*.15f));
            }
            for(int i=0;i<streams.Length;i++)
            {
                var l=streams[i];l.enabled=true;float phase=1-Mathf.Repeat(progress*1.9f+i*.08333f,1);
                for(int j=0;j<l.positionCount;j++)
                {
                    float u=j/(float)(l.positionCount-1),r=Mathf.Lerp(.32f,3.8f,Mathf.Clamp01(phase+u*.34f));
                    float a=i*Mathf.PI/6+(1-r/3.8f)*.65f;
                    l.SetPosition(j,origin+new Vector3(Mathf.Cos(a)*r,.065f+Mathf.Sin(u*Mathf.PI)*.10f,Mathf.Sin(a)*r));
                }
                BattleVfx.Tint(l,i%3==0?BattleVfx.Gold:BattleVfx.Violet,opacity*.7f);
            }
            if(emission>=.035f)
            {
                emission=0;
                for(int i=0;i<10;i++)
                {
                    float a=Random.value*Mathf.PI*2,r=Random.Range(1.5f,3.6f);
                    Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    BattleVfx.Emit(motes,origin+radial*r+Vector3.up*Random.Range(.05f,.3f),-radial*(r/.33f),i%3==0?BattleVfx.Gold:BattleVfx.Violet,Random.Range(.055f,.1f),.30f);
                }
            }
        }
        public void PresentWave(Vector3 origin,float radius,float clock)
        {
            Advance(clock);transform.position=origin;PresentedRadius=radius;
            float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((radius-21)/3));
            Annulus(crest,radius,.70f,.25f,clock,fade);
            Annulus(veil,Mathf.Max(.1f,radius-.42f),1.15f,.07f,clock,fade*.46f);
            Glyphs(Mathf.Max(.2f,radius-.43f),clock,fade*.85f);
            for(int i=0;i<rings.Length;i++)
            {
                rings[i].enabled=true;
                BattleVfx.Ring(rings[i],origin+Vector3.up*(i==0?.18f:.04f),Vector3.right,Vector3.forward,Mathf.Max(.1f,radius-i*.19f),0);
                BattleVfx.Tint(rings[i],i==0?BattleVfx.Gold:BattleVfx.Violet,fade*(i==0?.9f:.42f));
            }
            if(!burst || emission>=.045f)
            {
                int count=burst?14:72;burst=true;emission=0;
                for(int i=0;i<count;i++)
                {
                    float a=Random.value*Mathf.PI*2;Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    BattleVfx.Emit(motes,origin+radial*radius+Vector3.up*.14f,radial*Random.Range(.3f,1.1f)+Vector3.up*Random.Range(.3f,1.15f),i%3==0?BattleVfx.Gold:BattleVfx.Violet,Random.Range(.04f,.095f),.4f);
                }
            }
        }
        void OnDestroy(){if(crest!=null)crest.Dispose();if(veil!=null)veil.Dispose();if(glyphMesh)Destroy(glyphMesh);}
    }
}
