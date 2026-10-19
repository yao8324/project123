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
        [Min(0f), Tooltip("Playback rate of the imported loop. All parts of an object must share this rate.")]
        public float playbackSpeed = .22f;

        public void Sample(float seconds)
        {
            if (poses == null || poses.Length < 2 || duration <= 0f) return;
            // The final exported pose duplicates the first. Wrap the unique samples
            // so the interpolation keeps its velocity through the loop boundary.
            int count = poses.Length - 1;
            float f = Mathf.Repeat(seconds * Mathf.Max(0f, playbackSpeed), duration) / duration * count;
            int i = Mathf.Min(Mathf.FloorToInt(f), count - 1);
            float t = f - i;
            Pose a = poses[(i + count - 1) % count], b = poses[i];
            Pose c = poses[(i + 1) % count], d = poses[(i + 2) % count];
            transform.localPosition = Cubic(V(a.p), V(b.p), V(c.p), V(d.p), t);
            transform.localRotation = SmoothRotation(Q(a.q), Q(b.q), Q(c.q), Q(d.q), t);
            transform.localScale = Cubic(V(a.s), V(b.s), V(c.s), V(d.s), t);
        }

        static Vector3 Cubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            return .5f * ((2f * b) + (-a + c) * t
                + (2f * a - 5f * b + 4f * c - d) * t * t
                + (-a + 3f * b - 3f * c + d) * t * t * t);
        }

        static Quaternion SmoothRotation(Quaternion a, Quaternion b, Quaternion c, Quaternion d, float t)
        {
            // q and -q describe the same orientation. Align neighbouring samples
            // before cubic interpolation to avoid a spin at the quaternion seam.
            a = SameHemisphere(a, b);
            c = SameHemisphere(c, b);
            d = SameHemisphere(d, c);
            Vector3 xyz = Cubic(new Vector3(a.x, a.y, a.z), new Vector3(b.x, b.y, b.z),
                new Vector3(c.x, c.y, c.z), new Vector3(d.x, d.y, d.z), t);
            float w = Cubic(new Vector3(a.w, 0f, 0f), new Vector3(b.w, 0f, 0f),
                new Vector3(c.w, 0f, 0f), new Vector3(d.w, 0f, 0f), t).x;
            return new Quaternion(xyz.x, xyz.y, xyz.z, w).normalized;
        }

        static Quaternion SameHemisphere(Quaternion q, Quaternion reference)
        {
            return Quaternion.Dot(q, reference) < 0f ? new Quaternion(-q.x, -q.y, -q.z, -q.w) : q;
        }

        static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        static Quaternion Q(float[] a) => new Quaternion(a[0], a[1], a[2], a[3]);
        void Update() => Sample(Time.timeSinceLevelLoad + initialTime);
    }
}
