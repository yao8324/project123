using UnityEngine;
using ArcaneArchive.Core;

namespace ArcaneArchive.Visuals
{
    /// <summary>Encounter-facing adapter for the production skeletal model.</summary>
    public sealed class WardenVisuals : MonoBehaviour
    {
        public Transform floatingBody, mask, leftArm, rightArm;
        public WardenAnimationDriver animatedRig;
        public Vector3 CastOrigin => animatedRig.castingSocket.position + transform.forward * .25f;
        public void ResetPose() { floatingBody.localScale = Vector3.one; animatedRig.ResetPose(); }
        public void Present(float dt, int phase, int attack, BossEncounter.State state, float elapsed, float warningDuration, float recoveryDuration=1.15f)
        { animatedRig.Present(dt, phase, attack, state, elapsed, warningDuration, recoveryDuration); }
    }
}
