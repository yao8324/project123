using ArcaneArchive.Core;
using UnityEngine;

namespace ArcaneArchive.Player
{
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        private FirstPersonController player;
        private BossEncounter encounter;
        private float shotCooldown, absorbCooldown, absorbRemaining, invulnerability;
        private bool waitForRelease;
        private float guardRegenDelay, guardBreakRemaining;
        private bool guardReleaseRequired;
        public float Health { get; private set; }
        public float Power { get; private set; }
        public float AbsorbCooldown => absorbCooldown;
        public bool Absorbing => absorbRemaining > 0;
        public float Guard { get; private set; }
        public bool Guarding { get; private set; }
        public bool GuardBroken => guardBreakRemaining > 0;
        public float GuardBreakRemaining => guardBreakRemaining;
        public bool GuardNeedsRelease => guardReleaseRequired;
        public void ResetCombat(BossEncounter owner)
        {
            player = GetComponent<FirstPersonController>(); encounter = owner;
            Health = owner.Definition.playerHealth; Power = 0;
            Guard=owner.Definition.guardCapacity; Guarding=false;guardReleaseRequired=false;
            guardRegenDelay=guardBreakRemaining=0;
            shotCooldown = absorbCooldown = absorbRemaining = invulnerability = 0;
            waitForRelease = true;
        }
        public void SuppressUntilReleased() { waitForRelease=true;Guarding=false;absorbRemaining=0; }
        public void Tick(float dt)
        {
            shotCooldown = Mathf.Max(0, shotCooldown - dt);
            absorbCooldown = Mathf.Max(0, absorbCooldown - dt);
            absorbRemaining = Mathf.Max(0, absorbRemaining - dt);
            invulnerability = Mathf.Max(0, invulnerability - dt);
            guardBreakRemaining=Mathf.Max(0,guardBreakRemaining-dt);
            guardRegenDelay=Mathf.Max(0,guardRegenDelay-dt);
            bool previouslyGuarding=Guarding;Guarding=false;
            var bindings = player.Bindings;
            if (!bindings || !player.GameplayInputEnabled || !player.CursorIsLocked || bindings.SuppressInputThisFrame) return;
            var fire = bindings.AttackAction; var absorb = bindings.AbsorbAction;
            if(!absorb.IsPressed())guardReleaseRequired=false;
            if (waitForRelease)
            {
                if (!fire.IsPressed() && !absorb.IsPressed()) waitForRelease = false;
                return;
            }
            if (absorb.WasPressedThisFrame()) TryAbsorb();
            Guarding=encounter.CanFight && absorb.IsPressed() && !GuardBroken && !guardReleaseRequired && Guard>0;
            if(Guarding && !previouslyGuarding && encounter.SelectedAudio)encounter.SelectedAudio.RaiseGuard();
            if(Guarding)
            {
                Guard=Mathf.Max(0,Guard-encounter.Definition.guardHoldDrain*dt);
                guardRegenDelay=encounter.Definition.guardRegenDelay;
                if(Guard<=0)BreakGuard();
            }
            else if(guardRegenDelay<=0 && !GuardBroken)
                Guard=Mathf.Min(encounter.Definition.guardCapacity,Guard+encounter.Definition.guardRegen*dt);
            if (fire.IsPressed() && !Guarding) TryFire();
        }
        public bool TryFire()
        {
            if (!encounter || !encounter.CanFight || shotCooldown > 0 || Guarding) return false;
            bool empowered = Power >= 100;
            encounter.FirePlayerBolt(empowered ? encounter.Definition.empoweredDamage : encounter.Definition.boltDamage, empowered);
            if (empowered) Power = 0;
            shotCooldown = encounter.Definition.shotInterval;
            return true;
        }
        public bool TryAbsorb()
        {
            if (!encounter || !encounter.CanFight || absorbCooldown > 0 || GuardBroken || guardReleaseRequired || Guard<encounter.Definition.absorbGuardCost) return false;
            Guard-=encounter.Definition.absorbGuardCost;guardRegenDelay=encounter.Definition.guardRegenDelay;
            absorbRemaining = encounter.Definition.absorbWindow;
            absorbCooldown = encounter.Definition.absorbCooldown;
            encounter.PlaySound(encounter.Definition.absorbSound, .4f);
            return true;
        }
        public bool CanAbsorb(Vector3 projectile)
        {
            if (!Absorbing) return false;
            var camera = encounter.View;
            var delta = projectile - camera.transform.position;
            return delta.sqrMagnitude <= 4 && Vector3.Dot(camera.transform.forward, delta.normalized) > .70f;
        }
        public void AbsorbHit() { Power = Mathf.Min(100, Power + 25); encounter.Feedback("吞印成功  +25", .7f); }
        public bool TakeDamage(float damage) => ReceiveHit(damage,Vector3.zero,false);
        public bool ReceiveHit(float damage,Vector3 source,bool blockable,float hitProtection=-1)
        {
            if (!encounter || !encounter.CanFight || invulnerability > 0 || damage <= 0) return false;
            if(blockable && Guarding && Vector3.Dot(encounter.View.transform.forward,(source-encounter.View.transform.position).normalized)>=.5f)
            {
                float cost=damage*encounter.Definition.guardDamageCost;
                guardRegenDelay=encounter.Definition.guardRegenDelay;
                if(Guard>cost)
                {
                    Guard-=cost;invulnerability=.10f;
                    encounter.Feedback("格挡成功",.45f);if(encounter.SelectedAudio)encounter.SelectedAudio.Block();else encounter.PlaySound(encounter.Definition.absorbSound,.15f);return false;
                }
                float fraction=Guard/Mathf.Max(.01f,cost);
                damage*=1-fraction*.8f;BreakGuard();
            }
            Health = Mathf.Max(0, Health - damage); invulnerability = hitProtection>=0 ? hitProtection : encounter.Definition.hurtProtection;
            encounter.PlayerHurt();
            if (Health <= 0) encounter.Finish(false);
            return true;
        }
        private void BreakGuard()
        {
            Guard=0;Guarding=false;absorbRemaining=0;guardReleaseRequired=true;
            guardBreakRemaining=encounter.Definition.guardBreakTime;guardRegenDelay=encounter.Definition.guardRegenDelay;
            encounter.Feedback("护盾破碎 · 松开防御键，移动躲避",1.8f);
        }
    }
}
