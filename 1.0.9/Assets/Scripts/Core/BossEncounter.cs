using System.Collections.Generic;
using System.Linq;
using ArcaneArchive.Player;
using ArcaneArchive.UI;
using ArcaneArchive.Visuals;
using TMPro;
using UnityEngine;

namespace ArcaneArchive.Core
{
    [DisallowMultipleComponent]
    public sealed partial class BossEncounter : MonoBehaviour
    {
        public enum State { Inactive, Intro, Observe, Telegraph, Attack, Recovery, PhaseBreak, Won, Lost, Stagger, Retreat }
        public BossDefinition definition;
        public BossArena arenaPrefab;
        public GameObject libraryEnvironment;
        public TMP_FontAsset combatFont;
        private FirstPersonController player;
        private RecoveryMenu menu;
        private PlayerCombat combat;
        private BossArena arena;
        private BossCombatHUD hud;
        private AudioSource audioSource;
        private Vector3 returnPosition;
        private Quaternion returnRotation;
        private bool libraryWasActive, focusLost;
        private float stateTime, combatTime, messageTime;
        private int attack, pattern, emitted;
        private string message;
        private Vector3 lockTarget;
        private readonly List<Shot> shots = new List<Shot>();
        private readonly List<Wave> waves = new List<Wave>();
        private readonly List<Zone> zones = new List<Zone>();
        private readonly List<Flash> flashes = new List<Flash>();
        private GameObject warningRing, absorbVisual;
        private LaserBeamVisuals laser;
        private Vector3 laserOrigin,laserDirection;
        private float nextLaserTick;
        private int orbitDirection=1;
        private sealed class Shot { public GameObject go; public Vector3 velocity; public float damage, life, age, homingUsed, knightFanOffset; public bool enemy,homing,knightBarrage; }
        private sealed class Wave { public GameObject go; public Vector3 origin; public float radius; public bool hit; public WardenWaveVisuals visual; }
        private sealed class Zone { public GameObject go; public GroundFractureVisuals fracture; public Vector3 position; public float born; public bool fired; }
        private bool fractureSequenceStarted;
        private float nextFractureSpawn;
        public int FractureSpawnCount { get; private set; }
        public int FractureHitCount { get; private set; }
        public float RecoveryDuration => KnightMode && knightSpec!=null ? knightSpec.recovery : attack==5 ? definition.meleeRecovery : attack==4 ? definition.rushRecovery : attack>=2 ? definition.heavyRecovery : definition.standardRecovery;
        private sealed class Flash { public GameObject go; public float life, total; public Vector3 scale; }
        public BossDefinition Definition => definition;
        public State CurrentState { get; private set; }
        public bool IsActive => CurrentState != State.Inactive;
        public bool IsFinished => CurrentState == State.Won || CurrentState == State.Lost;
        public bool IsPaused => IsActive && (KnightStoryPending || focusLost || (menu && menu.IsOpen));
        public bool CanFight => IsActive && !IsFinished && !IsPaused && CurrentState != State.Intro;
        public Camera View { get; private set; }
        public float BossHealth { get; private set; }
        public int Phase { get; private set; }
        public int ActiveAttackCount => shots.Count + waves.Count + zones.Count + (laser?1:0) + (rushWarningVisual?1:0) + (volleyCrown?1:0) + (knightShockwave?1:0);
        public int CurrentAttack => attack;
        public float AttackElapsed => stateTime;
        public bool LaserIsFiring => attack==3 && CurrentState==State.Attack && laser;
        public Vector3 LaserOrigin => laserOrigin;
        public Vector3 LaserDirection => laserDirection;
        public float LaserLength { get; private set; }
        public float BossDistanceTravelled { get; private set; }
        public PlayerCombat Combat => combat;
        public BossArena Arena => arena;
        public float BattleTime => combatTime;

        private void Awake()
        {
            player = GetComponent<FirstPersonController>(); menu = GetComponent<RecoveryMenu>();
            combat = GetComponent<PlayerCombat>(); View = GetComponentInChildren<Camera>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
        }
        public bool Begin()
        {
            if (IsActive) return false;
            if (!definition || !arenaPrefab || !combatFont || !player || !combat || !View || !libraryEnvironment) return false;
            if (!KnightMode && (!arenaPrefab.laserEffect || !arenaPrefab.fractureEffect || !arenaPrefab.rushEffect || !arenaPrefab.volleyEffect || !arenaPrefab.rushWakeEffect || !arenaPrefab.pageImpactEffect)) return false;
            returnPosition = transform.position; returnRotation = transform.rotation;
            libraryWasActive = libraryEnvironment.activeSelf;
            arena = Instantiate(arenaPrefab, new Vector3(160, 0, 0), Quaternion.identity);
            arena.name = "BossEncounterRoot";
            Physics.SyncTransforms();
            if (!player.TryReturnToSpawn(arena.spawn.position, arena.spawn.rotation))
            { Destroy(arena.gameObject); arena = null; return false; }
            libraryEnvironment.SetActive(false);
            hud = gameObject.AddComponent<BossCombatHUD>(); hud.Initialize(this, combatFont);
            ResetBattle();
            return true;
        }
        private void ResetBattle()
        {
            ClearAttacks(); Phase = 1; BossHealth = definition.health; combatTime = 0; pattern = 0;
            BossDistanceTravelled=0;orbitDirection=1;
            FractureSpawnCount=FractureHitCount=0;FracturePathLength=FractureTotalTurn=0;
            combat.ResetCombat(this); focusLost = false;
            arena.boss.localPosition = arena.bossSpawn ? arena.transform.InverseTransformPoint(arena.bossSpawn.position) : new Vector3(0, 0, 4);
            arena.boss.localScale=Vector3.one*definition.bossScale;
            arena.boss.localRotation = Quaternion.Euler(0, 180, 0);
            if(!KnightMode)arena.model.floatingBody.localScale = Vector3.one;
            ResetIntent();ResetMelee();
            if(KnightMode)ResetKnight();else arena.model.ResetPose();
            SetState(State.Intro);
            menu.Resume(); player.SetGameplayInputEnabled(true); player.SetCursorLocked(true);
            player.WaitForReleasedControls(); combat.SuppressUntilReleased();
            if(!KnightMode)Feedback("归档编号零。你不该醒来。", 3);
        }
        public void Retry()
        {
            if (!IsActive || !IsFinished) return;
            if (player.TryReturnToSpawn(arena.spawn.position, arena.spawn.rotation)) ResetBattle();
        }
        public bool ReturnToLibrary()
        {
            if (!IsActive) return false;
            libraryEnvironment.SetActive(libraryWasActive); Physics.SyncTransforms();
            bool returned = player.TryReturnToSpawn(returnPosition, returnRotation) ||
                            player.TryReturnToSpawn(new Vector3(0, .08f, 7.8f), Quaternion.identity);
            if (!returned) { libraryEnvironment.SetActive(false); Feedback("返回位置被阻挡，请稍后重试", 3); return false; }
            ClearAttacks(); CurrentState = State.Inactive;combat.EndCombat();
            if (hud) { hud.Hide(); Destroy(hud); }
            if (arena) { arena.gameObject.SetActive(false); Destroy(arena.gameObject); arena = null; }
            RestoreKnightSession();
            menu.Resume(); player.SetGameplayInputEnabled(true); player.SetCursorLocked(true); player.WaitForReleasedControls();
            return true;
        }
        private void Update()
        {
            if (!IsActive || !arena) return;
            if (hud) hud.Refresh();
            float dt = Mathf.Min(Time.deltaTime, .1f);
            if (IsFinished) { PresentBoss(dt); return; }
            if (IsPaused) { combat.SuppressUntilReleased(); return; }
            if (messageTime > 0) { messageTime -= dt; if (messageTime <= 0) message = ""; }
            combatTime += dt;
            // Controls use one Input System. Combat never samples the legacy Input API.
            combat.Tick(Mathf.Min(player.PlayerDeltaTime,.1f));
            if (IsFinished || KnightStoryPending) return;
            TickBoss(dt);
            if(IsFinished)return;
            Physics.SyncTransforms();TickShots(dt);
            if (IsFinished) return;
            TickFractures(); if(IsFinished)return;
            TickWaveCharge(); TickWaves(dt); if(IsFinished)return;
            TickFlashes(dt); UpdateAbsorbVisual();
            PresentBoss(dt);
            TickRitualEffects();
            if(ActiveDodge)ActiveDodge.ConfirmPerfect();
            if (transform.position.y < -3) Finish(false);
        }
        private void TickBoss(float dt)
        {
            if(KnightMode){TickKnight(dt);return;}
            stateTime += dt;
            UpdatePerception(dt);
            switch (CurrentState)
            {
                case State.Intro: if (stateTime >= 2.2f) SetState(State.Observe); break;
                case State.Observe:
                    MoveBoss(dt,1);
                    if (stateTime >= definition.repositionTime || (stateTime>=.35f && MeleeInRange && combatTime>=nextMeleeAt)) BeginWarning();
                    break;
                case State.Retreat: TickVolleyRetreat(); break;
                case State.Telegraph:
                    float duration = WarningDuration;
                    if (stateTime < duration - AimLockDuration)
                    {
                        lockTarget = (attack==3 || attack==5 ? transform.position : lastKnownPlayer) + Vector3.up;
                        FacePoint(lockTarget,dt);
                        if (attack == 3) AimLaser();
                        if (attack == 4) AimRush();
                    }
                    if(attack==5)AdvanceMeleeWarning();
                    if(attack==3)TickLaser(false);
                    if(attack==2 && !fractureSequenceStarted && stateTime>=duration-definition.fractureLeadTime)
                    { StartFractureLine(); }
                    if(attack==4 && rushWarningVisual)rushWarningVisual.Present(rushOrigin,rushOrigin+rushDirection*rushLength,stateTime/duration);
                    if (warningRing) warningRing.transform.localScale = Vector3.one * Mathf.Lerp(3.0f, 1.2f, stateTime / duration);
                    if (stateTime >= duration) { if (warningRing) Destroy(warningRing); SetState(State.Attack); }
                    break;
                case State.Attack:
                    if (attack == 0)
                    {
                        int count = VolleyCount;
                        while (emitted < count && stateTime >= definition.volleyReleaseDelay + emitted * definition.volleyInterval)
                        { SpawnPage(emitted, count); emitted++; }
                        if (stateTime > 1.15f) SetState(State.Recovery);
                    }
                    else if (attack == 1)
                    {
                        int count = Phase == 1 ? 1 : 2;
                        if (emitted < count && stateTime >= .15f+emitted * 1.25f) { SpawnWave(); emitted++; }
                        if (stateTime > 1.1f + (Phase == 2 ? 1.25f : 0)) SetState(State.Recovery);
                    }
                    else if(attack==2)
                    {
                        // Per-pulse warnings, damage and cleanup use the battle clock below.
                    }
                    else if(attack==3)
                    {
                        TickLaser(true);
                        if(IsFinished)return;
                        if(stateTime>=definition.laserDuration){if(laser)Destroy(laser.gameObject);laser=null;SetState(State.Recovery);}
                    }
                    else if(attack==5)TickMeleeStrike();
                    else TickRush(dt);
                    break;
                case State.Recovery:
                    // Recovery intentionally exposes a flank: no automatic facing or pursuit.
                    // Existing projectiles finish independently; they must not freeze the animation state.
                    if (stateTime >= RecoveryDuration) SetState(State.Observe);
                    break;
                case State.Stagger: if(stateTime>=definition.staggerDuration)SetState(State.Recovery);break;
                case State.PhaseBreak: if (stateTime >= 2.2f) SetState(State.Observe); break;
            }
        }
        private void MoveBoss(float dt,float factor)
        {
            MoveTowardIntent(dt,factor);
        }
        private float WarningDuration => KnightMode && knightSpec!=null ? knightSpec.warning : attack==5 ? definition.meleeWarning : attack == 0 ? definition.volleyWarning : attack == 1 ? definition.waveWarning : attack==2 ? definition.fractureWarning : attack==3 ? definition.laserChargeTime : definition.rushWarning;
        private float AimLockDuration => attack==3?definition.laserLockTime:attack==4?definition.rushLockTime:attack==5?.24f:.55f;
        private void BeginWarning()
        {
            attack=SelectAttack();
            if(pattern%2==0)orbitDirection*=-1;
            emitted = 0; lockTarget = lastKnownPlayer + Vector3.up;
            if(attack==0){BeginVolleyRetreat();return;}
            SetState(State.Telegraph); PlaySound(definition.warningSound, .35f);
            Feedback(attack==5 ? "刃杖横斩 · 侧退或举盾弹反" : attack == 0 ? "禁页齐射 · 追踪，侧移或吞印" : attack == 1 ? "缄默环波 · 看准时机跳跃" : attack==2 ? "裂地禁咒 · 沿地追踪，侧移躲避" : attack==3 ? "湮灭秘光 · 金线锁定后侧移" : "封缄冲撞 · 路线锁定后侧向躲避", attack==2?7:WarningDuration);
            if(attack==5)BeginMeleeWarning();
            else if (attack == 0) BeginVolleyCrown();
            else if (attack == 1) BeginWaveCharge();
            else if (attack == 2)
            {
                FractureSpawnCount=FractureHitCount=0; fractureSequenceStarted=false;
            }
            else if(attack==3)
            {
                laser=Instantiate(arena.laserEffect,arena.effectRoot);nextLaserTick=0;AimLaser();TickLaser(false);
            }
            else if(attack==4){rushWarningVisual=Instantiate(arena.rushEffect,arena.effectRoot);AimRush();}
            else warningRing = arena.Effect(arena.ringEffect, arena.boss.position + Vector3.up * .04f, Quaternion.identity);
        }
        private void AimLaser()
        {
            laserOrigin=arena.boss.TransformPoint(new Vector3(0,2.6f,.92f));
            laserDirection=(View.transform.position-laserOrigin).normalized;
        }
        private void TickLaser(bool firing)
        {
            if(!laser)return;
            Physics.SyncTransforms();
            float distance=definition.laserRange;bool hitPlayer=false;
            var hits=Physics.SphereCastAll(laserOrigin,definition.laserRadius,laserDirection,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            foreach(var hit in hits)
            {
                if(hit.collider.transform.IsChildOf(arena.boss))continue;
                distance=hit.distance;hitPlayer=hit.collider.transform.IsChildOf(transform);break;
            }
            LaserLength=distance;
            float visibleDistance=distance;
            if(hitPlayer && combat.Guarding && Vector3.Dot(View.transform.forward,(laserOrigin-View.transform.position).normalized)>=.5f)
                visibleDistance=Mathf.Min(distance,Mathf.Max(0,Vector3.Dot(View.transform.position-laserOrigin,laserDirection)-1.1f));
            laser.Present(laserOrigin,laserOrigin+laserDirection*visibleDistance,Mathf.Clamp01(stateTime/definition.laserChargeTime),firing,definition.laserRadius,combatTime,firing || stateTime>=definition.laserChargeTime-definition.laserLockTime);
            if(firing && stateTime>=nextLaserTick)
            {
                nextLaserTick=stateTime+definition.laserTickInterval;
                // Every damaging tick can reward a new, genuinely successful dodge.
                if(!hitPlayer && CheckingDodge)
                    CheckDodge(SegmentThreatensDodge(laserOrigin,laserOrigin+laserDirection*distance,definition.laserRadius),false);
                if(hitPlayer)combat.ReceiveHit(definition.laserDamage,laserOrigin,true,definition.laserTickInterval*.9f);
            }
        }
        private void SpawnPage(int index, int count)
        {
            if (!volleyCrown) return;
            var page = volleyCrown.Release(index, arena.effectRoot);
            if (!page) return;
            Vector3 start = page.transform.position;
            // Sample the current player position for each released page; the crown's earlier
            // pose target is presentation only and can be stale after the retreat.
            Vector3 target=transform.position+Vector3.up*1.2f;
            Vector3 side = Vector3.Cross(Vector3.up, (target - arena.boss.position).normalized).normalized;
            Vector3 aim = target + side * (index - (count - 1) * .5f) * .42f;
            Vector3 direction = (aim - start).normalized;
            page.transform.rotation = Quaternion.LookRotation(direction);
            var motion = page.GetComponent<WardenSpellMotion>();
            if (motion) motion.Launch();
            // Keep the former 30 / 50 total volley damage while increasing the page count.
            shots.Add(new Shot { go = page, velocity = direction * definition.projectileSpeed,
                damage = definition.volleyDamage * (Phase == 1 ? 3f : 5f) / count, enemy = true, life = 4, homing = true });
            VolleyReleasedCount++;
            PlaySound(definition.shotSound, .16f);
        }
        public void FirePlayerBolt(float damage, bool empowered)
        {
            var start = View.transform.position + View.transform.forward * .55f;
            var go = arena.Effect(arena.boltEffect, start, View.transform.rotation);
            go.transform.localScale *= empowered ? 1.7f : 1;
            var toBoss=arena.boss.TransformPoint(new Vector3(0,2.55f,.88f))-start;
            bool acquire=Vector3.Angle(View.transform.forward,toBoss)<=definition.playerAcquireAngle && HasClearShot(start,start+toBoss);
            shots.Add(new Shot { go = go, velocity = View.transform.forward * 30, damage = damage, life = 1.5f,homing=acquire });
            PlaySound(definition.shotSound, empowered ? .5f : .24f);
        }
        private void TickShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (i >= shots.Count) continue;
                var s = shots[i]; if (!s.go) { shots.RemoveAt(i); continue; }
                SteerProjectile(s,dt);
                Vector3 from = s.go.transform.position, movement = s.velocity * dt;
                Vector3 dodgeEnd=from+movement;
                bool consumed = false;
                // Segment closest point makes the short absorption window work at low frame rates.
                if (s.enemy)
                {
                    float t = Mathf.Clamp01(Vector3.Dot(View.transform.position - from, movement) / Mathf.Max(.0001f, movement.sqrMagnitude));
                    if (combat.CanAbsorb(from + movement * t))
                    { if(ActiveDodge)ActiveDodge.RegisterContact(); combat.AbsorbHit(); consumed = true; PageImpact(s, from + movement * t, true); }
                }
                if (!consumed)
                {
                    var hits = Physics.SphereCastAll(from, s.enemy ? .13f : .09f, s.velocity.normalized, movement.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide).OrderBy(h => h.distance);
                    foreach (var h in hits)
                    {
                        bool hitPlayer = h.collider.transform.IsChildOf(transform);
                        bool hitBoss = h.collider.transform.IsChildOf(arena.boss);
                        if ((s.enemy && hitBoss) || (!s.enemy && hitPlayer)) continue;
                        if (h.collider.isTrigger && !hitBoss) continue;
                        consumed = true;
                        // Keep the actual swept path up to the first collision, including a wall hit later in this step.
                        dodgeEnd=from+s.velocity.normalized*h.distance;
                        PageImpact(s, h.point, false);
                        if (s.enemy && hitPlayer) combat.ReceiveHit(s.damage,from,true);
                        else if (!s.enemy && hitBoss) DamageBoss(s.damage, h.collider.name == "CoreHitbox");
                        break;
                    }
                }
                if(s.enemy && CheckingDodge)
                    CheckDodge(SegmentThreatensDodge(from,dodgeEnd,.13f),false);
                // Damage can finish the fight or start phase transition and clear this list.
                if (i >= shots.Count || shots[i] != s) break;
                s.life -= dt;
                if (consumed || s.life <= 0) { Destroy(s.go); shots.RemoveAt(i); }
                else
                {
                    s.go.transform.position += movement;
                    if(s.knightBarrage){var presentation=s.go.GetComponent<ArcaneArchive.Visuals.KnightBoltVisual>();if(presentation)presentation.Present();}
                }
            }
        }
        private void SpawnWave()
        {
            var origin = arena.boss.position; origin.y = arena.transform.position.y + .06f;
            var visual=CreateWaveVisual(false);visual.PresentWave(origin,.8f,combatTime);
            waves.Add(new Wave { origin = origin, radius = .8f, go = visual.gameObject, visual=visual });
        }
        private void TickWaves(float dt)
        {
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                if (i >= waves.Count) continue;
                var wave = waves[i]; float previous = wave.radius;
                wave.radius += dt * definition.waveSpeed;
                wave.visual.PresentWave(wave.origin,wave.radius,combatTime);
                float distance = HorizontalDistance(transform.position, wave.origin);
                bool inWave = distance >= previous - .45f && distance <= wave.radius + .45f && transform.position.y < wave.origin.y + .50f;
                if(!wave.hit && !inWave && CheckingDodge)
                {
                    float oldDistance=HorizontalDistance(ActiveDodge.StartPosition,wave.origin);
                    CheckDodge(oldDistance>=previous-.45f && oldDistance<=wave.radius+.45f &&
                        ActiveDodge.StartPosition.y<wave.origin.y+.50f,false);
                }
                if (!wave.hit && inWave){ wave.hit = true; combat.TakeDamage(definition.waveDamage); }
                if (i >= waves.Count || waves[i] != wave) break;
                if (wave.radius > 24) { Destroy(wave.go); waves.RemoveAt(i); }
            }
        }
        public void DamageBoss(float damage, bool core)
        {
            if (!CanFight || CurrentState == State.PhaseBreak || damage <= 0) return;
            if(KnightMode){DamageKnight(damage,core);return;}
            BossHealth = Mathf.Max(0, BossHealth - damage * (core && CurrentState == State.Recovery ? 1.5f : 1));
            if (BossHealth <= 0) { Finish(true); return; }
            if (Phase == 1 && BossHealth <= definition.health * .5f)
            { ClearAttacks(); Phase = 2; SetState(State.PhaseBreak); Feedback("封印破裂 · 它认出了你", 2.5f); }
        }
        public void Finish(bool won)
        {
            if (!IsActive || IsFinished) return;
            ClearAttacks(); SetState(won ? State.Won : State.Lost);
            player.SetGameplayInputEnabled(false); menu.ShowPage(RecoveryMenu.Page.BattleResult);
        }
        private void SetState(State value)
        {
            CurrentState=value;stateTime=0;
            if(value==State.Observe){UpdatePerception(0);ChooseMoveIntent();}
        }
        private static float HorizontalDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private void FlashEffect(GameObject prefab, Vector3 position, Vector3 scale, float lifetime)
        {
            var go = arena.Effect(prefab, position, Quaternion.identity); go.transform.localScale = scale;
            flashes.Add(new Flash { go = go, life = lifetime, total = lifetime, scale = scale });
        }
        private void TickFlashes(float dt)
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i]; f.life -= dt;
                if (f.life <= 0) { if (f.go) Destroy(f.go); flashes.RemoveAt(i); }
                else if (f.go) f.go.transform.localScale = f.scale * (.5f + f.life / f.total);
            }
        }
        private void UpdateAbsorbVisual()
        {
            if (combat.Absorbing || combat.Guarding)
            {
                if (!absorbVisual) absorbVisual = arena.Effect(arena.ringEffect, Vector3.zero, Quaternion.identity);
                absorbVisual.transform.SetPositionAndRotation(View.transform.position + View.transform.forward * 1.1f,
                    View.transform.rotation * Quaternion.Euler(90, 0, 0));
                absorbVisual.transform.localScale = Vector3.one * (combat.Absorbing ? .45f : .37f);
            }
            else if (absorbVisual) Destroy(absorbVisual);
        }
        private void ClearAttacks()
        {
            ClearKnightShockwave();
            ClearKnightTelegraph();
            ClearMeleeVisual();
            ClearWaveCharge();
            ClearRitualEffects();
            foreach (var s in shots) if (s.go) Destroy(s.go);
            foreach (var w in waves) if (w.go) Destroy(w.go);
            foreach (var z in zones) if (z.go) Destroy(z.go);
            foreach (var f in flashes) if (f.go) Destroy(f.go);
            shots.Clear(); waves.Clear(); zones.Clear(); flashes.Clear();
            fractureSequenceStarted=false;nextFractureSpawn=0;
            if (warningRing) Destroy(warningRing); if (absorbVisual) Destroy(absorbVisual);
            if(laser)Destroy(laser.gameObject);laser=null;LaserLength=0;
            if(rushWarningVisual)Destroy(rushWarningVisual.gameObject);rushWarningVisual=null;ramTravel=0;
        }
        public void PlaySound(AudioClip clip, float volume) { if (clip && audioSource) audioSource.PlayOneShot(clip, volume); }
        public void Feedback(string value, float duration) { message = value; messageTime = duration; }
        public string Message => message;
        public void PlayerHurt() { if (hud) hud.Hurt(); PlaySound(definition.impactSound, .35f); }
        private void OnApplicationFocus(bool focused)
        {
            if (!IsActive || IsFinished) return;
            focusLost = !focused;
            if (!focused && menu && !menu.IsOpen) menu.Open();
        }
        private void OnDestroy()
        {
            if (libraryEnvironment && IsActive) libraryEnvironment.SetActive(libraryWasActive);
            if (arena) Destroy(arena.gameObject);
        }
    }
}
