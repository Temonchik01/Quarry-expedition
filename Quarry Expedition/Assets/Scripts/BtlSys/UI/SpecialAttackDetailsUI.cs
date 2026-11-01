using TMPro;
using System.Text;
using UnityEngine;

namespace BtlSys.UI
{
    public class SpecialAttackDetailsUI : MonoBehaviour
    {
        [SerializeField] private GameObject detailsPanel;
        public TextMeshProUGUI targetText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI effectText;

        public void Show(AttackData attack, int upgradeLevel = 0)
        {
            if (attack == null)
            {
                Clear();
                return;
            }

            upgradeLevel = Mathf.Clamp(upgradeLevel, 0, attack.MaximumUpgradeLevel);
            AttackRuntimeStats stats = attack.GetRuntimeStats(upgradeLevel);
            if (detailsPanel != null) detailsPanel.SetActive(true);
            targetText.text = TargetLabel(attack, stats);
            accuracyText.text = $"Accuracy: {attack.accuracy}%";
            powerText.text = attack.isStatusAttack ? "Power: -" : $"Power: {stats.Power}";
            costText.text = $"Cost: {stats.AmmoCost} AM";
            descriptionText.text = DescriptionWithUpgrades(attack, upgradeLevel);
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
            if (detailsPanel != null) detailsPanel.SetActive(false);
        }

        private static string DescriptionWithUpgrades(AttackData attack, int level)
        {
            var lines = new StringBuilder(string.IsNullOrWhiteSpace(attack.description)
                ? DefaultDescription(attack)
                : attack.description);
            if (level > 0)
            {
                AttackUpgradeData currentUpgrade = attack.GetUpgrade(level - 1);
                if (currentUpgrade != null && !string.IsNullOrWhiteSpace(currentUpgrade.description))
                    lines.Append("\n<color=#25D65A>+ ").Append(currentUpgrade.description).Append("</color>");
            }
            if (level < attack.MaximumUpgradeLevel)
            {
                AttackUpgradeData nextUpgrade = attack.GetUpgrade(level);
                if (nextUpgrade != null && !string.IsNullOrWhiteSpace(nextUpgrade.description))
                    lines.Append("\nNext: ").Append(nextUpgrade.description);
            }
            return lines.ToString();
        }

        private static string TargetLabel(AttackData attack, AttackRuntimeStats stats)
        {
            if (stats.TargetType == TargetType.Self) return "Self";
            if (stats.TargetType == TargetType.EntireParty)
                return stats.TargetAllies ? "All Allies" : "All Enemies";
            return stats.TargetAllies ? "1 Ally" : "1 Enemy";
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
