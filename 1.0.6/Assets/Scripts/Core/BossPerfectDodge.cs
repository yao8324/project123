using UnityEngine;
using ArcaneArchive.Player;

namespace ArcaneArchive.Core
{
    public sealed partial class BossEncounter
    {
        PlayerDash ActiveDodge => player ? player.Dash : null;
        bool CheckingDodge => ActiveDodge && ActiveDodge.CanCheckPerfect;
        bool MeleeCovers(Vector3 feet, Vector3 eye)
        {
            Vector3 offset = feet - arena.boss.position; offset.y = 0;
            return offset.magnitude <= definition.meleeReach &&
                Vector3.Angle(arena.boss.forward, offset) <= definition.meleeArc * .5f &&
                Mathf.Abs(feet.y - arena.boss.position.y) < 2 &&
                HasClearShot(arena.boss.position + Vector3.up * 1.4f, eye);
        }
        void CheckDodge(bool threatenedOrigin, bool hitCurrent)
        { if(ActiveDodge)ActiveDodge.ConsiderNearMiss(threatenedOrigin, hitCurrent); }
        // Exact closest distance between the attack segment and the captured upright capsule axis.
        // Capturing the body prevents a crouch/height change from enlarging the award volume.
        bool SegmentThreatensDodge(Vector3 from, Vector3 to, float attackRadius)
        {
            if(!CheckingDodge)return false;
            Vector3 a = to - from, b = ActiveDodge.CapsuleUpper - ActiveDodge.CapsuleLower;
            Vector3 r = from - ActiveDodge.CapsuleLower;
            float aa = Vector3.Dot(a, a), bb = Vector3.Dot(b, b);
            float ab = Vector3.Dot(a, b), ar = Vector3.Dot(a, r), br = Vector3.Dot(b, r);
            float s, t;
            if(aa < .000001f){s = 0; t = bb > .000001f ? Mathf.Clamp01(br / bb) : 0;}
            else if(bb < .000001f){t = 0; s = Mathf.Clamp01(-ar / aa);}
            else
            {
                float denominator = aa * bb - ab * ab;
                s = denominator > .000001f ? Mathf.Clamp01((ab * br - ar * bb) / denominator) : 0;
                t = (ab * s + br) / bb;
                if(t < 0){t = 0; s = Mathf.Clamp01(-ar / aa);}
                else if(t > 1){t = 1; s = Mathf.Clamp01((ab - ar) / aa);}
            }
            float radius = ActiveDodge.CapsuleRadius + attackRadius;
            return (r + a * s - b * t).sqrMagnitude <= radius * radius;
        }
        void CheckRushDodge(Vector3 from, Vector3 to, float radius, float half)
        {
            if(!CheckingDodge || ramHit)return;
            Vector3 oldCenter = (ActiveDodge.CapsuleLower + ActiveDodge.CapsuleUpper) * .5f;
            Vector3 flat = to - from; flat.y = 0;
            Vector3 offset = oldCenter - from; offset.y = 0;
            float t = Mathf.Clamp01(Vector3.Dot(offset, flat) / Mathf.Max(.00001f, flat.sqrMagnitude));
            float verticalGap = Mathf.Max(0, Mathf.Abs(oldCenter.y - from.y) - half -
                Vector3.Distance(ActiveDodge.CapsuleLower, ActiveDodge.CapsuleUpper) * .5f);
            float combined = radius + ActiveDodge.CapsuleRadius;
            bool crossed = (offset - flat * t).sqrMagnitude + verticalGap * verticalGap <= combined * combined;
            CheckDodge(crossed && HasClearShot(from, ActiveDodge.StartEye), false);
        }
    }
}
