using System.Collections.Generic;
using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    public sealed class KnightRefinedEffects : MonoBehaviour
    {
        public Material gold,softGold,ivory;
        public Transform bladeBase,bladeTip;
        public GameObject[] readyBlades;
        sealed class Burst { public GameObject root;public float born,duration;public readonly Dictionary<LineRenderer,float> widths=new Dictionary<LineRenderer,float>(); }
        readonly List<Burst> bursts=new List<Burst>();
        readonly List<Vector3> trail=new List<Vector3>();
        readonly List<float> trailTimes=new List<float>();
        LineRenderer bladeTrail,bladeCore,charge;
        BossEncounter owner;
        float lastClock=-1;

        public void Present(BossEncounter encounter)
        {
            owner=encounter;float clock=owner.BattleTime;
            if(owner.CurrentState==BossEncounter.State.Telegraph)
            {
                float progress=Mathf.Clamp01(owner.AttackElapsed/owner.KnightCurrentSpec.warning);
                var points=new Vector3[49];float radius=.12f+progress*.36f;
                for(int n=0;n<points.Length;n++){float a=n*Mathf.PI*2/48+clock*2;points[n]=bladeTip.position+(owner.View.transform.right*Mathf.Cos(a)+owner.View.transform.up*Mathf.Sin(a))*radius;}
                if(!charge)charge=KnightVisuals.Line("Gathering oath at blade tip",owner.Arena.effectRoot,gold,points,.025f);
                charge.SetPositions(points);charge.widthMultiplier=.5f+progress;
            }
            else if(charge)Destroy(charge.gameObject);
            for(int i=bursts.Count-1;i>=0;i--)
            {
                var burst=bursts[i];float age=(clock-burst.born)/burst.duration;
                if(age>=1){if(burst.root)Destroy(burst.root);bursts.RemoveAt(i);}
                else if(burst.root)
                {
                    foreach(var line in burst.root.GetComponentsInChildren<LineRenderer>())
                    {float width;if(!burst.widths.TryGetValue(line,out width)){width=line.widthMultiplier;burst.widths.Add(line,width);}line.startColor=line.endColor=new Color(1,1,1,1-age);line.widthMultiplier=width*Mathf.Lerp(1f,.15f,age);}
                }
            }
            bool slash=owner.CurrentState==BossEncounter.State.Attack &&
                (owner.KnightCurrentMove==KnightMove.OathCombo || owner.KnightCurrentMove==KnightMove.OrbitSweep || owner.KnightCurrentMove==KnightMove.SealThrust);
            if(slash && Mathf.Abs(clock-lastClock)>.0001f)
            {trail.Add(bladeTip.position);trailTimes.Add(clock);}
            while(trailTimes.Count>0 && (clock-trailTimes[0]>.19f || trail.Count>24)){trailTimes.RemoveAt(0);trail.RemoveAt(0);}
            if(trail.Count>1)
            {
                if(!bladeTrail)bladeTrail=KnightVisuals.Line("Golden oath blade ribbon",owner.Arena.effectRoot,softGold,trail.ToArray(),.12f);
                if(!bladeCore)bladeCore=KnightVisuals.Line("Blade bright edge",owner.Arena.effectRoot,gold,trail.ToArray(),.028f);
                bladeTrail.positionCount=bladeCore.positionCount=trail.Count;bladeTrail.SetPositions(trail.ToArray());bladeCore.SetPositions(trail.ToArray());
            }
            else {if(bladeTrail)Destroy(bladeTrail.gameObject);if(bladeCore)Destroy(bladeCore.gameObject);}
            if(readyBlades!=null)for(int i=0;i<readyBlades.Length;i++)if(readyBlades[i])
            {
                bool show=owner.KnightCurrentMove==KnightMove.SealBolts && (owner.CurrentState==BossEncounter.State.Telegraph || owner.CurrentState==BossEncounter.State.Attack && owner.AttackElapsed<.1f+i*.32f);
                readyBlades[i].SetActive(show);if(show)readyBlades[i].transform.localScale=Vector3.one*(.85f+Mathf.Sin(clock*3+i)*.025f);
            }
            lastClock=clock;
        }
        public void Release(BossEncounter encounter,int index)
        {
            owner=encounter;var move=owner.KnightCurrentMove;
            var root=new GameObject("Knight release: "+move);root.transform.SetParent(owner.Arena.effectRoot,false);
            bursts.Add(new Burst{root=root,born=owner.BattleTime,duration=move==KnightMove.Heavenfall?.58f:.34f});
            Vector3 origin=owner.Arena.boss.position;
            if(move==KnightMove.Heavenfall)
            {
                Vector3 point=owner.KnightHeavenfallPoint(index);Lightning(root.transform,point+Vector3.up*7.5f,point,42+index);
                Circle(root.transform,point+Vector3.up*.075f,owner.KnightCurrentSpec.reach,64);
                Circle(root.transform,point+Vector3.up*.09f,owner.KnightCurrentSpec.reach*.72f,64);
                for(int n=0;n<10;n++){float a=n*Mathf.PI/5;Vector3 ray=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Lightning(root.transform,point+ray*.3f+Vector3.up*.16f,point+ray*owner.KnightCurrentSpec.reach,70+n);}
            }
            else if(move==KnightMove.GateCleave)
            {
                foreach(float angle in owner.Phase==2?new[]{-35f,0,35}:new[]{-25f,25})
                {
                    Vector3 forward=Quaternion.AngleAxis(angle,Vector3.up)*owner.KnightCommittedForward;
                    for(int n=1;n<=7;n++){Vector3 point=origin+forward*(n*owner.KnightCurrentSpec.reach/7);point.y=owner.Arena.transform.position.y;Lightning(root.transform,point+Vector3.up*.45f,point,n*11);}
                }
            }
            else if(move!=KnightMove.Bastion && move!=KnightMove.SealBolts)
            {
                float radius=owner.KnightCurrentSpec.reach;
                float sweep=owner.KnightCurrentSpec.arc;
                if(move==KnightMove.SealThrust)
                {
                    Vector3 start=origin+Vector3.up*1.1f;
                    KnightVisuals.Line("Thrust luminous lance",root.transform,gold,new[]{start,start+owner.KnightCommittedForward*3},.18f);
                    return;
                }
                // Layered, tapered crescents read as a blade wave rather than a wire circle.
                for(int layer=0;layer<6;layer++)
                {
                    var ribbon=new Vector3[65];
                    for(int n=0;n<65;n++){float t=n/64f;float a=Mathf.Lerp(-sweep*.5f,sweep*.5f,t);float r=radius-layer*.11f*Mathf.Sin(t*Mathf.PI);ribbon[n]=origin+Vector3.up*(1.1f+Mathf.Sin(t*Mathf.PI)*.12f)+Quaternion.AngleAxis(a,Vector3.up)*owner.KnightCommittedForward*r;}
                    var line=KnightVisuals.Line("Layered oath crescent",root.transform,layer==0?gold:softGold,ribbon,layer==0?.085f:.12f);
                    line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.18f,.8f),new Keyframe(.62f,1),new Keyframe(1,0));
                    line.widthMultiplier=layer==0?.085f:.12f;
                }
                var points=new Vector3[49];
                for(int i=0;i<49;i++)points[i]=origin+Vector3.up*1.1f+Quaternion.AngleAxis(Mathf.Lerp(-sweep*.5f,sweep*.5f,i/48f),Vector3.up)*owner.KnightCommittedForward*radius;
                KnightVisuals.Line("Swept gold release",root.transform,softGold,points,.11f);
                KnightVisuals.Line("Fine luminous edge",root.transform,gold,points,.025f);
            }
        }
        void Circle(Transform parent,Vector3 center,float radius,int samples)
        {
            var points=new Vector3[samples+1];for(int i=0;i<=samples;i++){float a=i*Mathf.PI*2/samples;points[i]=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
            KnightVisuals.Line("Seal impact footprint",parent,gold,points,.045f);
        }
        void Lightning(Transform parent,Vector3 from,Vector3 to,int seed)
        {
            var points=new Vector3[18];Vector3 delta=to-from;
            for(int i=0;i<18;i++){float t=i/17f;float taper=Mathf.Sin(t*Mathf.PI);points[i]=Vector3.Lerp(from,to,t)+new Vector3(Mathf.Sin(i*7.1f+seed),0,Mathf.Cos(i*4.7f+seed))*.18f*taper;}
            KnightVisuals.Line("Sealing lightning core",parent,gold,points,.065f);
            KnightVisuals.Line("Sealing lightning glow",parent,softGold,points,.17f);
            for(int branch=0;branch<3;branch++)
            {
                int at=4+branch*4;Vector3 end=points[at]+new Vector3(Mathf.Sin(seed+branch)*.7f,-.6f,Mathf.Cos(seed+branch)*.7f);
                KnightVisuals.Line("Lightning branch",parent,gold,new[]{points[at],Vector3.Lerp(points[at],end,.6f)+Vector3.right*.1f,end},.018f);
            }
        }
        public void Clear()
        {
            foreach(var burst in bursts)if(burst.root)Destroy(burst.root);bursts.Clear();trail.Clear();trailTimes.Clear();
            if(bladeTrail)Destroy(bladeTrail.gameObject);if(bladeCore)Destroy(bladeCore.gameObject);
            if(charge)Destroy(charge.gameObject);
            if(readyBlades!=null)foreach(var item in readyBlades)if(item)item.SetActive(false);
        }
        void OnDestroy(){Clear();}
    }
}
