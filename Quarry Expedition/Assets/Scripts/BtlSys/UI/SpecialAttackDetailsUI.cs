using TMPro;
using UnityEngine;

namespace BtlSys.UI
{
    public class SpecialAttackDetailsUI : MonoBehaviour
    {
        public TextMeshProUGUI targetText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI effectText;

        public void Show(AttackData attack)
        {
            if (attack == null)
            {
                Clear();
                return;
            }

            targetText.text = TargetLabel(attack);
            accuracyText.text = $"Accuracy: {attack.accuracy}%";
            powerText.text = attack.isStatusAttack ? "Power: -" : $"Power: {attack.power}";
            costText.text = $"Cost: {attack.ammoCost} AM";
            descriptionText.text = string.IsNullOrWhiteSpace(attack.description)
                ? DefaultDescription(attack)
                : attack.description;
            effectText.text = EffectLabel(attack);
        }

        public void Clear()
        {
            targetText.text = string.Empty;
            accuracyText.text = string.Empty;
            powerText.text = string.Empty;
            costText.text = string.Empty;
            descriptionText.text = string.Empty;
            effectText.text = string.Empty;
        }

        private static string TargetLabel(AttackData attack)
        {
            if (attack.targetType == TargetType.Self) return "Self";
            if (attack.targetType == TargetType.EntireParty)
                return attack.targetAllies ? "All Allies" : "All Enemies";
            return attack.targetAllies ? "1 Ally" : "1 Enemy";
        }

        private static string DefaultDescription(AttackData attack)
        {
            if (attack.isStatusAttack) return "Applies a temporary effect.";
            return attack.hits > 1
                ? $"Attempts {attack.hits} hits on each target."
                : "Attempts one hit on each target.";
        }

        private static string EffectLabel(AttackData attack)
        {
            if (attack.statusEffect != StatusEffectType.None)
                return $"<color=#25D65A>+ {attack.statusEffect} on hit</color>";
            if (attack.isStatusAttack && attack.statModifierValue != 0)
                return $"<color=#25D65A>{attack.modifiedStat} {attack.statModifierValue:+#;-#;0} for 3 turns</color>";
            if (attack.hasPriority) return "<color=#25D65A>+ Priority</color>";
            if (attack.requiresCharge) return "<color=#25D65A>+ Charges for one turn</color>";
            return string.Empty;
        }
    }
}
