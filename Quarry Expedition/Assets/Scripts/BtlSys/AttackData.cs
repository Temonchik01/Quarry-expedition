using UnityEngine;

namespace BtlSys
{
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "BtlSys/AttackData")]
    public class AttackData : ScriptableObject
    {
        public string attackName;
        public int power;
        public int accuracy = 100;
        public int ammoCost;
        public TargetType targetType;
        public StatusEffectType statusEffect;
        public float statusChance = 1.0f;
        public bool hasPriority;
        public int hits = 1;
        public string animationTrigger; // To be used with Animator

        [Header("Status Attack Modifier")]
        public bool isStatusAttack;
        public StatType modifiedStat;
        public int statModifierValue; // e.g., 40
    }
}
