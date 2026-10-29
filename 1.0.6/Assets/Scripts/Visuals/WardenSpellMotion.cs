using System.Collections.Generic;
using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Lightweight ornamental motion. All animation and trail history freeze with the encounter.</summary>
    public sealed class WardenSpellMotion : MonoBehaviour
    {
        public Transform ornament, coreOrbit;
        public Transform[] motes;
        public LineRenderer trail, coreTrail;
        public bool projectile;
        public float trailLifetime = .16f, trailLength = 2.4f;
        BossEncounter encounter;
        float born, previous;
        bool suspended;
        readonly List<Vector3> points = new List<Vector3>(64);
        readonly List<float> stamps = new List<float>(64);
        Vector3 lastPosition;
        public bool Suspended => suspended;
        void OnEnable()
        {
            encounter = FindFirstObjectByType<BossEncounter>();
            born = previous = encounter ? encounter.BattleTime : 0;
            ResetTrail();
        }
        void ResetTrail()
        {
            points.Clear(); stamps.Clear(); lastPosition = transform.position;
            points.Add(lastPosition); stamps.Add(previous);
            if (trail) { trail.positionCount = 2; trail.SetPosition(0, lastPosition); trail.SetPosition(1, lastPosition); trail.enabled = !suspended; }
            if (coreTrail) { coreTrail.positionCount = 2; coreTrail.SetPosition(0, lastPosition); coreTrail.SetPosition(1, lastPosition); coreTrail.enabled = !suspended; }
        }
        public void SetSuspended(bool value) { suspended = value; ResetTrail(); }
        public void Launch()
        { suspended = false; born = previous = encounter ? encounter.BattleTime : 0; ResetTrail(); }
        void LateUpdate()
        {
            if (!encounter || encounter.IsPaused || encounter.IsFinished || encounter.BattleTime == previous) return;
            float now = encounter.BattleTime, age = now - born;
            if (ornament) ornament.localRotation = projectile ? Quaternion.Euler(90, 0, suspended ? Mathf.Sin(age * 2) * 12 : age * 155) : Quaternion.Euler(0, age * 12, 0);
            if (coreOrbit) coreOrbit.localRotation = Quaternion.Euler(0, age * -95, 18);
            if (trail && !suspended)
            {
                // Subdivide long movement, then trim by age and distance rather than frame count.
                int subdivisions = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(lastPosition, transform.position) / .18f), 1, 24);
                for (int j = 1; j <= subdivisions; j++)
                {
                    float t = j / (float)subdivisions;
                    points.Add(Vector3.Lerp(lastPosition, transform.position, t)); stamps.Add(Mathf.Lerp(previous, now, t));
                }
                while (points.Count > 2 && (points.Count > 64 || now - stamps[0] > trailLifetime)) { points.RemoveAt(0); stamps.RemoveAt(0); }
                float length = 0;
                for (int j = points.Count - 1; j > 0; j--)
                {
                    length += Vector3.Distance(points[j], points[j - 1]);
                    if (length > trailLength && j > 1) { points.RemoveRange(0, j - 1); stamps.RemoveRange(0, j - 1); break; }
                }
                trail.positionCount = points.Count; if (coreTrail) coreTrail.positionCount = points.Count;
                for (int j = 0; j < points.Count; j++)
                {
                    var point = points[points.Count - 1 - j];
                    trail.SetPosition(j, point); if (coreTrail) coreTrail.SetPosition(j, point);
                }
            }
            lastPosition = transform.position; previous = now;
            if (motes != null) for (int i = 0; i < motes.Length; i++)
            {
                float a = i * 2.399963f + age * .8f;
                float r = projectile ? .15f : .35f + (i % 4) * .18f;
                float h = projectile ? -.1f - (i % 4) * .13f : Mathf.Repeat(age * .45f + i * .19f, .85f);
                motes[i].localPosition = projectile ? new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, h) : new Vector3(Mathf.Cos(a) * r, h, Mathf.Sin(a) * r);
                motes[i].localScale = Vector3.one * (.012f + .012f * (.5f + .5f * Mathf.Sin(age * 3 + i)));
            }
        }
    }
}
