using System;
using ArcaneArchive.Core;
using ArcaneArchive.Player;
using UnityEngine;

namespace ArcaneArchive.Visuals
{
    /// <summary>Visual-only constraints, evaluated after the battle-time pose. Never owns movement or damage.</summary>
    [DisallowMultipleComponent]
    public sealed class KnightRigConstraints : MonoBehaviour
    {
        [Serializable] public sealed class RibbonChain { public Transform[] bones; [NonSerialized] public Quaternion[] bind; }
        public KnightVisuals visual;
        public Transform rightGrip, leftGrip, leftFoot, rightFoot, head;
        public RibbonChain[] ribbons;
        public float gripBlendSeconds = .18f, soleOffset = .105f;
        public float LeftGripError { get; private set; }
        public float RightGripError { get; private set; }
        public float FootGroundError { get; private set; }
        public float TwoHandWeight { get; private set; }
        public int GroundedFeet { get; private set; }
        float previousClock = -1, distance;
        Vector3 previousPosition, leftPlant, rightPlant;
        bool planted, initialized;
        KnightArenaPresentation environment;

        void Initialize(BossEncounter encounter)
        {
            initialized = true; previousPosition = transform.position;
            if (ribbons != null) foreach (var chain in ribbons)
            {
                if (chain == null || chain.bones == null) continue;
                chain.bind = new Quaternion[chain.bones.Length];
                for (int i = 0; i < chain.bones.Length; i++) chain.bind[i] = chain.bones[i].localRotation;
            }
            environment = encounter.Arena.GetComponent<KnightArenaPresentation>();
        }

        public void Apply(BossEncounter encounter)
        {
            if (!visual || !rightGrip || !leftGrip || !encounter.Arena) return;
            if (!initialized) Initialize(encounter);
            float clock = encounter.BattleTime;
            if (previousClock >= 0 && Mathf.Abs(clock - previousClock) < .000001f) return;
            // Retry starts a fresh visual session instead of carrying old planted feet or secondary motion.
            if (clock < previousClock) { planted = false; TwoHandWeight = 0; distance = 0; previousPosition = transform.position; }
            float dt = previousClock < 0 || clock < previousClock ? 1f / 60 : Mathf.Min(.1f, clock - previousClock);
            previousClock = clock;
            var state = encounter.CurrentState;
            var move = encounter.KnightCurrentMove;
            bool melee = move == KnightMove.OathCombo || move == KnightMove.SealThrust || move == KnightMove.OrbitSweep;
            bool working = state == BossEncounter.State.Telegraph || state == BossEncounter.State.Attack || state == BossEncounter.State.Recovery;
            bool twoHands = melee && working && !encounter.KnightGuarding;
            TwoHandWeight = Mathf.MoveTowards(TwoHandWeight, twoHands ? 1 : 0, dt / Mathf.Max(.01f, gripBlendSeconds));

            // The weapon remains the pose source. Translation is constrained to the intersection of arm reach,
            // then both arm chains are solved. Restoring the world pose prevents parent-hand feedback drift.
            Vector3 weaponPosition = visual.weapon.position;
            Quaternion weaponRotation = visual.weapon.rotation;
            weaponPosition += ReachCorrection(visual.rightArm, visual.rightForearm, visual.rightHand, rightGrip.position);
            visual.weapon.SetPositionAndRotation(weaponPosition, weaponRotation);
            if (TwoHandWeight > .001f)
            {
                Vector3 rightOffset = rightGrip.position - weaponPosition, leftOffset = leftGrip.position - weaponPosition;
                Vector3 projected = weaponPosition;
                for (int i = 0; i < 8; i++)
                {
                    projected += ReachCorrection(visual.rightArm, visual.rightForearm, visual.rightHand, projected + rightOffset);
                    projected += ReachCorrection(visual.leftArm, visual.leftForearm, visual.leftHand, projected + leftOffset);
                }
                weaponPosition = Vector3.Lerp(weaponPosition, projected, TwoHandWeight);
                weaponPosition += ReachCorrection(visual.rightArm, visual.rightForearm, visual.rightHand, weaponPosition + rightOffset);
                visual.weapon.SetPositionAndRotation(weaponPosition, weaponRotation);
            }
            Vector3 rightTarget = rightGrip.position, leftTarget = leftGrip.position;
            Quaternion rightOrientation = rightGrip.rotation, leftOrientation = leftGrip.rotation;
            SolveArm(visual.rightArm, visual.rightForearm, visual.rightHand, rightTarget, -1, 1);
            visual.rightHand.rotation = rightOrientation;
            if (TwoHandWeight > .001f)
            {
                SolveArm(visual.leftArm, visual.leftForearm, visual.leftHand, leftTarget, 1, TwoHandWeight);
                visual.leftHand.rotation = Quaternion.Slerp(visual.leftHand.rotation, leftOrientation, TwoHandWeight);
            }
            visual.weapon.SetPositionAndRotation(weaponPosition, weaponRotation);
            RightGripError = Vector3.Distance(visual.rightHand.position, rightGrip.position);
            LeftGripError = Vector3.Distance(visual.leftHand.position, leftGrip.position);

            ApplyFeet(encounter, dt);
            if (head)
            {
                Vector3 view = encounter.transform.position - head.position;
                Vector3 local = transform.InverseTransformDirection(view);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -22, 22);
                head.localRotation = Quaternion.Euler(0, yaw * .45f, 0);
            }
            if (ribbons != null) for (int chainIndex = 0; chainIndex < ribbons.Length; chainIndex++)
            {
                var chain = ribbons[chainIndex]; if (chain == null || chain.bind == null) continue;
                for (int i = 0; i < chain.bones.Length; i++)
                {
                    float tip = (i + 1f) / chain.bones.Length;
                    float turn = move == KnightMove.OrbitSweep && state == BossEncounter.State.Attack ? -12 * tip : 0;
                    Quaternion target = chain.bind[i] * Quaternion.Euler(Mathf.Sin(clock * 2.2f - i * .36f + chainIndex) * 2.2f * tip, turn, Mathf.Sin(clock * 1.3f - i * .28f + chainIndex) * 1.4f * tip);
                    chain.bones[i].localRotation = Quaternion.Slerp(chain.bones[i].localRotation, target, 1 - Mathf.Exp(-7 * dt));
                }
            }
            if (environment) environment.Present(encounter.Phase, clock);
        }

        static Vector3 ReachCorrection(Transform upper, Transform middle, Transform tip, Vector3 target)
        {
            float length = Vector3.Distance(upper.position, middle.position) + Vector3.Distance(middle.position, tip.position) - .018f;
            Vector3 offset = target - upper.position;
            return offset.magnitude > length ? upper.position + offset.normalized * length - target : Vector3.zero;
        }
        void SolveArm(Transform upper, Transform middle, Transform tip, Vector3 target, float side, float weight)
        {
            Vector3 pole = upper.position + transform.right * side + Vector3.down * .35f - transform.forward * .45f;
            SolveTwoBone(upper, middle, tip, Vector3.Lerp(tip.position, target, weight), pole);
        }
        public static void SolveTwoBone(Transform upper, Transform middle, Transform tip, Vector3 target, Vector3 pole)
        {
            Vector3 origin = upper.position;
            float a = Vector3.Distance(origin, middle.position), b = Vector3.Distance(middle.position, tip.position);
            Vector3 direction = target - origin;
            float d = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .002f, a + b - .002f);
            if (a < .001f || b < .001f || direction.sqrMagnitude < .00001f) return;
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(pole - origin, direction).normalized;
            if (bend.sqrMagnitude < .1f) bend = Vector3.Cross(direction, Vector3.right).normalized;
            float along = (a * a - b * b + d * d) / (2 * d);
            Vector3 elbow = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(middle.position - origin, elbow - origin) * upper.rotation;
            middle.rotation = Quaternion.FromToRotation(tip.position - middle.position, origin + direction * d - middle.position) * middle.rotation;
        }

        void ApplyFeet(BossEncounter encounter, float dt)
        {
            GroundedFeet = 0; FootGroundError = 0;
            if (!leftFoot || !rightFoot) return;
            Vector3 travel = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
            previousPosition = transform.position; distance += travel.magnitude;
            if(travel.sqrMagnitude>.75f*.75f)planted=false;
            bool airborne = transform.position.y > encounter.Arena.transform.position.y + .025f;
            bool spinning = encounter.KnightCurrentMove == KnightMove.OrbitSweep && encounter.CurrentState == BossEncounter.State.Attack;
            if (airborne || spinning) { planted = false; return; }
            if (!planted) { leftPlant = leftFoot.position; rightPlant = rightFoot.position; planted = true; }
            bool moving = travel.magnitude > .002f;
            Plant(encounter, visual.leftThigh, visual.leftShin, leftFoot, ref leftPlant, moving, Mathf.Sin(distance * 3.4f), dt);
            Plant(encounter, visual.rightThigh, visual.rightShin, rightFoot, ref rightPlant, moving, -Mathf.Sin(distance * 3.4f), dt);
        }
        void Plant(BossEncounter encounter, Transform thigh, Transform shin, Transform foot, ref Vector3 plant, bool moving, float phase, float dt)
        {
            if(Vector3.ProjectOnPlane(foot.position-plant,Vector3.up).magnitude>.42f)plant=foot.position;
            else if (moving && phase > 0)
                plant = Vector3.Lerp(plant, foot.position, Mathf.Clamp01(dt * 18));
            Vector3 origin = new Vector3(plant.x, encounter.Arena.transform.position.y + 2, plant.z);
            float highest = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<FirstPersonController>()) continue;
                if (hit.normal.y > .75f && hit.point.y > highest) highest = hit.point.y;
            }
            if (float.IsNegativeInfinity(highest)) return;
            float lift = moving ? Mathf.Max(0, phase) * .095f : 0;
            Vector3 target = new Vector3(plant.x, highest + soleOffset + lift, plant.z);
            float length=Vector3.Distance(thigh.position,shin.position)+Vector3.Distance(shin.position,foot.position)-.004f;
            float vertical=thigh.position.y-target.y;
            float reach=Mathf.Sqrt(Mathf.Max(0,length*length-vertical*vertical));
            Vector3 planar=Vector3.ProjectOnPlane(target-thigh.position,Vector3.up);
            if(planar.magnitude>reach)
            {
                planar=Vector3.ClampMagnitude(planar,Mathf.Max(0,reach-.002f));
                target.x=thigh.position.x+planar.x;target.z=thigh.position.z+planar.z;
                plant.x=target.x;plant.z=target.z;
            }
            for(int iteration=0;iteration<3;iteration++)SolveTwoBone(thigh, shin, foot, target, thigh.position + transform.forward * 2);
            foot.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            GroundedFeet++;
            FootGroundError = Mathf.Max(FootGroundError, Mathf.Abs(foot.position.y - target.y));
        }
    }
}
