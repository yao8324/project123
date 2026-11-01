using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    // Samples stay in world space: looking around never drags the old blade path with the camera.
    public sealed class BladeRibbon : MonoBehaviour
    {
        const int Count = 36, Rows = 7;
        static readonly float[] Span = { .22f, .52f, .78f, .91f, .965f, 1f, 1.018f };
        static readonly float[] Alpha = { 0, .045f, .12f, .25f, .82f, .48f, 0 };
        static readonly float[] CrossUV = { 0, .12f, .28f, .4f, .5f, .64f, 1 };
        readonly Vector3[] roots = new Vector3[Count], tips = new Vector3[Count];
        readonly float[] ages = new float[Count];
        readonly Vector3[] vertices = new Vector3[Count * Rows];
        readonly Color[] colors = new Color[Count * Rows];
        Mesh mesh;
        int samples, action;
        bool wasEmitting;
        Vector3 previousBase, previousTip;
        float Lifetime => action == 1 ? .19f : action == 4 ? .22f : .115f;

        public void Initialize(Material material)
        {
            if (mesh) return;
            mesh = new Mesh { name = "Resampled blade edge ribbon" }; mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var uv = new Vector2[Count * Rows]; var triangles = new int[(Count - 1) * (Rows - 1) * 6];
            for (int i = 0; i < Count; i++)
                for (int row = 0; row < Rows; row++) uv[i * Rows + row] = new Vector2(i / (float)(Count - 1), CrossUV[row]);
            int index = 0;
            for (int i = 0; i < Count - 1; i++)
                for (int row = 0; row < Rows - 1; row++)
                {
                    int v = i * Rows + row;
                    triangles[index++] = v; triangles[index++] = v + 1; triangles[index++] = v + Rows;
                    triangles[index++] = v + 1; triangles[index++] = v + Rows + 1; triangles[index++] = v + Rows;
                }
            mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
        }

        public void Configure(int skill) { action = Mathf.Clamp(skill, 0, 4); }

        public void ResetTrail()
        {
            samples = 0; wasEmitting = false;
            if (!mesh) return;
            System.Array.Clear(colors, 0, colors.Length); mesh.colors = colors;
        }

        void Sample(Vector3 root, Vector3 tip, float age)
        {
            for (int i = Count - 1; i > 0; i--) { roots[i] = roots[i - 1]; tips[i] = tips[i - 1]; ages[i] = ages[i - 1]; }
            roots[0] = root; tips[0] = tip; ages[0] = age; samples = Mathf.Min(Count, samples + 1);
        }

        public void Tick(float dt, bool emitting, Vector3 bladeBase, Vector3 bladeTip)
        {
            if (!mesh || dt <= 0) return;
            dt = Mathf.Min(dt, .1f);
            for (int i = 0; i < samples; i++) ages[i] += dt;
            if (emitting)
            {
                if (wasEmitting && (bladeBase - previousBase).sqrMagnitude < 9)
                {
                    // Resample the rotating blade rather than bridging sparse tips with a flat triangle.
                    int steps = Mathf.Clamp(Mathf.CeilToInt(dt / .0083f), 1, 12);
                    Vector3 oldBlade = previousTip - previousBase, newBlade = bladeTip - bladeBase;
                    for (int step = 1; step <= steps; step++)
                    {
                        float u = step / (float)steps;
                        Vector3 root = Vector3.Lerp(previousBase, bladeBase, u);
                        Sample(root, root + Vector3.Slerp(oldBlade, newBlade, u), dt * (1 - u));
                    }
                }
                else { ResetTrail(); Sample(bladeBase, bladeTip, 0); }
                previousBase = bladeBase; previousTip = bladeTip;
            }
            wasEmitting = emitting;
            Color inner = action == 4 ? new Color(.62f, .32f, 1) : action==3?new Color(.3f,.65f,1):new Color(.74f, .38f, .13f);
            Color edge = action == 4 ? new Color(.87f, .66f, 1) : action==3?new Color(.72f,.92f,1):new Color(1, .93f, .73f);
            for (int i = 0; i < Count; i++)
            {
                int slot = Mathf.Clamp(i, 0, Mathf.Max(0, samples - 1));
                float fade = i < samples ? Mathf.Clamp01(1 - ages[slot] / Lifetime) : 0;
                fade *= fade;
                for (int row = 0; row < Rows; row++)
                {
                    int v = i * Rows + row;
                    vertices[v] = transform.InverseTransformPoint(Vector3.LerpUnclamped(roots[slot], tips[slot], Span[row]));
                    Color color = Color.Lerp(inner, edge, Mathf.Clamp01((Span[row] - .7f) * 3.5f));
                    color.a = Alpha[row] * fade; colors[v] = color;
                }
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
        }
        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
