using ArcaneArchive.Core;
using ArcaneArchive.Visuals;
using UnityEngine;

namespace ArcaneArchive.Player
{
    // Movement is stepped by FirstPersonController. Only a verified near miss owns slow time.
    [DisallowMultipleComponent]
    public sealed class PlayerDash : MonoBehaviour
    {
        public const float Distance = 4.8f, Duration = .24f, Cost = 20f, Cooldown = .85f;
        public const float PerfectWindow = .22f, SlowDuration = .75f, SlowHold = .45f;
        public const float WorldSlowScale = .05f, RewardProtection = .30f;
        FirstPersonController player;
        CharacterController capsule;
        PlayerMelee melee;
        PlayerCombat combat;
        BossEncounter encounter;
        DashAfterimageVfx effects;
        TemporalStopPulse stopPulse;
        DodgeWorldEchoes worldEchoes;
        readonly Vector3[] path = new Vector3[128];
        readonly float[] pathLengths = new float[128];
        int pathCount;
        float pathLength;
        float age = 10, fraction, cooldown, slowAge, previousScale, previousFixed, appliedScale, appliedFixed;
        bool moving, eligible, candidate, rewarded, airUsed, ownsTime;
        Vector3 direction;
        public Vector3 StartPosition { get; private set; }
        public Vector3 StartEye { get; private set; }
        public Vector3 CapsuleLower { get; private set; }
        public Vector3 CapsuleUpper { get; private set; }
        public float CapsuleRadius { get; private set; }
        public float CooldownRemaining => cooldown;
        public bool IsDashing => moving;
        public bool SlowActive => ownsTime;
        public float RewardAge => slowAge;
        public bool RewardProtected => ownsTime && slowAge < RewardProtection;
        public float PlayerDeltaTime => ownsTime ? Time.unscaledDeltaTime * previousScale : Time.deltaTime;
        public float MotionWeight => moving ? Mathf.Sin(Mathf.Clamp01(age / Duration) * Mathf.PI) : 0;
        public float RewardWeight => ownsTime ? 1 - Mathf.SmoothStep(0, 1, slowAge / SlowDuration) : 0;
        public Vector3 Direction => direction;
        public uint DashSequence { get; private set; }
        public uint PerfectSequence { get; private set; }
        public bool CanCheckPerfect => eligible && !rewarded && age > 0 && age <= PerfectWindow &&
            player && player.GameplayInputEnabled && !player.SimulationPaused &&
            (!combat || (!combat.Guarding && !combat.Absorbing && !combat.GuardBroken)) &&
            (Vector3.ProjectOnPlane(transform.position - StartPosition, Vector3.up).sqrMagnitude >= .36f);

        public void Initialize(FirstPersonController owner, CharacterController controller)
        { player = owner; capsule = controller; }
        void Resolve()
        {
            if(!melee)melee = GetComponent<PlayerMelee>();
            if(!combat)combat = GetComponent<PlayerCombat>();
            if(!encounter)encounter = GetComponent<BossEncounter>();
        }
        public bool TryStart(Vector2 input)
        {
            Resolve();
            if(!isActiveAndEnabled || !player || !capsule || !capsule.enabled || !player.GameplayInputEnabled ||
                player.SimulationPaused || cooldown > 0 || moving || ownsTime ||
                (encounter && encounter.IsActive && !encounter.CanFight) ||
                (melee && melee.Busy && melee.Action == 3) ||
                (combat && combat.GuardBroken) || (!player.IsGrounded && airUsed))return false;
            if(!melee || !melee.loadout || !melee.TrySpendStamina(Cost))return false;
            direction = Vector3.ProjectOnPlane(transform.right * input.x + transform.forward * input.y, Vector3.up);
            if(direction.sqrMagnitude < .01f)direction = transform.forward;
            direction.Normalize();
            StartPosition = transform.position;
            path[0]=StartPosition;pathLengths[0]=pathLength=0;pathCount=1;
            var eye = melee.view ? melee.view.viewCamera : GetComponentInChildren<Camera>();
            StartEye = eye ? eye.transform.position : StartPosition + Vector3.up * 1.6f;
            CapsuleRadius = capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            Vector3 center = transform.TransformPoint(capsule.center);
            float half = Mathf.Max(0, capsule.height * Mathf.Abs(transform.lossyScale.y) * .5f - CapsuleRadius);
            CapsuleLower = center - Vector3.up * half; CapsuleUpper = center + Vector3.up * half;
            age = fraction = 0; cooldown = Cooldown; moving = true; rewarded = candidate = false;
            eligible = encounter && encounter.CanFight && !combat.Guarding && !combat.Absorbing && !melee.PracticeGuarding;
            airUsed = !player.IsGrounded;
            DashSequence++;
            var sound = GetComponent<MeleeFeedback>(); if(sound)sound.Dodge(false);
            return true;
        }
        public void Tick(float realDt)
        {
            if(!player || player.SimulationPaused || !player.GameplayInputEnabled ||
                (encounter && encounter.IsFinished)){Cancel(false);return;}
            float dt = Mathf.Max(0, realDt);
            cooldown = Mathf.Max(0, cooldown - dt); age += dt;
            if(player.IsGrounded && !moving)airUsed = false;
            if(age > PerfectWindow){eligible = candidate = false;}
            if(!ownsTime)return;
            slowAge += dt;
            if(slowAge >= SlowDuration){RestoreTime();return;}
            ApplyTime();
        }
        // Use controller sweeps rather than teleportation, including on long render frames.
        public bool Move(out CollisionFlags flags)
        {
            flags = CollisionFlags.None;
            if(!moving || !capsule || !capsule.enabled)return false;
            float next = 1 - Mathf.Pow(1 - Mathf.Clamp01(age / Duration), 2);
            float remaining = Mathf.Max(0, next - fraction) * Distance; fraction = next;
            while(remaining > .00001f)
            {
                float step = Mathf.Min(.18f, remaining);
                Vector3 before = transform.position;
                CollisionFlags contact = capsule.Move(direction * step); flags |= contact;
                RecordPath();
                remaining -= step;
                if((contact & CollisionFlags.Sides) != 0 && Vector3.Dot(transform.position - before, direction) < step * .7f)
                { moving = false; break; }
            }
            if(fraction >= 1)moving = false;
            return true;
        }
        void RecordPath()
        {
            if(pathCount==0)return;
            float segment=Vector3.Distance(path[pathCount-1],transform.position);
            if(segment<.002f)return;
            pathLength+=segment;
            int index=Mathf.Min(pathCount,path.Length-1);path[index]=transform.position;pathLengths[index]=pathLength;
            pathCount=Mathf.Min(pathCount+1,path.Length);
        }
        public Vector3 SamplePath(float progress)
        {
            if(pathCount==0)return transform.position;
            float distance=Mathf.Clamp01(progress)*pathLength;
            for(int i=1;i<pathCount;i++)if(pathLengths[i]>=distance)
                return Vector3.Lerp(path[i-1],path[i],Mathf.InverseLerp(pathLengths[i-1],pathLengths[i],distance));
            return path[pathCount-1];
        }
        public void ConsiderNearMiss(bool threatenedOrigin, bool hitCurrent)
        { if(threatenedOrigin && !hitCurrent && CanCheckPerfect)candidate = true; }
        // A block, absorption or any hit attempt in the same frame invalidates the reward.
        public void RegisterContact() { eligible = candidate = false; }
        public void ConfirmPerfect()
        {
            if(!candidate || !CanCheckPerfect)return;
            candidate = false; eligible = false; rewarded = true; PerfectSequence++;
            previousScale = Time.timeScale; previousFixed = Time.fixedDeltaTime;
            if(previousScale <= 0)return;
            ownsTime = true; slowAge = 0; ApplyTime();
            if(!effects)
            {
                effects = gameObject.AddComponent<DashAfterimageVfx>();
                effects.Initialize(player, melee.view, melee.loadout.goldEffect);
            }
            effects.Trigger();
            if(!stopPulse)
            {
                stopPulse = gameObject.AddComponent<TemporalStopPulse>();
                stopPulse.Initialize(this,melee.loadout.goldEffect);
            }
            stopPulse.Trigger(transform.position);
            if(!worldEchoes)
            {
                worldEchoes=gameObject.AddComponent<DodgeWorldEchoes>();
                worldEchoes.Initialize(player,melee.view,melee.loadout.goldEffect);
            }
            worldEchoes.Trigger();
            var sound = GetComponent<MeleeFeedback>(); if(sound)sound.Dodge(true);
            if(encounter)encounter.Feedback("极限闪避 · 时间减缓", .9f);
        }
        void ApplyTime()
        {
            float recovery = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(SlowHold, SlowDuration, slowAge));
            appliedScale = previousScale * Mathf.Lerp(WorldSlowScale, 1, recovery);
            appliedFixed = previousFixed * appliedScale / previousScale;
            Time.timeScale = appliedScale; Time.fixedDeltaTime = appliedFixed;
        }
        void RestoreTime()
        {
            if(!ownsTime)return;
            // Respect another owner if it has deliberately replaced our values.
            if(Mathf.Abs(Time.timeScale - appliedScale) < .0001f)Time.timeScale = previousScale;
            if(Mathf.Abs(Time.fixedDeltaTime - appliedFixed) < .00001f)Time.fixedDeltaTime = previousFixed;
            ownsTime = false;
            if(stopPulse)stopPulse.Clear();
        }
        public void Cancel(bool resetCooldown)
        {
            moving = eligible = candidate = false; age = 10; fraction = 1;
            RestoreTime(); if(effects)effects.Clear();if(stopPulse)stopPulse.Clear();if(worldEchoes)worldEchoes.Clear();
            if(resetCooldown){cooldown = 0; airUsed = false;}
        }
        void OnApplicationFocus(bool focused){if(!focused)Cancel(false);}
        void OnDisable(){Cancel(false);}
        void OnDestroy(){RestoreTime();}
    }
}
