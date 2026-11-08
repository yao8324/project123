using ArcaneArchive.Player;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    // World-space silver shock rings reinforce the screen's outward/inward colour boundary.
    [DefaultExecutionOrder(110)]
    public sealed class TemporalStopPulse : MonoBehaviour
    {
        PlayerDash dash;
        Transform root;
        Material material;
        readonly LineRenderer[] rings = new LineRenderer[3];
        Vector3 origin;
        public bool Visible => root && root.gameObject.activeSelf;
        public float Radius { get; private set; }
        public void Initialize(PlayerDash owner, Material ribbon)
        {
            dash = owner;
            material = new Material(ribbon){name="Temporal silver shockwave"};
            material.SetColor("_BaseColor",new Color(1.65f,1.65f,1.65f,1));
            root = new GameObject("Temporal stop world rings").transform;
            for(int i=0;i<rings.Length;i++)
                rings[i]=BattleVfx.Line(root,"Silver wave "+i,material,129,i==0?.13f:.045f,Color.white);
            Clear();
        }
        public void Trigger(Vector3 playerPosition)
        {
            origin=playerPosition+Vector3.up*.035f;
            root.gameObject.SetActive(true);Present(0);
        }
        public void Present(float time)
        {
            if(!root)return;
            bool returning=time>=PlayerDash.SlowHold;
            float phase=returning?Mathf.InverseLerp(PlayerDash.SlowHold,PlayerDash.SlowDuration,time):
                Mathf.Clamp01(time/.21f);
            Radius=returning?Mathf.Lerp(17,.12f,Mathf.SmoothStep(0,1,phase)):Mathf.Lerp(.12f,17,1-Mathf.Pow(1-phase,2));
            float opacity=returning?Mathf.Sin(phase*Mathf.PI)*.8f:Mathf.Pow(1-phase,1.25f);
            for(int i=0;i<rings.Length;i++)
            {
                float offset=(i-1)*(returning?.35f:.26f);
                BattleVfx.Ring(rings[i],origin+Vector3.up*(i*.018f),Vector3.right,Vector3.forward,Mathf.Max(.03f,Radius+offset),0);
                BattleVfx.Tint(rings[i],i==1?new Color(.48f,.48f,.48f,.9f):Color.white,opacity*(i==0?1:.6f));
                rings[i].enabled=opacity>.001f;
            }
        }
        void LateUpdate()
        {
            if(!dash || !dash.SlowActive){Clear();return;}
            if(root && root.gameObject.activeSelf)Present(dash.RewardAge);
        }
        public void Clear(){if(root)root.gameObject.SetActive(false);Radius=0;}
        void OnDisable(){Clear();}
        void OnDestroy(){if(root)Destroy(root.gameObject);if(material)Destroy(material);}
    }
}
