using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcaneArchive.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace ArcaneArchive.Visuals.Editor
{
    public static partial class BossEncounterBuilder
    {
        [MenuItem("Arcane Archive/Boss/Build fracture and rush upgrade")]
        public static void BuildCombatPolish()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before upgrading assets.");
            Directory.CreateDirectory(Root+"/Meshes/Fracture");AssetDatabase.Refresh();
            var stone=LoadDetailMaterial("Obsidian");var glow=Mat("CrackMagenta",new Color(1.5f,.025f,.58f),0,0,true);
            var root=Node("FracturedGround");var fx=root.AddComponent<GroundFractureVisuals>();
            var random=new System.Random(19019);
            // A visibly asymmetric perimeter, not another circular spell decal.
            var border=new List<Vector2>{new Vector2(-.95f,-.55f),new Vector2(-.3f,-.88f),new Vector2(.36f,-.72f),new Vector2(.88f,-.42f),new Vector2(1,.23f),new Vector2(.46f,.84f),new Vector2(-.28f,1),new Vector2(-.93f,.32f)};
            fx.boundary=border.ToArray();
            fx.glowingCracks=FractureMeshObject("GlowBed",root.transform,border,.006f,.006f,glow).GetComponent<Renderer>();
            var seeds=new List<Vector2>{new Vector2(.04f,-.05f)};
            for(int ring=0;ring<2;ring++)for(int i=0;i<(ring==0?6:10);i++)
            {float a=i*Mathf.PI*2/(ring==0?6:10)+ring*.2f;float r=(ring==0?.42f:.8f)+(float)random.NextDouble()*.10f;seeds.Add(new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
            var slabs=new List<Transform>();
            for(int i=0;i<seeds.Count;i++)
            {
                var cell=new List<Vector2>(border);
                for(int j=0;j<seeds.Count;j++)if(j!=i)cell=ClipCell(cell,seeds[j]-seeds[i],(seeds[j].sqrMagnitude-seeds[i].sqrMagnitude)*.5f);
                if(cell.Count<3)continue;Vector2 center=Vector2.zero;foreach(var point in cell)center+=point;center/=cell.Count;
                var polygon=cell.Select(p=>CrackInset(p,center,border)).ToList();
                // Local origins at cell centers make each slab tilt independently.
                var local=polygon.Select(p=>p-center).ToList();
                var slab=FractureMeshObject("Slab_"+i,root.transform,local,.023f,.10f,stone);
                slab.transform.localPosition=new Vector3(center.x,0,center.y);slabs.Add(slab.transform);
            }
            fx.slabs=slabs.ToArray();var fracture=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/FracturedGround.prefab");UnityEngine.Object.DestroyImmediate(root);
            root=Node("RushTelegraph");var rush=root.AddComponent<RushTelegraphVisuals>();
            rush.left=BeamLine("LaneLeft",root.transform,glow,.04f);rush.right=BeamLine("LaneRight",root.transform,glow,.04f);
            rush.arrow=BeamLine("DirectionArrow",root.transform,LoadDetailMaterial("SealGold"),.05f);rush.arrow.positionCount=3;
            var rushPrefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/RushTelegraph.prefab");UnityEngine.Object.DestroyImmediate(root);
            var chamber=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/SealChamber.prefab");
            try{var arena=chamber.GetComponent<BossArena>();arena.fractureEffect=fracture.GetComponent<GroundFractureVisuals>();arena.rushEffect=rushPrefab.GetComponent<RushTelegraphVisuals>();PrefabUtility.SaveAsPrefabAsset(chamber,Root+"/Prefabs/SealChamber.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(chamber);}
            var data=AssetDatabase.LoadAssetAtPath<BossDefinition>(Root+"/WardenEncounter.asset");
            data.guardHoldDrain=18;data.guardDamageCost=2.8f;data.absorbGuardCost=14;data.guardRegenDelay=1.5f;EditorUtility.SetDirty(data);
            BuildFont();AssetDatabase.SaveAssets();Debug.Log("[Boss] Fracture cells, rush telegraph and higher guard costs saved.");
        }
        static Vector2 CrackInset(Vector2 p,Vector2 center,List<Vector2> border)
        {
            var inset=center+(p-center)*.91f;
            // Keep outside edges on the original floor perimeter so only internal cracks glow.
            for(int i=0;i<border.Count;i++)
            {
                var a=border[i];var edge=border[(i+1)%border.Count]-a;
                float t=Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude);
                if((p-a-edge*t).sqrMagnitude<.000001f)return a+edge*Mathf.Clamp01(Vector2.Dot(inset-a,edge)/edge.sqrMagnitude);
            }
            return inset;
        }
        static List<Vector2> ClipCell(List<Vector2> polygon,Vector2 normal,float limit)
        {
            var output=new List<Vector2>();if(polygon.Count==0)return output;
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float da=Vector2.Dot(a,normal)-limit,db=Vector2.Dot(b,normal)-limit;
                if(da<=0)output.Add(a);
                if((da<=0)!=(db<=0))output.Add(Vector2.LerpUnclamped(a,b,da/(da-db)));
            }
            return output;
        }
        static GameObject FractureMeshObject(string name,Transform parent,List<Vector2> outline,float top,float depth,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=outline.Count;
            foreach(float y in new[]{top,top-depth})foreach(var p in outline)v.Add(new Vector3(p.x,y,p.y));
            for(int i=1;i<n-1;i++)t.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
            for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[]{i,j,n+i,j,n+j,n+i});}
            var mesh=Mesh(v,t);mesh.name=name;string path=Root+"/Meshes/Fracture/"+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(mesh,path);
            var go=Node(name,parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return go;
        }
    }
}
