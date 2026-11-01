using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    /// <summary>Builds reusable branching ribbons and engraved warnings; no filled neon floor.</summary>
    public static class WardenRiftBuilder
    {
        const string Root=WardenCombatV2Builder.Root;
        sealed class Geometry
        {
            public readonly List<Vector3> v=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<Color> colors=new List<Color>();
            public readonly List<int> triangles=new List<int>();
            public void Ribbon(List<Vector3> points,float width,float seed=0,bool vertical=false)
            {
                int start=v.Count;
                for(int i=0;i<points.Count;i++)
                {
                    var dir=points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(i-1,0)];
                    var side=Vector3.Cross(vertical?Vector3.forward:Vector3.up,dir).normalized;
                    float t=i/(float)(points.Count-1),w=width*Mathf.Lerp(1,.23f,t);
                    v.Add(points[i]-side*w);v.Add(points[i]+side*w);
                    float u=vertical?t:Mathf.Clamp01(points[i].z)*.85f;
                    uv.Add(new Vector2(u,0));uv.Add(new Vector2(u,1));
                    var c=new Color(seed,1,1,1);colors.Add(c);colors.Add(c);
                    if(i==points.Count-1)continue;int n=start+i*2;
                    triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
                }
            }
            public Mesh Mesh(string name)
            {var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
        static Vector2[] Boundary()=>new[]{new Vector2(-.5f,0),new Vector2(.5f,0),new Vector2(.5f,1),new Vector2(-.5f,1)};
        static Mesh CrackMesh(int seed)
        {
            var rng=new System.Random(1309+seed*97);var g=new Geometry();var spine=new List<Vector3>();
            for(int k=0;k<=16;k++)
            {
                float z=k/16f,x=k==0 || k==16?0:((float)rng.NextDouble()-.5f)*.16f;
                spine.Add(new Vector3(x,0,z));
            }
            g.Ribbon(spine,.036f,seed*.2f);
            for(int branch=0;branch<14;branch++)
            {
                int origin=1+branch;var from=spine[origin];float side=branch%2==0?1:-1;var twig=new List<Vector3>();
                for(int k=0;k<=6;k++)
                {
                    float t=k/6f;
                    var point=from+new Vector3(side*t*(.32f+(float)rng.NextDouble()*.13f),0,t*.07f);
                    point.x=Mathf.Clamp(point.x,-.47f,.47f);point.z=Mathf.Clamp01(point.z);twig.Add(point);
                }
                g.Ribbon(twig,.016f,(float)rng.NextDouble());
            }
            return g.Mesh("Connected linear fissure "+seed);
        }
        static Mesh RimMesh()
        {
            var g=new Geometry();
            g.Ribbon(new List<Vector3>{new Vector3(0,.002f,0),new Vector3(.018f,.002f,.24f),new Vector3(-.015f,.002f,.48f),new Vector3(.01f,.002f,.73f),new Vector3(0,.002f,1)},.011f);
            for(int side=-1;side<=1;side+=2)
            {
                g.Ribbon(new List<Vector3>{new Vector3(side*.49f,0,0),new Vector3(side*.49f,0,1)},.009f);
                for(int i=0;i<4;i++)
                {
                    float z=.09f+i*.24f;
                    g.Ribbon(new List<Vector3>{new Vector3(side*.36f,0,z),new Vector3(side*.43f,0,z+.04f),new Vector3(side*.36f,0,z+.09f)},.008f);
                    g.Ribbon(new List<Vector3>{new Vector3(side*.40f,0,z+.02f),new Vector3(side*.40f,0,z+.075f)},.007f);
                }
            }
            return g.Mesh("Linear warning filament without ring");
        }
        static Mesh HaloMesh()
        {
            var g=new Geometry();
            for(int z=0;z<=1;z++)for(int x=0;x<3;x++)
            {g.v.Add(new Vector3((x-1)*.48f,0,z));g.uv.Add(new Vector2(x==1?0:1,z));g.colors.Add(Color.white);}
            g.triangles.AddRange(new[]{0,3,1,1,3,4,1,4,2,2,4,5});return g.Mesh("Soft linear ground glow");
        }
        static Mesh WispMesh()
        {
            var g=new Geometry();
            for(int i=0;i<6;i++)
            {
                var p=new List<Vector3>();
                for(int k=0;k<20;k++)
                {
                    float t=k/19f;
                    p.Add(new Vector3(Mathf.Sin(t*5+i)*(.10f+i%2*.09f),t*(.60f+i%3*.16f),.08f+i*.16f+Mathf.Sin(t*3+i)*.05f));
                }
                g.Ribbon(p,.012f,i/6f,true);
            }
            return g.Mesh("Ascending fracture threads");
        }
        static Mesh ShardMesh()
        {
            var m=new Mesh{name="Small fracture shard"};m.vertices=new[]{new Vector3(-.05f,0,-.04f),new Vector3(.06f,0,-.025f),new Vector3(.045f,0,.05f),new Vector3(-.035f,0,.04f),new Vector3(-.02f,.045f,-.02f),new Vector3(.02f,.023f,.015f)};
            m.triangles=new[]{0,1,4,1,2,5,1,5,4,2,3,5,3,0,4,3,4,5,4,5,2,4,2,1,0,3,2,0,2,1};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static Mesh Persist(Mesh mesh,string name)
        {
            mesh.name=name;
            string path=Root+"/Models/Rift/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Material EffectMaterial(string name,float mode,Color tint,Color core,bool ink=false)
        {
            var shader=Shader.Find("Arcane Archive/Rift Filament");if(!shader || !shader.isSupported)throw new InvalidOperationException("Rift shader missing or unsupported");
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;
            m.SetColor("_Tint",tint);m.SetColor("_Core",core);m.SetFloat("_Mode",mode);m.SetFloat("_DstBlend",ink?10:1);m.renderQueue=ink?2998:mode==1?2999:3000;EditorUtility.SetDirty(m);return m;
        }
        static GameObject Part(string name,Transform parent,Mesh mesh,Material mat,float height=0)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*height;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go;
        }
        public static GameObject BuildAsset()
        {
            Directory.CreateDirectory(Root+"/Models/Rift");AssetDatabase.Refresh();
            Color violet=new Color(.28f,.012f,.42f),crimson=new Color(2.4f,.12f,.34f),gold=new Color(1.15f,.64f,.27f);
            var energy=EffectMaterial("RiftCrimsonFilaments",0,violet,crimson);
            var ink=EffectMaterial("RiftDarkScar",3,Color.black,Color.black,true);
            var rim=EffectMaterial("RiftWarningGlyphs",0,new Color(.38f,.18f,.09f),gold);
            var halo=EffectMaterial("RiftLocalGlow",1,violet,new Color(.65f,.01f,.12f));
            var wisp=EffectMaterial("RiftSoulFilaments",2,violet,new Color(1.3f,.16f,.27f));
            var variants=new Mesh[4];for(int i=0;i<4;i++)variants[i]=Persist(CrackMesh(i),"Branches"+i);
            var root=new GameObject("TrackingFractureLine");var visual=root.AddComponent<GroundFractureVisuals>();
            visual.boundary=Boundary();visual.crackVariants=variants;
            var scar=Part("Hairline dark fissures",root.transform,variants[0],ink);
            var lines=Part("Crimson core and amethyst falloff",root.transform,variants[0],energy,.003f);
            visual.scar=scar.GetComponent<Renderer>();visual.glowingCracks=lines.GetComponent<Renderer>();visual.crackLayers=new[]{scar.GetComponent<MeshFilter>(),lines.GetComponent<MeshFilter>()};
            visual.warningRim=Part("Leading warning thread",root.transform,Persist(RimMesh(),"WarningSpine"),rim,.007f).GetComponent<Renderer>();
            visual.halo=Part("Linear reflected glow",root.transform,Persist(HaloMesh(),"SoftLinearGlow"),halo,.002f).GetComponent<Renderer>();
            visual.wisps=Part("Soul filaments",root.transform,Persist(WispMesh(),"SoulArcs"),wisp,.015f).GetComponent<Renderer>();
            var shard=Persist(ShardMesh(),"SmallShard");var stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Warden_12.mat");
            visual.slabs=new Transform[4];
            for(int i=0;i<4;i++){var t=Part("Small obsidian chip "+i,root.transform,shard,stone).transform;t.localPosition=new Vector3(i%2==0?-.19f:.21f,.005f,.12f+i*.23f);visual.slabs[i]=t;}
            visual.embers=new Transform[6];var mote=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Models/SpellCrystal.asset");var gilt=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/SpellOldGold.mat");
            for(int i=0;i<6;i++){var t=Part("Gilt splinter "+i,root.transform,mote,gilt).transform;t.localScale=Vector3.one*.01f;visual.embers[i]=t;}
            var light=new GameObject("Brief reflected rift light");light.transform.SetParent(root.transform,false);light.transform.localPosition=Vector3.up*.25f;visual.riftLight=light.AddComponent<Light>();visual.riftLight.type=LightType.Point;visual.riftLight.color=new Color(.65f,.035f,.16f);visual.riftLight.range=1.8f;visual.riftLight.shadows=LightShadows.None;visual.riftLight.enabled=false;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/IlluminatedFractures.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();return prefab;
        }
    }
}
