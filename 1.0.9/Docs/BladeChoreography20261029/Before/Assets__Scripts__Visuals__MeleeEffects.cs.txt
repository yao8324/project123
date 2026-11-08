using ArcaneArchive.Core;
using ArcaneArchive.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals
{
    // All geometry is bounded, short-lived and clock driven. Combat damage never comes from these visuals.
    public sealed class MeleeEffects : MonoBehaviour
    {
        enum Effect { Impact, Parry, LegacySweep, Cast, Heavy, Shield, Thrust, Orbit }
        static readonly Color Gold = new Color(1, .88f, .58f), Pale = new Color(1, .96f, .84f), Violet = new Color(.72f, .48f, 1);
        BossEncounter encounter;
        RecoveryMenu menu;
        LineRenderer[] rays;
        Vector3[] directions;
        float[] widths;
        float age, lifetime, sweepRadius, sweepArc;
        bool startedInCombat;
        int skill;
        Effect effect;
        Vector3 origin;
        Quaternion basis;

        public static LineRenderer CreateLine(string name, Transform parent, Material material, float width, int count)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.layer = parent.gameObject.layer;
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = true;
            line.positionCount = count; line.startWidth = line.endWidth = width;
            line.numCapVertices = 3; line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off; line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.textureMode = LineTextureMode.Stretch; line.alignment = LineAlignment.View;
            return line;
        }

        static MeleeEffects Spawn(string name, Transform parent, Vector3 point, Quaternion facing, BossEncounter owner, Effect kind, float duration, int count)
        {
            if (!parent) return null;
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.position = point; go.layer = parent.gameObject.layer;
            var fx = go.AddComponent<MeleeEffects>(); fx.encounter = owner;
            fx.menu = owner ? owner.GetComponent<RecoveryMenu>() : Object.FindFirstObjectByType<RecoveryMenu>();
            fx.startedInCombat = owner && owner.IsActive;
            fx.origin = point; fx.basis = facing; fx.effect = kind; fx.lifetime = duration;
            fx.rays = new LineRenderer[count]; fx.widths = new float[count];
            return fx;
        }

        void Line(int index, string name, Material material, float width, int count)
        {
            rays[index] = CreateLine(name, transform, material, width, count); widths[index] = width;
            for (int i = 0; i < count; i++) rays[index].SetPosition(i, origin);
        }

        public static void Burst(Transform parent, Vector3 point, Vector3 normal, Material material, BossEncounter owner, bool parry)
        {
            var facing = Quaternion.LookRotation(normal.sqrMagnitude > .001f ? normal : Vector3.forward);
            var fx = Spawn(parry ? "Perfect guard gilt fracture" : "Steel contact sparks", parent, point, facing, owner,
                parry ? Effect.Parry : Effect.Impact, parry ? .32f : .19f, parry ? 16 : 12);
            if (!fx) return;
            int sparks = parry ? 12 : 10;
            fx.directions = new Vector3[sparks];
            for (int i = 0; i < sparks; i++)
            {
                float angle = i * 2.399963f;
                // A narrow spray comes back off the surface, with varied energy and no random full-screen flash.
                fx.directions[i] = facing * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .72f, -.65f).normalized;
                fx.Line(i, "Contact filament", material, i % 3 == 0 ? .018f : .009f, 3);
            }
            for (int i = sparks; i < fx.rays.Length; i++)
                fx.Line(i, parry ? "Broken guard sigil" : "Contact glint", material, i % 2 == 0 ? .014f : .007f, parry ? 49 : 3);
            fx.Draw(0);
        }

        // Kept for existing callers; now rendered as a tapered edge and a lighter offset echo.
        public static void Sweep(Transform parent, Vector3 center, Vector3 forward, float radius, float arc, Material material, BossEncounter owner)
        {
            var facing = Quaternion.LookRotation(forward.sqrMagnitude > .001f ? forward : Vector3.forward, Vector3.up);
            var fx = Spawn("Seal cutting crescent", parent, center, facing, owner, Effect.LegacySweep, .24f, 2);
            if (!fx) return;
            fx.sweepRadius = radius; fx.sweepArc = arc;
            fx.Line(0, "Cutting edge", material, .028f, 49); fx.Line(1, "Trailing filament", material, .009f, 49);
            fx.Draw(0);
        }

        public static void SkillCast(Transform parent, Vector3 point, Quaternion facing, int skill, Material gold, Material violet, BossEncounter owner)
        {
            if (skill < 1 || skill > 4) return;
            var fx = Spawn("Melee anticipation sigil", parent, point, facing, owner, Effect.Cast, skill == 1 ? .24f : .18f, 3);
            if (!fx) return;
            fx.skill = skill;
            fx.Line(0, "Seal gathering edge", gold, .011f, 33);
            fx.Line(1, "Seal inner echo", violet, .005f, 33);
            fx.Line(2, "Seal cut", gold, .009f, 3);
            fx.Draw(0);
        }

        public static void SkillRelease(Transform parent, Vector3 point, Quaternion facing, int skill, Material gold, Material violet, BossEncounter owner)
        {
            if (skill < 1 || skill > 4) return;
            Effect kind = skill == 1 ? Effect.Heavy : skill == 2 ? Effect.Shield : skill == 3 ? Effect.Thrust : Effect.Orbit;
            int count = skill == 1 ? 3 : skill == 2 ? 8 : skill == 3 ? 5 : 4;
            var fx = Spawn("Melee " + kind + " release", parent, point, facing, owner, kind, skill == 4 ? .32f : .24f, count);
            if (!fx) return;
            fx.skill = skill;
            for (int i = 0; i < count; i++)
            {
                bool accent = skill == 4 ? i % 2 == 1 : i == count - 1;
                int points = skill == 3 ? 17 : skill == 2 && i >= 2 ? 3 : 49;
                fx.Line(i, accent ? "Violet edge echo" : "Gilt cutting filament", accent ? violet : gold,
                    i == 0 ? .032f : i == 1 ? .015f : .008f, points);
            }
            fx.Draw(0);
        }

        void Update()
        {
            if ((encounter && encounter.IsPaused) || (menu && menu.IsOpen)) return;
            if (encounter && (encounter.IsFinished || (startedInCombat && !encounter.IsActive))) { Destroy(gameObject); return; }
            age += Mathf.Min(Time.deltaTime, .1f);
            if (age >= lifetime) { Destroy(gameObject); return; }
            Draw(age / lifetime);
        }

        void Style(int index, Color color, float alpha, bool tapered = true)
        {
            var line = rays[index]; color.a = alpha;
            line.startColor = line.endColor = color;
            line.widthMultiplier = widths[index];
            if (tapered)
                line.widthCurve = EdgeWidth;
        }
        static readonly AnimationCurve EdgeWidth = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.14f, .65f),
            new Keyframe(.45f, 1), new Keyframe(.83f, .62f), new Keyframe(1, 0));

        Vector3 World(Vector3 local) => origin + basis * local;

        void Arc(int index, Vector3 center, Vector3 axisA, Vector3 axisB, float radius, float from, float to)
        {
            var line = rays[index];
            for (int p = 0; p < line.positionCount; p++)
            {
                float angle = Mathf.Lerp(from, to, p / (float)(line.positionCount - 1)) * Mathf.Deg2Rad;
                line.SetPosition(p, World(center + (axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle)) * radius));
            }
        }

        void Draw(float t)
        {
            float decay = (1 - t) * (1 - t);
            if (effect == Effect.Impact || effect == Effect.Parry) { DrawImpact(t, decay); return; }
            if (effect == Effect.Cast)
            {
                float visibility = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t + .05f));
                float side = skill == 2 ? .32f : -.32f;
                Vector3 center = new Vector3(side, -.07f, .68f);
                float radius = Mathf.Lerp(.16f, .075f, t);
                Style(0, Gold, visibility * .55f); Style(1, Violet, visibility * .25f); Style(2, Pale, visibility * .38f);
                Arc(0, center, Vector3.right, Vector3.up, radius, 35 + 65 * t, 285 + 65 * t);
                Arc(1, center, Vector3.right, Vector3.up, radius * 1.32f, -130 - 35 * t, 30 - 35 * t);
                rays[2].SetPosition(0, World(center + new Vector3(-radius * .6f, 0, 0)));
                rays[2].SetPosition(1, World(center + new Vector3(0, radius * .25f, 0)));
                rays[2].SetPosition(2, World(center + new Vector3(radius * .6f, 0, 0)));
                return;
            }
            if (effect == Effect.Heavy)
            {
                // Vertical scythe silhouette, offset slightly left for the sword hand.
                Vector3 center = new Vector3(-.12f, -.10f, 1.25f + t * .22f);
                Vector3 right = new Vector3(.24f, .97f, 0), depth = new Vector3(.10f, -.025f, .55f);
                for (int i = 0; i < rays.Length; i++)
                {
                    Style(i, i == 2 ? Violet : Pale, decay * (i == 0 ? .82f : .4f));
                    Arc(i, center + Vector3.right * i * .045f, right, depth, 1.03f + i * .07f, -36 - t * 25, 144 - t * 25);
                }
                return;
            }
            if (effect == Effect.Shield)
            {
                Vector3 center = new Vector3(.22f, -.05f, 1.13f + t * .25f);
                float radius = .32f + t * .3f;
                for (int i = 0; i < rays.Length; i++)
                {
                    Style(i, i == rays.Length - 1 ? Violet : Pale, decay * (i == 0 ? .85f : .50f));
                    if (i < 2) Arc(i, center, Vector3.right, Vector3.up, radius * (i == 0 ? 1 : .72f), i == 0 ? 10 : 195, i == 0 ? 310 : 495);
                    else
                    {
                        float angle = (i - 2) * Mathf.PI / 3;
                        Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                        Vector3 tangent = new Vector3(-radial.y, radial.x, 0);
                        rays[i].SetPosition(0, World(center + radial * radius * .83f - tangent * .04f));
                        rays[i].SetPosition(1, World(center + radial * radius * 1.16f));
                        rays[i].SetPosition(2, World(center + radial * radius * .83f + tangent * .04f));
                    }
                }
                return;
            }
            if (effect == Effect.Thrust)
            {
                for (int i = 0; i < rays.Length; i++)
                {
                    float angle = i * 2.399963f;
                    Style(i, i == 4 ? Violet : Pale, decay * (i == 0 ? .82f : .42f));
                    for (int p = 0; p < rays[i].positionCount; p++)
                    {
                        float u = p / (float)(rays[i].positionCount - 1);
                        float curl = Mathf.Sin(u * Mathf.PI) * (.07f + i * .025f);
                        float z = Mathf.Lerp(.63f + t * .8f, 2.55f + t * .45f, u);
                        rays[i].SetPosition(p, World(new Vector3(-.12f + Mathf.Cos(angle + u * 1.8f) * curl,
                            -.08f + Mathf.Sin(angle + u * 1.8f) * curl, z)));
                    }
                }
                return;
            }
            // Two thin, staggered orbit cuts. Their center stays at the cast position, not the moving camera.
            bool legacy = effect == Effect.LegacySweep;
            float radiusBase = legacy ? sweepRadius : 2.35f;
            float arc = legacy ? sweepArc : 275;
            for (int i = 0; i < rays.Length; i++)
            {
                Style(i, i % 2 == 1 ? Violet : Pale, decay * (i == 0 ? .80f : .42f));
                float radius = radiusBase * (.87f + t * .13f) - i * .08f;
                float tilt = legacy ? 0 : i % 2 == 0 ? .08f : -.06f;
                Vector3 center = new Vector3(0, legacy ? 0 : -.22f + i * .045f, 0);
                Arc(i, center, Vector3.forward, new Vector3(1, tilt, 0), radius, -arc * .5f + t * 22 + i * 12, arc * .5f + t * 22 + i * 12);
            }
        }

        void DrawImpact(float t, float decay)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Style(i, i % 3 == 0 ? Pale : Gold, decay * (i % 3 == 0 ? 1 : .65f));
                float speed = .34f + (i % 4) * .15f;
                float distance = Mathf.Sqrt(t) * speed;
                Vector3 drop = Vector3.down * t * t * .08f;
                Vector3 a = origin + directions[i] * distance + drop;
                Vector3 b = origin + directions[i] * (distance + .07f * (1 - t)) + drop;
                rays[i].SetPosition(0, a); rays[i].SetPosition(1, Vector3.Lerp(a, b, .3f)); rays[i].SetPosition(2, b);
            }
            for (int i = directions.Length; i < rays.Length; i++)
            {
                Style(i, Pale, decay * (effect == Effect.Parry ? .8f : .75f));
                if (effect == Effect.Parry)
                {
                    int n = i - directions.Length;
                    float radius = .14f + t * .25f + (n % 2) * .045f;
                    Arc(i, Vector3.zero, Vector3.right, Vector3.up, radius, n * 90 + 8, n * 90 + 78);
                }
                else
                {
                    float size = .13f * decay;
                    Vector3 axis = i == directions.Length ? Vector3.right : Vector3.up;
                    rays[i].SetPosition(0, World(-axis * size)); rays[i].SetPosition(1, origin); rays[i].SetPosition(2, World(axis * size));
                }
            }
        }
    }
}
