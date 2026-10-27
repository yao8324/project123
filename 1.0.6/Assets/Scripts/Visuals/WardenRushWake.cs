using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>World-space ribbons and shared low-poly silhouettes, independent of the moving rig.</summary>
    public sealed class WardenRushWake : MonoBehaviour
    {
        public LineRenderer[] ribbons;
        public Transform[] ghosts, scraps;
        public Renderer[] trailRenderers;
        Renderer[][] ghostRenderers;
        MaterialPropertyBlock properties;
        Vector3 origin, direction;
        float distance, duration, finish = -1, latest;
        public float Fade { get; private set; } = 1;
        public float Distance => distance;
        public bool IsLingering => finish >= 0;
        public void Configure(float lifetime)
        {
            duration = lifetime; properties = new MaterialPropertyBlock();
            ghostRenderers = new Renderer[ghosts.Length][];
            for (int i = 0; i < ghosts.Length; i++) ghostRenderers[i] = ghosts[i].GetComponentsInChildren<Renderer>();
        }
        public void SetPath(Vector3 start, Vector3 forward, float travelled, float clock)
        { origin = start; direction = forward; distance = travelled; latest = clock; }
        public void Finish(float clock) { finish = clock; }
        void Tint(Renderer[] items, float fade, float clock)
        {
            properties.Clear(); properties.SetFloat("_Fade", fade); properties.SetFloat("_Phase", clock);
            foreach (var r in items) if (r) r.SetPropertyBlock(properties);
        }
        public bool Present(float clock)
        {
            float age = finish >= 0 ? clock - finish : 0;
            Fade = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / Mathf.Max(.1f, duration)));
            if (Fade <= 0) return false;
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            for (int i = 0; i < ribbons.Length; i++)
            {
                var line = ribbons[i]; line.enabled = distance > .05f;
                float sign = i % 2 == 0 ? -1 : 1;
                for (int j = 0; j < line.positionCount; j++)
                {
                    float p = j / (float)(line.positionCount - 1);
                    float flutter = Mathf.Sin(p * 17 - latest * 8 + i) * .09f * Mathf.Sin(p * Mathf.PI);
                    float height = i < 2 ? 1.55f : 2.6f;
                    line.SetPosition(j, origin + direction * (distance * p) + side * (sign * (.55f + .22f * Mathf.Sin(p * 7)) + flutter) + Vector3.up * (height + flutter - age * .25f));
                }
            }
            Tint(trailRenderers, Fade, clock);
            for (int i = 0; i < ghosts.Length; i++)
            {
                float p = (i + 1f) / (ghosts.Length + 1f);
                ghosts[i].gameObject.SetActive(distance > 1);
                ghosts[i].SetPositionAndRotation(origin + direction * distance * p + Vector3.up * age * .13f, Quaternion.LookRotation(direction));
                ghosts[i].localScale = new Vector3(1 + age * .12f, 1, 1);
                Tint(ghostRenderers[i], Mathf.Clamp01(1 - age / (duration * (.62f + p * .38f))) * .58f, clock);
            }
            for (int i = 0; i < scraps.Length; i++)
            {
                float p = (i + .5f) / scraps.Length;
                scraps[i].position = origin + direction * distance * p + side * Mathf.Sin(i * 2.4f) * .65f + Vector3.up * (1.2f + i % 4 * .3f + age * .6f);
                scraps[i].rotation = Quaternion.Euler(i * 27 + age * 120, i * 43, i * 71 + age * 80);
                scraps[i].localScale = Vector3.one * Fade * (.045f + i % 3 * .014f);
            }
            return true;
        }
    }
}
