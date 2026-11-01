using System;
using System.Collections.Generic;
using UnityEngine;

namespace BtlSys
{
    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType type;
        public int remainingTurns;

        public ActiveStatusEffect(StatusEffectType type, int turns)
        {
            this.type = type;
            remainingTurns = turns;
        }
    }

    public class Combatant : MonoBehaviour
    {
        private const int StatLimit = 250;
        private const int StatusPenalty = 40;

        public CharacterData data;
        public bool isPlayer;

        public int currentHP { get; private set; }
        public int currentAmmo { get; private set; }
        public int currentDefense => EffectiveStat(StatType.Defense);
        public int currentAttack => EffectiveStat(StatType.Attack);
        public int currentSpeed => EffectiveStat(StatType.Speed);
        public bool IsAlive => currentHP > 0;

        // Kept for compatibility with existing Battle scene serialization.
        public List<ActiveStatusEffect> statusEffects = new List<ActiveStatusEffect>();

        public event Action onStatsChanged;
        public event Action onDeath;

        private readonly List<TimedStatModifier> modifiers = new List<TimedStatModifier>();
        private int defendBonus;
        [Header("Status Tint")]
        [SerializeField] private Color igniteTint = new Color(1f, 0.65f, 0.65f);
        [SerializeField] private Color paralysisTint = new Color(1f, 0.95f, 0.55f);
        [SerializeField] private Color corrosionTint = new Color(0.65f, 1f, 0.72f);
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;

        private sealed class TimedStatModifier
        {
            public StatType Stat;
            public int Amount;
            public int RemainingTurns;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null) originalColor = spriteRenderer.color;
            }
            if (data == null)
            {
                Debug.LogError($"[Combatant] {name} has no CharacterData.", this);
                return;
            }

            currentHP = Mathf.Max(0, data.maxHealth);
            currentAmmo = Mathf.Max(0, data.maxAmmo);
            statusEffects.Clear();
            modifiers.Clear();
            defendBonus = 0;
            UpdateVisualTint();
            onStatsChanged?.Invoke();
        }

        public void LoadState(int hp, int ammo)
        {
            if (data == null) return;
            currentHP = Mathf.Clamp(hp, 0, data.maxHealth);
            currentAmmo = Mathf.Clamp(ammo, 0, data.maxAmmo);
            UpdateVisualTint();
            onStatsChanged?.Invoke();
        }

        public bool CanUse(AttackData attack, int upgradeLevel = 0)
        {
            if (!IsAlive || attack == null || !attack.IsUpgradeLevelValid(upgradeLevel)) return false;
            return currentAmmo >= attack.GetRuntimeStats(upgradeLevel).AmmoCost;
        }

        public bool TrySpendAmmo(int amount)
        {
            if (amount < 0 || currentAmmo < amount) return false;
            currentAmmo -= amount;
            onStatsChanged?.Invoke();
            return true;
        }

        public void ReplenishAmmo(int amount)
        {
            if (!IsAlive || data == null || amount <= 0) return;
            currentAmmo = Mathf.Min(data.maxAmmo, currentAmmo + amount);
            onStatsChanged?.Invoke();
        }

        public void TakeDamage(int rawDamage, int defensePercent = 100)
        {
            if (!IsAlive) return;
            int blocked = Mathf.RoundToInt(currentDefense * Mathf.Clamp(defensePercent, 0, 100) / 100f);
            int damage = Mathf.Max(1, rawDamage - blocked);
            currentHP = Mathf.Max(0, currentHP - damage);
            onStatsChanged?.Invoke();

            if (currentHP == 0)
            {
                UpdateVisualTint();
                Debug.Log($"[Combatant] {name} was defeated.", this);
                onDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive || data == null || amount <= 0) return;
            currentHP = Mathf.Min(data.maxHealth, currentHP + amount);
            onStatsChanged?.Invoke();
        }

        public void Defend()
        {
            defendBonus = 100;
            onStatsChanged?.Invoke();
        }

        public void BeginRound()
        {
            if (defendBonus == 0) return;
            defendBonus = 0;
            onStatsChanged?.Invoke();
        }

        public bool ShouldSkipTurn()
        {
            return statusEffects.Count > 0 && statusEffects[0].type == StatusEffectType.Paralysis
                && UnityEngine.Random.value < 0.5f;
        }

        public bool ApplyStatusEffect(StatusEffectType type, int turns = 3)
        {
            if (!IsAlive || type == StatusEffectType.None || turns <= 0) return false;
            if (statusEffects.Count > 0)
            {
                if (statusEffects[0].type != type) return false;
                statusEffects[0].remainingTurns = turns;
            }
            else
            {
                statusEffects.Add(new ActiveStatusEffect(type, turns));
            }
            UpdateVisualTint();
            onStatsChanged?.Invoke();
            return true;
        }

        public void ModifyStat(StatType stat, int amount, int turns = 3)
        {
            if (!IsAlive || amount == 0 || turns <= 0) return;
            if (stat == StatType.Health)
            {
                if (amount > 0) Heal(amount);
                else TakeDamage(-amount, 0);
                return;
            }
            if (stat == StatType.Ammo)
            {
                if (amount > 0) ReplenishAmmo(amount);
                else TrySpendAmmo(Mathf.Min(currentAmmo, -amount));
                return;
            }

            modifiers.Add(new TimedStatModifier { Stat = stat, Amount = amount, RemainingTurns = turns });
            onStatsChanged?.Invoke();
        }

        public void CompleteTurn()
        {
            if (statusEffects.Count > 0)
            {
                statusEffects[0].remainingTurns--;
                if (statusEffects[0].remainingTurns <= 0) statusEffects.Clear();
            }

            UpdateVisualTint();
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                modifiers[i].RemainingTurns--;
                if (modifiers[i].RemainingTurns <= 0) modifiers.RemoveAt(i);
            }
            onStatsChanged?.Invoke();
        }

        private int EffectiveStat(StatType stat)
        {
            if (data == null) return 0;
            int value = stat == StatType.Attack ? data.attackPower
                : stat == StatType.Defense ? data.defense : data.speed;

            foreach (TimedStatModifier modifier in modifiers)
            {
                if (modifier.Stat == stat) value += modifier.Amount;
            }
            if (stat == StatType.Defense) value += defendBonus;
            if (statusEffects.Count > 0)
            {
                StatusEffectType effect = statusEffects[0].type;
                if (stat == StatType.Attack && effect == StatusEffectType.Ignite) value -= StatusPenalty;
                if (stat == StatType.Speed && effect == StatusEffectType.Paralysis) value -= StatusPenalty;
                if (stat == StatType.Defense && effect == StatusEffectType.Corrosion) value -= StatusPenalty;
            }
            return Mathf.Clamp(value, 0, StatLimit);
        }

        private void UpdateVisualTint()
        {
            if (spriteRenderer == null) return;
            if (!IsAlive)
            {
                spriteRenderer.color = Color.black;
                return;
            }

            Color tint = Color.white;
            if (statusEffects.Count > 0)
            {
                switch (statusEffects[0].type)
                {
                    case StatusEffectType.Ignite: tint = igniteTint; break;
                    case StatusEffectType.Paralysis: tint = paralysisTint; break;
                    case StatusEffectType.Corrosion: tint = corrosionTint; break;
                }
            }
            spriteRenderer.color = originalColor * tint;
        }
    }
}
