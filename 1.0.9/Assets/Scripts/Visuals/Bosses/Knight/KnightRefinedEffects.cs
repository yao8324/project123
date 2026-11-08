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
        sealed class Spark { public LineRenderer line;public Vector3 origin,velocity; }
        sealed class Burst { public GameObject root;public float born,duration;public readonly Dictionary<LineRenderer,float> widths=new Dictionary<LineRenderer,float>();public readonly List<Spark> sparks=new List<Spark>(); }
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
                    if(burst.widths.Count==0)foreach(var line in burst.root.GetComponentsInChildren<LineRenderer>())burst.widths.Add(line,line.widthMultiplier);
                    float fade=(1-age)*(1-age);
                    foreach(var pair in burst.widths)if(pair.Key){pair.Key.startColor=pair.Key.endColor=new Color(1,1,1,fade);pair.Key.widthMultiplier=pair.Value*Mathf.Sqrt(fade);}
                    float seconds=clock-burst.born;
                    foreach(var spark in burst.sparks)if(spark.line)
                    {
                        Vector3 point=spark.origin+spark.velocity*seconds+Vector3.down*(seconds*seconds*2);
                        spark.line.SetPosition(0,point);spark.line.SetPosition(1,point-spark.velocity.normalized*(.06f+.18f*(1-age)));
                    }
                }
            }
            bool slash=owner.CurrentState==BossEncounter.State.Attack &&
                (owner.KnightCurrentMove==KnightMove.OathCombo || owner.KnightCurrentMove==KnightMove.OrbitSweep || owner.KnightCurrentMove==KnightMove.SealThrust || owner.KnightCurrentMove==KnightMove.LeapCleave || owner.KnightCurrentMove==KnightMove.BlinkChase);
            if(slash && Mathf.Abs(clock-lastClock)>.0001f)
            {trail.Add(bladeTip.position);trailTimes.Add(clock);}
            while(trailTimes.Count>0 && (clock-trailTimes[0]>.19f || trail.Count>24)){trailTimes.RemoveAt(0);trail.RemoveAt(0);}
            if(trail.Count>1)
            {
                if(!bladeTrail){bladeTrail=KnightVisuals.Line("Golden oath blade ribbon",owner.Arena.effectRoot,softGold,trail.ToArray(),.12f);Taper(bladeTrail,.12f);}
                if(!bladeCore){bladeCore=KnightVisuals.Line("Blade bright edge",owner.Arena.effectRoot,ivory?ivory:gold,trail.ToArray(),.028f);Taper(bladeCore,.028f);}
                bladeTrail.positionCount=bladeCore.positionCount=trail.Count;bladeTrail.SetPositions(trail.ToArray());bladeCore.SetPositions(trail.ToArray());
            }
            else {if(bladeTrail)Destroy(bladeTrail.gameObject);if(bladeCore)Destroy(bladeCore.gameObject);}
            if(readyBlades!=null)for(int i=0;i<readyBlades.Length;i++)if(readyBlades[i])
            {
                bool show=owner.KnightCurrentMove==KnightMove.SealBolts && (owner.CurrentState==BossEncounter.State.Telegraph || owner.CurrentState==BossEncounter.State.Attack && owner.AttackElapsed<.1f+i*.32f);
                show|=owner.KnightCurrentMove==KnightMove.BlinkBarrage && (owner.CurrentState==BossEncounter.State.Telegraph || owner.CurrentState==BossEncounter.State.Attack && owner.AttackElapsed<owner.KnightCurrentSpec.impacts[0]);
                readyBlades[i].SetActive(show);if(show)readyBlades[i].transform.localScale=Vector3.one*(.85f+Mathf.Sin(clock*3+i)*.025f);
            }
            lastClock=clock;
        }
        public void Release(BossEncounter encounter,int index)
        {
            owner=encounter;var move=owner.KnightCurrentMove;
            var root=new GameObject("Knight release: "+move);root.transform.SetParent(owner.Arena.effectRoot,false);
            bursts.Add(new Burst{root=root,born=owner.BattleTime,duration=move==KnightMove.Heavenfall?.65f:move==KnightMove.LeapCleave?.6f:.42f});
            Vector3 origin=owner.Arena.boss.position;
            if(move!=KnightMove.Bastion && move!=KnightMove.SealBolts)
                Sparks(root.transform,move==KnightMove.Heavenfall?owner.KnightHeavenfallPoint(index):origin,move==KnightMove.Heavenfall || move==KnightMove.LeapCleave?16:8);
            if(move==KnightMove.LeapCleave)
            {
                Vector3 point=origin+Vector3.up*.08f;
                Circle(root.transform,point,owner.KnightCurrentSpec.reach,64);
                Circle(root.transform,point+Vector3.up*.025f,owner.KnightCurrentSpec.reach*.65f,64);
                Lightning(root.transform,point+Vector3.up*3.2f,point,93);
                for(int i=0;i<8;i++){Vector3 ray=Quaternion.AngleAxis(i*45,Vector3.up)*Vector3.forward;Lightning(root.transform,point+ray*.4f,point+ray*owner.KnightCurrentSpec.reach,i+31);}
            }
            else if(move==KnightMove.BlinkBarrage)
            {
                Circle(root.transform,origin+Vector3.up*.09f,1.1f+index*.2f,48);
                for(int i=0;i<5;i++){Vector3 direction=Quaternion.AngleAxis((i-2)*12+(index==1?6:0),Vector3.up)*owner.KnightCommittedForward;KnightVisuals.Line("Seal fan muzzle",root.transform,gold,new[]{origin+Vector3.up*1.35f+direction*.4f,origin+Vector3.up*1.35f+direction*1.5f},.06f);}
            }
            else if(move==KnightMove.Heavenfall)
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
        public void Blink(BossEncounter encounter,Vector3 from,Vector3 to)
        {
            owner=encounter;trail.Clear();trailTimes.Clear();
            if(bladeTrail)Destroy(bladeTrail.gameObject);if(bladeCore)Destroy(bladeCore.gameObject);
            var root=new GameObject("Oath refraction departure and arrival");root.transform.SetParent(owner.Arena.effectRoot,false);
            bursts.Add(new Burst{root=root,born=owner.BattleTime,duration=.45f});
            foreach(Vector3 point in new[]{from,to})
            {
                Sparks(root.transform,point,8);
                Circle(root.transform,point+Vector3.up*.08f,.9f,48);
                Circle(root.transform,point+Vector3.up*1.5f,.65f,48);
                for(int i=0;i<4;i++){Vector3 offset=Quaternion.AngleAxis(i*90,Vector3.up)*Vector3.forward*.45f;Lightning(root.transform,point+offset+Vector3.up*3.5f,point+offset,i+19);}
            }
        }
        void Circle(Transform parent,Vector3 center,float radius,int samples)
        {
            var points=new Vector3[samples+1];for(int i=0;i<=samples;i++){float a=i*Mathf.PI*2/samples;points[i]=center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
            KnightVisuals.Line("Seal impact footprint",parent,gold,points,.045f);
            KnightVisuals.Line("Soft impact corona",parent,softGold,points,.11f);
        }
        void Lightning(Transform parent,Vector3 from,Vector3 to,int seed)
        {
            var points=new Vector3[18];Vector3 delta=to-from;
            for(int i=0;i<18;i++){float t=i/17f;float taper=Mathf.Sin(t*Mathf.PI);points[i]=Vector3.Lerp(from,to,t)+new Vector3(Mathf.Sin(i*7.1f+seed),0,Mathf.Cos(i*4.7f+seed))*.18f*taper;}
            KnightVisuals.Line("Sealing lightning core",parent,gold,points,.065f);
            KnightVisuals.Line("Sealing lightning glow",parent,softGold,points,.17f);
            KnightVisuals.Line("Ivory lightning filament",parent,ivory?ivory:gold,points,.019f);
            for(int branch=0;branch<3;branch++)
            {
                int at=4+branch*4;Vector3 end=points[at]+new Vector3(Mathf.Sin(seed+branch)*.7f,-.6f,Mathf.Cos(seed+branch)*.7f);
                KnightVisuals.Line("Lightning branch",parent,gold,new[]{points[at],Vector3.Lerp(points[at],end,.6f)+Vector3.right*.1f,end},.018f);
            }
        }
        static void Taper(LineRenderer line,float width)
        {
            line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.35f,.35f),new Keyframe(1,1));
            line.widthMultiplier=width;
        }
        void Sparks(Transform parent,Vector3 origin,int count)
        {
            var burst=bursts[bursts.Count-1];
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                Vector3 velocity=new Vector3(Mathf.Cos(angle)*(2+i%3),1.2f+(i%4)*.5f,Mathf.Sin(angle)*(2+i%3));
                var line=KnightVisuals.Line("Oath ember",parent,i%3==0 && ivory?ivory:gold,new[]{origin+Vector3.up*.1f,origin+Vector3.up*.2f},.025f);
                burst.sparks.Add(new Spark{line=line,origin=origin+Vector3.up*.1f,velocity=velocity});
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
