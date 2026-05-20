using UnityEngine;
using System.Collections.Generic;

namespace BtlSys
{
    [System.Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType type;
        public int remainingTurns;

        public ActiveStatusEffect(StatusEffectType type, int turns)
        {
            this.type = type;
            this.remainingTurns = turns;
        }
    }

    public class Combatant : MonoBehaviour
    {
        public CharacterData data;
        public bool isPlayer;

        public int currentHP { get; private set; }
        public int currentAmmo { get; private set; }
        public int currentDefense { get; private set; }
        public int currentAttack { get; private set; }
        public int currentSpeed { get; private set; }

        public List<ActiveStatusEffect> statusEffects = new List<ActiveStatusEffect>();

        public System.Action onStatsChanged;
        public System.Action onDeath;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (data == null) return;
            currentHP = data.maxHealth;
            currentAmmo = data.maxAmmo;
            currentDefense = data.defense;
            currentAttack = data.attackPower;
            currentSpeed = data.speed;
        }

        public void TakeDamage(int rawDamage)
        {
            int actualDamage = Mathf.Max(1, rawDamage - currentDefense);
            currentHP = Mathf.Max(0, currentHP - actualDamage);
            onStatsChanged?.Invoke();

            if (currentHP <= 0)
            {
                onDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            currentHP = Mathf.Min(data.maxHealth, currentHP + amount);
            onStatsChanged?.Invoke();
        }

        public void ConsumeAmmo(int amount)
        {
            currentAmmo = Mathf.Max(0, currentAmmo - amount);
            onStatsChanged?.Invoke();
        }

        public void ReplenishAmmo(int amount)
        {
            currentAmmo = Mathf.Min(data.maxAmmo, currentAmmo + amount);
            onStatsChanged?.Invoke();
        }

        public void ApplyStatusEffect(StatusEffectType type, int turns)
        {
            // Simple logic: overwrite or add
            var existing = statusEffects.Find(s => s.type == type);
            if (existing != null)
            {
                existing.remainingTurns = Mathf.Max(existing.remainingTurns, turns);
            }
            else
            {
                statusEffects.Add(new ActiveStatusEffect(type, turns));
                ApplyEffectStats(type, true);
            }
            onStatsChanged?.Invoke();
        }

        private void ApplyEffectStats(StatusEffectType type, bool apply)
        {
            int mod = apply ? -40 : 40;
            switch (type)
            {
                case StatusEffectType.Ignite:
                    currentAttack += mod;
                    break;
                case StatusEffectType.Paralysis:
                    currentSpeed += mod;
                    break;
                case StatusEffectType.Corrosion:
                    currentDefense += mod;
                    break;
            }
        }

        public void TickStatusEffects()
        {
            for (int i = statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = statusEffects[i];
                
                // DOT damage
                if (effect.type == StatusEffectType.Ignite || effect.type == StatusEffectType.Corrosion)
                {
                    int dot = Mathf.CeilToInt(data.maxHealth * 0.04f);
                    TakeDamage(dot + currentDefense); // Bypass defense for DOT
                }

                effect.remainingTurns--;
                if (effect.remainingTurns <= 0)
                {
                    ApplyEffectStats(effect.type, false);
                    statusEffects.RemoveAt(i);
                }
            }
            onStatsChanged?.Invoke();
        }

        public void ModifyStat(StatType stat, int amount)
        {
            switch (stat)
            {
                case StatType.Attack: currentAttack += amount; break;
                case StatType.Defense: currentDefense += amount; break;
                case StatType.Speed: currentSpeed += amount; break;
                case StatType.Ammo: currentAmmo += amount; break;
            }
            onStatsChanged?.Invoke();
        }

        public void Defend()
        {
            currentDefense += 100; // Temporary for round
            onStatsChanged?.Invoke();
        }

        public void ResetTemporaryStats()
        {
            // Reset defense if it was boosted by Defend
            // This is a bit naive, better to track base and current
            currentDefense = data.defense;
            // Also re-apply status effect modifiers
            foreach(var effect in statusEffects)
            {
                if (effect.type == StatusEffectType.Corrosion) currentDefense -= 40;
            }
            onStatsChanged?.Invoke();
        }
    }
}
