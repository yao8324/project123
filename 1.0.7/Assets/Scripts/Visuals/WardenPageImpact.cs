using UnityEngine;
namespace ArcaneArchive.Visuals
{
    public sealed class WardenPageImpact : MonoBehaviour
    {
        public Transform seal;
        public Transform[] scraps;
        public Renderer[] renderers;
        float born;
        bool absorbed;
        Vector3 start, target;
        MaterialPropertyBlock properties;
        public void Configure(float clock, bool wasAbsorbed, Vector3 destination)
        { born = clock; absorbed = wasAbsorbed; start = transform.position; target = destination; properties = new MaterialPropertyBlock(); }
        public bool Present(float clock)
        {
            float t = Mathf.Clamp01((clock - born) / .48f);
            if (t >= 1) return false;
            transform.position = absorbed ? Vector3.Lerp(start, target, t * t) : start;
            float radius = absorbed ? .65f * (1 - t) : .12f + .65f * Mathf.Sqrt(t);
            seal.localScale = Vector3.one * radius;
            seal.localRotation = Quaternion.Euler(90, t * 90, 0);
            for (int i = 0; i < scraps.Length; i++)
            {
                float a = i * 2.399963f;
                scraps[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), .2f * Mathf.Sin(i)) * radius;
                scraps[i].localRotation = Quaternion.Euler(t * 190 + i * 19, i * 71, t * 130);
                scraps[i].localScale = Vector3.one * .06f * (1 - t);
            }
            properties.Clear(); properties.SetFloat("_Fade", 1 - t); properties.SetFloat("_Phase", clock);
            foreach (var r in renderers) if (r) r.SetPropertyBlock(properties);
            return true;
        }
    }
}
