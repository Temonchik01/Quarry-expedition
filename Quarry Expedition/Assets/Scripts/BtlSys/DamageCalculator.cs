using UnityEngine;
using JRPG.Core.Interfaces;
using JRPG.Data;

namespace JRPG.Core
{
    /// <summary>
    /// Single Responsibility: knows only how to compute final damage.
    /// Formula: FinalDamage = (Attacker.EffectiveAttack * Attack.Power) / 100
    /// Defense absorption is handled by CharacterRuntime.ApplyDamage to maintain SRP.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Returns the raw damage value before defense absorption.
        /// </summary>
        public static int Calculate(ICharacter attacker, AttackDataSO attack)
        {
            float raw = (attacker.EffectiveAttack * attack.Power) / 100f;
            return Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        /// <summary>Accuracy roll. Returns true if the attack lands.</summary>
        public static bool RollAccuracy(AttackDataSO attack)
        {
            return Random.Range(0, 100) < attack.Accuracy;
        }

        /// <summary>Status application roll.</summary>
        public static bool RollStatusChance(AttackDataSO attack)
        {
            return Random.value <= attack.StatusChance;
        }
    }
}