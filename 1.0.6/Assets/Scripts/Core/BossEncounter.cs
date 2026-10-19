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
        public enum State { Inactive, Intro, Observe, Telegraph, Attack, Recovery, PhaseBreak, Won, Lost }
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
        private sealed class Shot { public GameObject go; public Vector3 velocity; public float damage, life, age, homingUsed; public bool enemy,homing; }
        private sealed class Wave { public GameObject go; public Vector3 origin; public float radius; public bool hit; }
        private sealed class Zone { public GameObject go; public GroundFractureVisuals fracture; public Vector3 position; public bool fired; }
        private sealed class Flash { public GameObject go; public float life, total; public Vector3 scale; }
        public BossDefinition Definition => definition;
        public State CurrentState { get; private set; }
        public bool IsActive => CurrentState != State.Inactive;
        public bool IsFinished => CurrentState == State.Won || CurrentState == State.Lost;
        public bool IsPaused => IsActive && (focusLost || (menu && menu.IsOpen));
        public bool CanFight => IsActive && !IsFinished && !IsPaused && CurrentState != State.Intro;
        public Camera View { get; private set; }
        public float BossHealth { get; private set; }
        public int Phase { get; private set; }
        public int ActiveAttackCount => shots.Count + waves.Count + zones.Count + (laser?1:0) + (rushWarningVisual?1:0);
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
            if (!definition || !arenaPrefab || !arenaPrefab.laserEffect || !arenaPrefab.fractureEffect || !arenaPrefab.rushEffect || !combatFont || !player || !combat || !View || !libraryEnvironment) return false;
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
            combat.ResetCombat(this); focusLost = false;
            arena.boss.localPosition = new Vector3(0, 0, 4);
            arena.boss.localRotation = Quaternion.Euler(0, 180, 0);
            arena.model.floatingBody.localScale = Vector3.one;
            ResetIntent();arena.model.ResetPose();
            SetState(State.Intro);
            menu.Resume(); player.SetGameplayInputEnabled(true); player.SetCursorLocked(true);
            player.WaitForReleasedControls(); combat.SuppressUntilReleased();
            Feedback("归档编号零。你不该醒来。", 3);
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
            ClearAttacks(); CurrentState = State.Inactive;
            if (hud) { hud.Hide(); Destroy(hud); }
            if (arena) { arena.gameObject.SetActive(false); Destroy(arena.gameObject); arena = null; }
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
            combat.Tick(dt);
            if (IsFinished) return;
            TickBoss(dt);
            if(IsFinished)return;
            Physics.SyncTransforms();TickShots(dt);
            if (IsFinished) return;
            TickWaves(dt); TickFlashes(dt); UpdateAbsorbVisual();
            PresentBoss(dt);
            if (transform.position.y < -3) Finish(false);
        }
        private void TickBoss(float dt)
        {
            stateTime += dt;
            UpdatePerception(dt);
            switch (CurrentState)
            {
                case State.Intro: if (stateTime >= 3) SetState(State.Observe); break;
                case State.Observe:
                    MoveBoss(dt,1);
                    if (stateTime >= definition.repositionTime) BeginWarning();
                    break;
                case State.Telegraph:
                    float duration = WarningDuration;
                    if (stateTime < duration - AimLockDuration)
                    {
                        lockTarget = lastKnownPlayer + Vector3.up;
                        FacePoint(lockTarget,dt);
                        if (attack == 2) PositionZones();
                        if (attack == 3) AimLaser();
                        if (attack == 4) AimRush();
                    }
                    if(attack==3)TickLaser(false);
                    if(attack==2)foreach(var zone in zones)zone.fracture.Present(stateTime/duration,-1);
                    if(attack==4 && rushWarningVisual)rushWarningVisual.Present(rushOrigin,rushOrigin+rushDirection*rushLength,stateTime/duration);
                    if (warningRing) warningRing.transform.localScale = Vector3.one * Mathf.Lerp(3.0f, 1.2f, stateTime / duration);
                    if (stateTime >= duration) { if (warningRing) Destroy(warningRing); SetState(State.Attack); }
                    break;
                case State.Attack:
                    if (attack == 0)
                    {
                        int count = Phase == 1 ? 3 : 5;
                        if (emitted < count && stateTime >= .22f+emitted * .18f) { SpawnPage(emitted, count); emitted++; }
                        if (stateTime > 1.5f) SetState(State.Recovery);
                    }
                    else if (attack == 1)
                    {
                        int count = Phase == 1 ? 1 : 2;
                        if (emitted < count && stateTime >= emitted * 1.4f) { SpawnWave(); emitted++; }
                        if (stateTime > 3.8f + (Phase == 2 ? 1.4f : 0)) SetState(State.Recovery);
                    }
                    else if(attack==2)
                    {
                        TickFractures();
                    }
                    else if(attack==3)
                    {
                        TrackLaser(dt);
                        TickLaser(true);
                        if(IsFinished)return;
                        if(stateTime>=definition.laserDuration){if(laser)Destroy(laser.gameObject);laser=null;SetState(State.Recovery);}
                    }
                    else TickRush(dt);
                    break;
                case State.Recovery:
                    // Recovery intentionally exposes a flank: no automatic facing or pursuit.
                    if (stateTime > (attack==4?definition.rushRecovery:attack >= 2 ? 2 : 1.5f) && shots.All(s => !s.enemy) && waves.Count==0) SetState(State.Observe);
                    break;
                case State.PhaseBreak: if (stateTime >= 2.5f) SetState(State.Observe); break;
            }
        }
        private void MoveBoss(float dt,float factor)
        {
            MoveTowardIntent(dt,factor);
        }
        private float WarningDuration => attack == 0 ? definition.volleyWarning : attack == 1 ? definition.waveWarning : attack==2 ? definition.fractureWarning : attack==3 ? definition.laserChargeTime : definition.rushWarning;
        private float AimLockDuration => attack==3?definition.laserLockTime:attack==4?definition.rushLockTime:.55f;
        private void BeginWarning()
        {
            attack=SelectAttack();
            if(pattern%2==0)orbitDirection*=-1;
            emitted = 0; lockTarget = lastKnownPlayer + Vector3.up;
            SetState(State.Telegraph); PlaySound(definition.warningSound, .35f);
            Feedback(attack == 0 ? "禁页齐射 · 弱追踪，侧移或吞印" : attack == 1 ? "缄默环波 · 看准时机跳跃" : attack==2 ? "裂地禁咒 · 离开紫红裂纹" : attack==3 ? "湮灭秘光 · 缓慢追踪，持续横移" : "封缄冲撞 · 路线锁定后侧向躲避", WarningDuration);
            if (attack == 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    var fracture=Instantiate(arena.fractureEffect,arena.effectRoot);
                    zones.Add(new Zone { go=fracture.gameObject,fracture=fracture });
                }
                PositionZones();
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
            laserDirection=(lockTarget+Vector3.up*.6f-laserOrigin).normalized;
        }
        private void TrackLaser(float dt)
        {
            if(stateTime<=definition.laserTrackingDelay || definition.laserTrackingSpeed<=0)return;
            Vector3 toPlayer=View.transform.position-laserOrigin;
            if(toPlayer.sqrMagnitude<.001f)return;
            // Angular speed is bounded: moving sideways can outrun the sweep.
            // The initial hold keeps the telegraph meaningful; no instant aim snap.
            float ramp=Mathf.Clamp01((stateTime-definition.laserTrackingDelay)/.25f);
            laserDirection=Vector3.RotateTowards(laserDirection,toPlayer.normalized,
                definition.laserTrackingSpeed*Mathf.Deg2Rad*ramp*dt,0).normalized;
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
            laser.Present(laserOrigin,laserOrigin+laserDirection*visibleDistance,Mathf.Clamp01(stateTime/definition.laserChargeTime),firing,definition.laserRadius,combatTime);
            if(firing && stateTime>=nextLaserTick)
            {
                nextLaserTick=stateTime+definition.laserTickInterval;
                if(hitPlayer)combat.ReceiveHit(definition.laserDamage,laserOrigin,true,definition.laserTickInterval*.9f);
            }
        }
        private void PositionZones()
        {
            for (int i = 0; i < zones.Count; i++)
            {
                Vector3 offset=i==0?Vector3.zero:i==1?new Vector3(-3.4f,0,.8f):new Vector3(2.8f,0,2.2f);
                var local = arena.transform.InverseTransformPoint(lockTarget) + offset;
                local.y = 0; local = Vector3.ClampMagnitude(local, 11.4f); local.y = .04f;
                zones[i].position = arena.transform.TransformPoint(local);
                zones[i].go.transform.position = zones[i].position;
                zones[i].go.transform.localRotation=Quaternion.Euler(0,i*73+pattern*21,0);
                zones[i].go.transform.localScale = new Vector3(definition.fractureRadius,1,definition.fractureRadius);
            }
        }
        private void SpawnPage(int index, int count)
        {
            Vector3 start = arena.model.rightArm.TransformPoint(new Vector3(0,-1.65f,.14f));
            Vector3 aim = lockTarget + View.transform.right * (index - (count - 1) * .5f) * .6f;
            Vector3 direction = (aim - start).normalized;
            shots.Add(new Shot { go = arena.Effect(arena.pageProjectile, start, Quaternion.LookRotation(direction)),
                velocity = direction * definition.projectileSpeed, damage = definition.volleyDamage, enemy = true, life = 6,homing=true });
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
                bool consumed = false;
                // Segment closest point makes the short absorption window work at low frame rates.
                if (s.enemy)
                {
                    float t = Mathf.Clamp01(Vector3.Dot(View.transform.position - from, movement) / Mathf.Max(.0001f, movement.sqrMagnitude));
                    if (combat.CanAbsorb(from + movement * t))
                    { combat.AbsorbHit(); consumed = true; FlashEffect(arena.impactEffect, from, Vector3.one * .6f, .3f); }
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
                        FlashEffect(arena.impactEffect, h.point, Vector3.one * .45f, .25f);
                        if (s.enemy && hitPlayer) combat.ReceiveHit(s.damage,from,true);
                        else if (!s.enemy && hitBoss) DamageBoss(s.damage, h.collider.name == "CoreHitbox");
                        break;
                    }
                }
                // Damage can finish the fight or start phase transition and clear this list.
                if (i >= shots.Count || shots[i] != s) break;
                s.life -= dt;
                if (consumed || s.life <= 0) { Destroy(s.go); shots.RemoveAt(i); }
                else s.go.transform.position += movement;
            }
        }
        private void SpawnWave()
        {
            var origin = arena.boss.position; origin.y = arena.transform.position.y + .06f;
            waves.Add(new Wave { origin = origin, radius = .8f, go = arena.Effect(arena.ringEffect, origin, Quaternion.identity) });
        }
        private void TickWaves(float dt)
        {
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                if (i >= waves.Count) continue;
                var wave = waves[i]; float previous = wave.radius;
                wave.radius += dt * definition.waveSpeed;
                wave.go.transform.localScale = new Vector3(wave.radius, 1, wave.radius);
                float distance = HorizontalDistance(transform.position, wave.origin);
                if (!wave.hit && distance >= previous - .45f && distance <= wave.radius + .45f && transform.position.y < wave.origin.y + .50f)
                { wave.hit = true; combat.TakeDamage(definition.waveDamage); }
                if (i >= waves.Count || waves[i] != wave) break;
                if (wave.radius > 24) { Destroy(wave.go); waves.RemoveAt(i); }
            }
        }
        public void DamageBoss(float damage, bool core)
        {
            if (!CanFight || CurrentState == State.PhaseBreak || damage <= 0) return;
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
            foreach (var s in shots) if (s.go) Destroy(s.go);
            foreach (var w in waves) if (w.go) Destroy(w.go);
            foreach (var z in zones) if (z.go) Destroy(z.go);
            foreach (var f in flashes) if (f.go) Destroy(f.go);
            shots.Clear(); waves.Clear(); zones.Clear(); flashes.Clear();
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
