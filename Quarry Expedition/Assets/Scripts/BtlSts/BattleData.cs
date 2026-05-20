using UnityEngine;
using System.Collections.Generic;

namespace BtlSts
{
    [System.Serializable]
    public enum TargetType
    {
        SingleEnemy,
        AllEnemies,
        SingleAlly,
        AllAllies,
        Self
    }

    [System.Serializable]
    public enum StatusEffectType
    {
        None,
        Ignite,     // Reduces Attack by 40
        Paralysis,  // Reduces Speed by 40
        Corrosion   // Reduces Defense by 40
    }

    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Battle/Character Stats")]
    public class CharacterStats : ScriptableObject
    {
        public string characterName;
        public int baseHealth;
        public int baseDefense;
        public int baseAttack;
        public int baseAmmo; // Used as SP (Skill Points)
        public int baseSpeed;
        public int maxBP = 5; // Boost Points max
        
        public List<AttackActionData> availableAttacks;
    }

    [CreateAssetMenu(fileName = "NewAttack", menuName = "Battle/Attack Action")]
    public class AttackActionData : ScriptableObject
    {
        public string attackName;
        [TextArea] public string description;
        public int power;
        public int accuracy = 100;
        public int ammoCost;
        public int priority = 0;
        public int hitCount = 1;
        
        public TargetType targetType;
        public StatusEffectType statusEffect;
        public float statusChance = 1.0f; // 1.0 = 100%
        public int statusDuration = 40;

        public bool isBuff; // If true, increases stat instead of damage/debuff
        public int statModifierValue = 40;
    }
}
