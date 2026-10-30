using UnityEngine;

namespace BtlSys
{
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "BtlSys/AttackData")]
    public class AttackData : ScriptableObject
    {
        public string attackName;
        [Min(0)] public int power;
        [Range(0, 100)] public int accuracy = 100;
        [Min(0)] public int ammoCost;
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
    }
}
