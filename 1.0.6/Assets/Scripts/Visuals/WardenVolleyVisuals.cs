using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>A harmless battle-clock crown. The encounter explicitly releases each real page.</summary>
    public sealed class WardenVolleyVisuals : MonoBehaviour
    {
        public Transform innerRing, outerRing;
        public Renderer[] ringRenderers;
        GameObject[] pages;
        float radius;
        MaterialPropertyBlock properties;
        public int ReadyCount { get; private set; }
        public float VisualTime { get; private set; }
        public Vector3 Centre => transform.position;

        public void Configure(GameObject pagePrefab, int count, float ringRadius)
        {
            radius = ringRadius; pages = new GameObject[count]; properties = new MaterialPropertyBlock();
            for (int i = 0; i < count; i++)
            {
                pages[i] = Instantiate(pagePrefab, transform); pages[i].name = "Suspended forbidden page " + i;
                var motion = pages[i].GetComponent<WardenSpellMotion>();
                if (motion) motion.SetSuspended(true);
                pages[i].SetActive(false);
            }
        }
        public void Present(Vector3 anchor, Quaternion facing, Vector3 target, float clock, float elapsed, bool releasing, float windup)
        {
            VisualTime = clock;
            transform.SetPositionAndRotation(anchor, facing * Quaternion.Euler(40, 0, 0));
            float progress = releasing ? 1 : Mathf.Clamp01(elapsed / Mathf.Max(.1f, windup));
            float reveal = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.20f, .62f, progress));
            float fade = releasing ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .83f, elapsed)) : reveal;
            innerRing.localRotation = Quaternion.Euler(0, clock * 17, 0);
            outerRing.localRotation = Quaternion.Euler(0, -clock * 11, 0);
            transform.localScale = Vector3.one * Mathf.Lerp(.65f, 1, reveal);
            properties.Clear(); properties.SetFloat("_Fade", fade); properties.SetFloat("_Phase", clock);
            foreach (var renderer in ringRenderers) if (renderer) renderer.SetPropertyBlock(properties);
            ReadyCount = 0;
            for (int i = 0; i < pages.Length; i++)
            {
                if (!pages[i]) continue;
                float born = .26f + i * .027f;
                float growth = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(born, born + .19f, progress));
                pages[i].SetActive(growth > 0); if (growth <= 0) continue;
                float angle = i * Mathf.PI * 2 / pages.Length;
                var local = new Vector3(Mathf.Cos(angle) * radius, .09f * Mathf.Sin(clock * 2 + i * 1.9f), Mathf.Sin(angle) * radius);
                pages[i].transform.localPosition = local * Mathf.Lerp(.64f, 1, growth);
                pages[i].transform.localScale = Vector3.one * growth;
                pages[i].transform.rotation = Quaternion.LookRotation((target - pages[i].transform.position).normalized);
                if (growth >= .99f) ReadyCount++;
            }
        }
        public GameObject Release(int index, Transform effectRoot)
        {
            if (pages == null || index < 0 || index >= pages.Length || !pages[index]) return null;
            var page = pages[index]; pages[index] = null;
            page.SetActive(true); page.transform.SetParent(effectRoot, true); page.transform.localScale = Vector3.one;
            ReadyCount = Mathf.Max(0, ReadyCount - 1); return page;
        }
    }
}
