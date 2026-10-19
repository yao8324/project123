using System;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    public sealed class MilanMagicMotion : MonoBehaviour
    {
        [Serializable] public sealed class Pose { public float[] p, q, s; }
        public Pose[] poses;
        public float sampleStep = .25f;
        public float duration = 10f;
        public float initialTime = 34f / 24f;
        public void Sample(float seconds)
        {
            if (poses == null || poses.Length < 2) return;
            float f = Mathf.Repeat(seconds, duration) / sampleStep;
            int i = Mathf.Min(Mathf.FloorToInt(f), poses.Length - 2);
            float t = f - i; Pose a = poses[i], b = poses[i+1];
            transform.localPosition = Vector3.Lerp(V(a.p), V(b.p), t);
            transform.localRotation = Quaternion.Slerp(Q(a.q), Q(b.q), t);
            transform.localScale = Vector3.Lerp(V(a.s), V(b.s), t);
        }
        static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        static Quaternion Q(float[] a) => new Quaternion(a[0], a[1], a[2], a[3]);
        void Update() => Sample(Time.timeSinceLevelLoad + initialTime);
    }
}
