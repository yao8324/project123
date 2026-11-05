using System.Collections.Generic;
using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    // Camera-space retinal echoes use the actual sword, shield and freshly baked glove poses.
    // Static imported meshes stay shared (they intentionally have Read/Write disabled).
    [DefaultExecutionOrder(100)]
    public sealed class DashAfterimageVfx : MonoBehaviour
    {
        const int Count = 6, Rays = 22;
        const float Life = .26f;
        sealed class Source { public Renderer renderer; public Mesh mesh; public SkinnedMeshRenderer skin; }
        sealed class Echo
        {
            public Transform root;
            public MeshRenderer[] renderers;
            public Mesh[] baked;
            public float age;
            public Color tint;
        }
        readonly List<Source> sources = new List<Source>();
        readonly Echo[] echoes = new Echo[Count];
        MaterialPropertyBlock block;
        readonly Color[] colors = { new Color(.12f,1,.62f), new Color(.08f,.72f,1), new Color(.91f,.16f,.95f), new Color(1,.69f,.13f) };
        FirstPersonController player;
        SwordShieldView view;
        Transform root, streakRoot;
        Material echoMaterial, streakMaterial;
        Mesh streakMesh;
        MeshRenderer streakRenderer;
        Vector3[] vertices = new Vector3[Rays * 4];
        Color[] vertexColors = new Color[Rays * 4];
        float age, nextSample;
        int cursor;
        bool active;
        public int LiveEchoCount { get { int n=0;foreach(var e in echoes)if(e!=null && e.root && e.root.gameObject.activeSelf)n++;return n; } }
        public void Initialize(FirstPersonController owner, SwordShieldView model, Material ribbon)
        {
            player=owner;view=model;block=new MaterialPropertyBlock();
            echoMaterial=Resources.Load<Material>("PlayerDash/TemporalEcho");
            if(!view || !view.viewCamera || !echoMaterial || !ribbon)return;
            root=new GameObject("Perfect dodge spectral echoes").transform;
            root.SetParent(view.viewCamera.transform,false);
            foreach(var r in view.GetComponentsInChildren<Renderer>(true))
            {
                var skin=r as SkinnedMeshRenderer; var filter=r.GetComponent<MeshFilter>();
                bool silhouette=skin || r.name.Contains("| 01 ") || r.name.Contains("| 03 Guard body") ||
                    r.name.Contains("| 08 Grip leather") || r.name.Contains("| 02 Gilt rim") || r.name.Contains("| 06 Amethyst eye");
                if(!silhouette || (!skin && (!filter || !filter.sharedMesh)))continue;
                sources.Add(new Source{renderer=r,skin=skin,mesh=skin?skin.sharedMesh:filter.sharedMesh});
            }
            for(int j=0;j<Count;j++)
            {
                var e=new Echo{root=new GameObject("Spectral pose "+j).transform,renderers=new MeshRenderer[sources.Count],baked=new Mesh[sources.Count]};
                e.root.SetParent(root,false);echoes[j]=e;
                for(int k=0;k<sources.Count;k++)
                {
                    var part=new GameObject(sources[k].renderer.name+" echo",typeof(MeshFilter),typeof(MeshRenderer));
                    part.layer=view.gameObject.layer;part.transform.SetParent(e.root,false);
                    var mesh=sources[k].mesh;
                    if(sources[k].skin){mesh=new Mesh{name="Dodge glove snapshot"};mesh.MarkDynamic();e.baked[k]=mesh;}
                    part.GetComponent<MeshFilter>().sharedMesh=mesh;
                    var r=part.GetComponent<MeshRenderer>();e.renderers[k]=r;
                    var mats=new Material[Mathf.Max(1,sources[k].mesh.subMeshCount)];
                    for(int n=0;n<mats.Length;n++)mats[n]=echoMaterial;
                    r.sharedMaterials=mats;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                }
                e.root.gameObject.SetActive(false);
            }
            streakRoot=new GameObject("Peripheral spectral filaments",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            streakRoot.SetParent(root,false);streakRoot.gameObject.layer=view.gameObject.layer;
            streakMaterial=new Material(ribbon){name="Dodge neutral filament"};streakMaterial.SetColor("_BaseColor",Color.white);
            streakRenderer=streakRoot.GetComponent<MeshRenderer>();streakRenderer.sharedMaterial=streakMaterial;
            streakRenderer.shadowCastingMode=ShadowCastingMode.Off;streakRenderer.receiveShadows=false;
            streakMesh=new Mesh{name="Dodge peripheral light strips"};streakMesh.MarkDynamic();
            var uv=new Vector2[vertices.Length];var triangles=new int[Rays*6];
            for(int j=0;j<Rays;j++)
            {
                int v=j*4,t=j*6;uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(0,1);uv[v+2]=new Vector2(1,0);uv[v+3]=new Vector2(1,1);
                triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            streakMesh.vertices=vertices;streakMesh.uv=uv;streakMesh.triangles=triangles;
            streakRoot.GetComponent<MeshFilter>().sharedMesh=streakMesh;Clear();
        }
        public void Trigger(){if(!root)return;Clear();active=true;age=nextSample=0;cursor=0;root.gameObject.SetActive(true);}
        void LateUpdate()
        {
            if(!active || !player || !player.Dash)return;
            if(player.SimulationPaused || !player.GameplayInputEnabled || !player.Dash.SlowActive){Clear();return;}
            Present(Mathf.Min(Time.unscaledDeltaTime,.1f));
        }
        public void Present(float dt)
        {
            if(!active || !root || !view)return;
            age+=Mathf.Max(0,dt);
            if(age<=PlayerDash.SlowDuration-Life && age>=nextSample){Capture(echoes[cursor%Count],cursor);cursor++;nextSample=age+.045f;}
            foreach(var e in echoes)
            {
                if(!e.root.gameObject.activeSelf)continue;e.age+=dt;
                float u=Mathf.Clamp01(e.age/Life);
                if(u>=1){e.root.gameObject.SetActive(false);continue;}
                float side=(e.tint.r>e.tint.g)?1:-1;
                Vector3 drift=view.viewCamera.transform.InverseTransformDirection(-player.Dash.Direction);
                // Chromatic separation stays near the silhouette; the crosshair remains clear.
                e.root.localPosition=new Vector3(side*(.025f+u*.14f)+drift.x*u*.05f,-u*.018f,.014f+u*.045f);
                e.root.localRotation=Quaternion.Euler(0,side*u*1.8f,side*u*2.2f);
                block.SetColor("_Tint",e.tint*1.5f);block.SetFloat("_Opacity",Mathf.Pow(1-u,1.5f)*.66f);block.SetFloat("_Clock",age);
                foreach(var r in e.renderers)r.SetPropertyBlock(block);
            }
            DrawFilaments();
        }
        void Capture(Echo e,int index)
        {
            e.root.localPosition=Vector3.zero;e.root.localRotation=Quaternion.identity;
            e.root.gameObject.SetActive(true);e.age=0;e.tint=colors[index%colors.Length];
            var cameraTransform=view.viewCamera.transform;
            for(int k=0;k<sources.Count;k++)
            {
                var src=sources[k];var r=e.renderers[k];
                r.enabled=src.renderer && src.renderer.enabled;
                if(!r.enabled)continue;
                if(src.skin)src.skin.BakeMesh(e.baked[k],false);
                r.transform.localPosition=cameraTransform.InverseTransformPoint(src.renderer.transform.position);
                r.transform.localRotation=Quaternion.Inverse(cameraTransform.rotation)*src.renderer.transform.rotation;
                r.transform.localScale=src.renderer.transform.lossyScale;
            }
        }
        void DrawFilaments()
        {
            float height=Mathf.Tan(view.viewCamera.fieldOfView*.5f*Mathf.Deg2Rad)*.8f,width=height*view.viewCamera.aspect;
            float strength=player.Dash.RewardWeight;
            for(int j=0;j<Rays;j++)
            {
                float angle=j*2.399963f,phase=Mathf.Repeat(age*(1.3f+j%4*.21f)+j*.173f,1);
                Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                float inner=.68f+phase*.24f,outer=inner+.13f+(j%3)*.045f;
                Vector3 a=new Vector3(direction.x*width*inner,direction.y*height*inner,.8f);
                Vector3 b=new Vector3(direction.x*width*outer,direction.y*height*outer,.8f);
                Vector3 cross=new Vector3(-direction.y,direction.x,0)*(.0018f+(j%3)*.0009f);
                int k=j*4;vertices[k]=a-cross;vertices[k+1]=a+cross;vertices[k+2]=b-cross;vertices[k+3]=b+cross;
                Color c=colors[j%colors.Length];c.a=strength*(.13f+.35f*Mathf.Sin(phase*Mathf.PI));
                vertexColors[k]=vertexColors[k+1]=new Color(c.r,c.g,c.b,0);vertexColors[k+2]=vertexColors[k+3]=c;
            }
            streakMesh.vertices=vertices;streakMesh.colors=vertexColors;streakMesh.RecalculateBounds();
        }
        public void Clear(){active=false;if(root)root.gameObject.SetActive(false);foreach(var e in echoes)if(e!=null && e.root)e.root.gameObject.SetActive(false);}
        void OnDisable(){Clear();}
        void OnDestroy()
        {
            if(root)Destroy(root.gameObject);if(streakMesh)Destroy(streakMesh);if(streakMaterial)Destroy(streakMaterial);
            foreach(var e in echoes)if(e!=null)foreach(var mesh in e.baked)if(mesh)Destroy(mesh);
        }
    }
}
