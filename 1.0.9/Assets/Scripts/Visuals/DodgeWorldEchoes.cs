using System.Collections.Generic;
using ArcaneArchive.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    // Four fixed world poses at points actually traversed by the dash; never follow the camera.
    [DefaultExecutionOrder(120)]
    public sealed class DodgeWorldEchoes : MonoBehaviour
    {
        public const float Lifetime=1.10f;
        sealed class Source { public Renderer renderer; public SkinnedMeshRenderer skin; public Mesh mesh; }
        sealed class Ghost { public Transform root; public Renderer[] parts; public Mesh[] baked; }
        readonly List<Source> sources=new List<Source>();
        readonly Ghost[] ghosts=new Ghost[4];
        readonly LineRenderer[] waves=new LineRenderer[4];
        readonly Color[] colours={new Color(.05f,1,.55f),new Color(.04f,.8f,1),new Color(.9f,.12f,1),new Color(1,.65f,.06f)};
        FirstPersonController player;
        PlayerDash dash;
        SwordShieldView view;
        Transform root;
        MaterialPropertyBlock properties;
        Material ribbonMaterial,echoMaterial;
        Vector3 burstOrigin;
        float elapsed;
        public bool Visible => root && root.gameObject.activeSelf;
        public int GhostCount => Visible?ghosts.Length:0;
        public Vector3 GhostPosition(int index) => ghosts[index].root.position;
        public void Initialize(FirstPersonController owner,SwordShieldView model,Material ribbon)
        {
            player=owner;dash=owner.Dash;view=model;properties=new MaterialPropertyBlock();
            echoMaterial=Resources.Load<Material>("PlayerDash/TemporalEcho");
            var body=Resources.Load<Mesh>("PlayerDash/TemporalTravelerEcho");
            int layer=LayerMask.NameToLayer("TemporalAfterimages");
            if(!body || !echoMaterial || !view || layer<0)return;
            root=new GameObject("Perfect dodge world echoes").transform;
            ribbonMaterial=new Material(ribbon){name="Spectral body wave"};ribbonMaterial.SetColor("_BaseColor",Color.white);
            foreach(var renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                var skin=renderer as SkinnedMeshRenderer;var filter=renderer.GetComponent<MeshFilter>();
                bool shape=skin || renderer.name.Contains("| 01 ") || renderer.name.Contains("| 03 Guard body") ||
                    renderer.name.Contains("| 02 Gilt rim") || renderer.name.Contains("| 08 Grip leather");
                if(shape && (skin || (filter && filter.sharedMesh)))
                    sources.Add(new Source{renderer=renderer,skin=skin,mesh=skin?skin.sharedMesh:filter.sharedMesh});
            }
            for(int i=0;i<ghosts.Length;i++)
            {
                var ghost=new Ghost{root=new GameObject("Fixed spectral traveller "+i).transform,
                    parts=new Renderer[sources.Count+1],baked=new Mesh[sources.Count]};
                ghost.root.SetParent(root,false);ghosts[i]=ghost;
                ghost.parts[0]=Part("Traveller silhouette",body,ghost.root,layer);
                for(int j=0;j<sources.Count;j++)
                {
                    var mesh=sources[j].mesh;
                    if(sources[j].skin){mesh=new Mesh{name="World echo glove"};mesh.MarkDynamic();ghost.baked[j]=mesh;}
                    ghost.parts[j+1]=Part(sources[j].renderer.name,mesh,ghost.root,layer);
                    // Baked meshes are empty until capture: source topology owns the material slots.
                    var mats=new Material[Mathf.Max(1,sources[j].mesh.subMeshCount)];
                    for(int k=0;k<mats.Length;k++)mats[k]=echoMaterial;
                    ghost.parts[j+1].sharedMaterials=mats;
                }
            }
            for(int i=0;i<waves.Length;i++)
            {
                waves[i]=BattleVfx.Line(root,"Expanded chromatic pulse "+i,ribbonMaterial,97,i==0?.10f:.065f,Color.white);
                waves[i].gameObject.layer=layer;
            }
            Clear();
        }
        MeshRenderer Part(string label,Mesh mesh,Transform parent,int layer)
        {
            var part=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));part.layer=layer;
            part.transform.SetParent(parent,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=echoMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        public void Trigger()
        {
            if(!root)return;
            burstOrigin=transform.position;elapsed=0;root.gameObject.SetActive(true);
            for(int i=0;i<ghosts.Length;i++)
            {
                var ghost=ghosts[i];ghost.root.gameObject.SetActive(true);
                ghost.root.SetPositionAndRotation(dash.SamplePath(.10f+i*.22f),transform.rotation);
                for(int j=0;j<sources.Count;j++)
                {
                    var src=sources[j];var part=ghost.parts[j+1];
                    if(src.skin)src.skin.BakeMesh(ghost.baked[j],false);
                    part.transform.localPosition=transform.InverseTransformPoint(src.renderer.transform.position);
                    part.transform.localRotation=Quaternion.Inverse(transform.rotation)*src.renderer.transform.rotation;
                    part.transform.localScale=src.renderer.transform.lossyScale;
                }
            }
            Present(0);
        }
        public void Present(float age)
        {
            if(!root)return;elapsed=Mathf.Max(0,age);
            if(elapsed>=Lifetime){Clear();return;}
            for(int i=0;i<ghosts.Length;i++)
            {
                float fade=1-Mathf.SmoothStep(.15f,Lifetime-i*.055f,elapsed);
                properties.SetColor("_Tint",colours[i]*1.8f);
                properties.SetFloat("_Opacity",fade*.66f);properties.SetFloat("_Clock",elapsed);
                foreach(var renderer in ghosts[i].parts)renderer.SetPropertyBlock(properties);
            }
            for(int i=0;i<waves.Length;i++)
            {
                float t=elapsed-i*.045f;bool show=t>=0 && t<.72f;
                waves[i].enabled=show;if(!show)continue;
                float u=t/.72f,radius=Mathf.Lerp(.62f,3.0f,Mathf.SmoothStep(0,1,u));
                var tilt=Quaternion.Euler(i%2==0?12:-18,0,i%2==0?14:-11);
                Vector3 center=burstOrigin+Vector3.up*(.16f+i*.26f+u*.22f);
                BattleVfx.Ring(waves[i],center,tilt*Vector3.right,tilt*Vector3.forward,radius,elapsed*(i%2==0?1:-1));
                BattleVfx.Tint(waves[i],colours[i]*1.3f,Mathf.Sin(Mathf.Clamp01(u*4)*Mathf.PI*.5f)*Mathf.Pow(1-u,1.25f)*.85f);
            }
        }
        void LateUpdate()
        {
            if(!Visible)return;
            if(!player || player.SimulationPaused || !player.GameplayInputEnabled){Clear();return;}
            Present(elapsed+Time.unscaledDeltaTime);
        }
        public void Clear(){if(root)root.gameObject.SetActive(false);}
        void OnDisable(){Clear();}
        void OnDestroy()
        {
            if(root)Destroy(root.gameObject);if(ribbonMaterial)Destroy(ribbonMaterial);
            foreach(var ghost in ghosts)if(ghost!=null)foreach(var mesh in ghost.baked)if(mesh)Destroy(mesh);
        }
    }
}
