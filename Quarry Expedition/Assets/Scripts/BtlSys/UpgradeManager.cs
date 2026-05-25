using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BtlSys
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public bool CanUpgrade(ProgressionManager.CombatantState heroState)
        {
            return heroState.upgradePoints > 0;
        }

        public void UpgradeStat(ProgressionManager.CombatantState heroState, StatType stat)
        {
            if (!CanUpgrade(heroState)) return;

            switch (stat)
            {
                case StatType.Health:
                    if (heroState.maxHP < 250)
                    {
                        heroState.healthBoost += 25;
                        heroState.maxHP += 25;
                        heroState.hp += 25; // Heal slightly when upgrading HP?
                        SpendPoint(heroState);
                    }
                    break;
                case StatType.Attack:
                    // Need to check actual current attack, but for now we assume base + boost
                    // Since we don't have base in state, we'd need to lookup data or store base
                    // Let's assume the cap 250 applies to total.
                    heroState.attackBoost += 25;
                    SpendPoint(heroState);
                    break;
                case StatType.Defense:
                    heroState.defenseBoost += 25;
                    SpendPoint(heroState);
                    break;
                case StatType.Ammo:
                    if (heroState.maxAmmo < 250)
                    {
                        heroState.ammoBoost += 25;
                        heroState.maxAmmo += 25;
                        heroState.ammo += 25;
                        SpendPoint(heroState);
                    }
                    break;
                case StatType.Speed:
                    heroState.speedBoost += 25;
                    SpendPoint(heroState);
                    break;
            }
        }

        public void UpgradeSkill(ProgressionManager.CombatantState heroState, AttackData skill)
        {
            if (!CanUpgrade(heroState)) return;
            if (heroState.upgradedSkills.Contains(skill.attackName)) return;

            heroState.upgradedSkills.Add(skill.attackName);
            SpendPoint(heroState);
        }

        private void SpendPoint(ProgressionManager.CombatantState heroState)
        {
            heroState.upgradePoints--;
            Debug.Log($"Spent 1 point on {heroState.characterName}. Points left: {heroState.upgradePoints}");
        }
    }
}
