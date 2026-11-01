using System;
using UnityEngine;

namespace BtlSys
{
    [Serializable]
    public sealed class AttackUpgradeData
    {
        [TextArea(1, 3)] public string description;
        [Min(1)] public int upgradePointCost = 1;
        public int powerBonus;
        public int ammoCostDelta;
        [Min(0)] public int extraHits;
        public bool overridesTargetType;
        public TargetType targetType;
        public bool overridesTargetAllies;
        public bool targetAllies;
        public StatusEffectType bonusStatusEffect;
        [Range(0f, 1f)] public float bonusStatusChance;
    }

    public struct AttackRuntimeStats
    {
        public int Power;
        public int AmmoCost;
        public int Hits;
        public int AmmoRestore;
        public TargetType TargetType;
        public bool TargetAllies;

        public AttackRuntimeStats(AttackData attack)
        {
            Power = attack.power;
            AmmoCost = attack.ammoCost;
            Hits = Mathf.Max(1, attack.hits);
            AmmoRestore = attack.ammoRestore;
            TargetType = attack.targetType;
            TargetAllies = attack.targetAllies;
        }
    }

    [CreateAssetMenu(fileName = "NewAttackData", menuName = "BtlSys/AttackData")]
    public class AttackData : ScriptableObject
    {
        public string attackName;
        [Min(0)] public int power;
        [Range(0, 100)] public int accuracy = 100;
        [Min(0)] public int ammoCost;
        [Min(0)] public int ammoRestore;
        public TargetType targetType;
        public StatusEffectType statusEffect;
        [Range(0f, 1f)] public float statusChance = 1f;
        public bool hasPriority;
        [Min(1)] public int hits = 1;
        public string animationTrigger;

        [Header("Status Skill")]
        public bool isStatusAttack;
        public StatType modifiedStat;
        public int statModifierValue;

        [Header("Optional Rules")]
        public bool targetAllies;
        [TextArea(2, 4)] public string description;
        public bool requiresCharge;
        [Range(0, 100)] public int defensePercent = 100;

        [Header("Upgrades")]
        public AttackUpgradeData[] upgrades = new AttackUpgradeData[0];

        public int MaximumUpgradeLevel => upgrades == null ? 0 : upgrades.Length;

        public bool IsUpgradeLevelValid(int level)
        {
            return level >= 0 && level <= MaximumUpgradeLevel;
        }

        public AttackUpgradeData GetUpgrade(int index)
        {
            return upgrades != null && index >= 0 && index < upgrades.Length ? upgrades[index] : null;
        }

        public AttackRuntimeStats GetRuntimeStats(int upgradeLevel)
        {
            AttackRuntimeStats stats = new AttackRuntimeStats(this);
            int level = Mathf.Clamp(upgradeLevel, 0, MaximumUpgradeLevel);
            for (int i = 0; i < level; i++)
            {
                AttackUpgradeData upgrade = upgrades[i];
                if (upgrade == null) continue;
                stats.Power += upgrade.powerBonus;
                stats.AmmoCost = Mathf.Max(0, stats.AmmoCost + upgrade.ammoCostDelta);
                stats.Hits = Mathf.Max(1, stats.Hits + upgrade.extraHits);
                if (upgrade.overridesTargetType) stats.TargetType = upgrade.targetType;
                if (upgrade.overridesTargetAllies) stats.TargetAllies = upgrade.targetAllies;
            }
            return stats;
        }
    }
}
