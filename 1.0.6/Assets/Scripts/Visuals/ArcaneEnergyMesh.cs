using UnityEngine;
using UnityEngine.Rendering;
namespace ArcaneArchive.Visuals
{
    // Reusable strip topology: dynamic positions, fixed UVs/indices, shared materials.
    public sealed class ArcaneEnergyMesh
    {
        public readonly Vector3[] vertices;
        public readonly Color[] colors;
        public readonly int columns, rows;
        public readonly Transform transform;
        public readonly MeshRenderer renderer;
        readonly Mesh mesh;
        readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        public ArcaneEnergyMesh(Transform parent,string name,Material material,int segments,int crossRows=7)
        {
            columns=segments+1;rows=crossRows;
            var go=new GameObject(name);transform=go.transform;transform.SetParent(parent,false);
            mesh=new Mesh{name=name};mesh.MarkDynamic();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            vertices=new Vector3[columns*rows];colors=new Color[vertices.Length];
            var uv=new Vector2[vertices.Length];var triangles=new int[segments*(rows-1)*6];
            for(int i=0;i<columns;i++)for(int j=0;j<rows;j++){uv[i*rows+j]=new Vector2(i/(float)segments,j/(float)(rows-1));colors[i*rows+j]=Color.clear;}
            int at=0;
            for(int i=0;i<segments;i++)for(int j=0;j<rows-1;j++)
            {int v=i*rows+j;triangles[at++]=v;triangles[at++]=v+rows;triangles[at++]=v+1;triangles[at++]=v+1;triangles[at++]=v+rows;triangles[at++]=v+rows+1;}
            mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=triangles;
        }
        public void Commit(float time,float strength)
        {
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
            properties.SetFloat("_Clock",time);properties.SetFloat("_Strength",strength);renderer.SetPropertyBlock(properties);
            renderer.enabled=strength>.001f;
        }
        public void Hide(){renderer.enabled=false;}
        public void Dispose(){if(mesh)Object.Destroy(mesh);}
    }
}
