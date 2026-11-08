using System.Collections.Generic;
using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    // Uses the encounter clock, so pause also freezes the decorative pulse and ribbon.
    public sealed class KnightBoltVisual : MonoBehaviour
    {
        BossEncounter owner;
        LineRenderer ribbon,filament;
        readonly List<Vector3> positions=new List<Vector3>();
        readonly List<float> times=new List<float>();
        float clock=-1;
        public void Initialize(BossEncounter encounter,KnightRefinedEffects palette,Material accent)
        {
            owner=encounter;
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var blade=KnightVisuals.Line("Ivory seal blade",transform,palette.ivory?palette.ivory:palette.gold,
                new[]{new Vector3(0,0,-.42f),new Vector3(.085f,0,0),new Vector3(0,0,.58f),new Vector3(-.085f,0,0),new Vector3(0,0,-.42f)},.035f);
            blade.useWorldSpace=false;
            var spine=KnightVisuals.Line("Golden seal spine",transform,palette.gold,new[]{new Vector3(0,0,-.5f),new Vector3(0,0,.62f)},.055f);spine.useWorldSpace=false;
            var rune=KnightVisuals.Line("Absorbable cyan seal",transform,accent,
                new[]{new Vector3(-.17f,0,-.12f),new Vector3(0,.14f,0),new Vector3(.17f,0,-.12f),new Vector3(0,-.14f,0)},.025f,true);rune.useWorldSpace=false;
            ribbon=KnightVisuals.Line("Curved gold projectile wake",transform,palette.softGold,new Vector3[0],.09f);
            filament=KnightVisuals.Line("Cyan projectile filament",transform,accent,new Vector3[0],.023f);
            foreach(var line in new[]{ribbon,filament}){float width=line.startWidth;line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(1,1));line.widthMultiplier=width;}
            Present();
        }
        public void Present()
        {
            if(!owner || !ribbon || Mathf.Abs(owner.BattleTime-clock)<.00001f)return;
            clock=owner.BattleTime;
            positions.Add(transform.position);times.Add(clock);
            while(times.Count>0 && (clock-times[0]>.18f || times.Count>16)){times.RemoveAt(0);positions.RemoveAt(0);}
            ribbon.positionCount=filament.positionCount=positions.Count;
            ribbon.SetPositions(positions.ToArray());filament.SetPositions(positions.ToArray());
        }
    }
}
