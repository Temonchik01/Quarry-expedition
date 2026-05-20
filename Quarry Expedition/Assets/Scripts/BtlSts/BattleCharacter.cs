using UnityEngine;
using System;
using System.Collections.Generic;

namespace BtlSts
{
    public class BattleCharacter : MonoBehaviour
    {
        public CharacterStats stats;
        
        [Header("Runtime Stats")]
        public int currentHealth;
        public int currentAmmo; // SP
        public int currentBP;
        public int currentDefenseModifier;
        public int currentAttackModifier;
        public int currentSpeedModifier;

        public bool IsDead => currentHealth <= 0;
        public bool IsEnemy;

        public event Action OnStatsChanged;

        private void Start()
        {
            if (stats != null)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            currentHealth = stats.baseHealth;
            currentAmmo = stats.baseAmmo;
            currentBP = 0;
            currentDefenseModifier = 0;
            currentAttackModifier = 0;
            currentSpeedModifier = 0;
            OnStatsChanged?.Invoke();
        }

        public void AddBP(int amount)
        {
            currentBP = Mathf.Clamp(currentBP + amount, 0, stats.maxBP);
            OnStatsChanged?.Invoke();
        }

        public int GetEffectiveAttack() => stats.baseAttack + currentAttackModifier;
        public int GetEffectiveDefense() => stats.baseDefense + currentDefenseModifier;
        public int GetEffectiveSpeed() => stats.baseSpeed + currentSpeedModifier;

        public void TakeDamage(int damage)
        {
            int absorbed = GetEffectiveDefense();
            int actualDamage = Mathf.Max(1, damage - absorbed);
            currentHealth -= actualDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, stats.baseHealth);
            OnStatsChanged?.Invoke();
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, stats.baseHealth);
            OnStatsChanged?.Invoke();
        }

        public void ConsumeAmmo(int amount)
        {
            currentAmmo -= amount;
            currentAmmo = Mathf.Max(0, currentAmmo);
            OnStatsChanged?.Invoke();
        }

        public void RestoreAmmo(int amount)
        {
            currentAmmo += amount;
            currentAmmo = Mathf.Min(currentAmmo, stats.baseAmmo);
            OnStatsChanged?.Invoke();
        }

        public void ApplyStatusEffect(StatusEffectType type, int value, int duration)
        {
            // Simple implementation: immediate stat change
            // In a full game, we'd have a list of active effects with durations
            switch (type)
            {
                case StatusEffectType.Ignite:
                    currentAttackModifier -= value;
                    break;
                case StatusEffectType.Paralysis:
                    currentSpeedModifier -= value;
                    break;
                case StatusEffectType.Corrosion:
                    currentDefenseModifier -= value;
                    break;
            }
            OnStatsChanged?.Invoke();
        }
    }
}
