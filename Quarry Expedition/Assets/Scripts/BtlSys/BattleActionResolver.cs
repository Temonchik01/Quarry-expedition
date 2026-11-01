using System;
using System.Collections.Generic;
using UnityEngine;

namespace BtlSys
{
    public sealed class BattleActionResolver
    {
        public void Resolve(
            BattleAction action,
            IReadOnlyList<Combatant> playerTeam,
            IReadOnlyList<Combatant> enemyTeam,
            Action<BattleAction> queueCharge)
        {
            Combatant user = action?.User;
            if (user == null || !user.IsAlive) return;

            if (user.ShouldSkipTurn())
            {
                Debug.Log($"[Battle] {user.name} lost the turn to paralysis.");
                user.CompleteTurn();
                return;
            }

            switch (action.Type)
            {
                case BattleActionType.Defend:
                    user.Defend();
                    break;
                case BattleActionType.Reload:
                    user.ReplenishAmmo(15);
                    break;
                case BattleActionType.Attack:
                    ResolveAttack(action, playerTeam, enemyTeam, queueCharge);
                    break;
            }
            user.CompleteTurn();
        }

        private static void ResolveAttack(
            BattleAction action,
            IReadOnlyList<Combatant> playerTeam,
            IReadOnlyList<Combatant> enemyTeam,
            Action<BattleAction> queueCharge)
        {
            Combatant user = action.User;
            AttackData attack = action.Attack != null ? action.Attack : user.data?.basicAttack;
            if (attack == null)
            {
                Debug.LogError($"[Battle] {user.name} has no basic attack configured.");
                return;
            }

            AttackRuntimeStats stats = attack.GetRuntimeStats(action.UpgradeLevel);
            if (!action.IsCharged && !user.TrySpendAmmo(stats.AmmoCost))
            {
                Debug.LogWarning($"[Battle] {user.name} cannot afford {attack.attackName}.");
                return;
            }

            if (attack.requiresCharge && !action.IsCharged)
            {
                queueCharge?.Invoke(action.AsCharged());
                Debug.Log($"[Battle] {user.name} is charging {attack.attackName}.");
                return;
            }

            if (!string.IsNullOrEmpty(attack.animationTrigger))
            {
                Animator animator = user.GetComponentInChildren<Animator>();
                if (animator != null) animator.SetTrigger(attack.animationTrigger);
            }

            IReadOnlyList<Combatant> allies = user.isPlayer ? playerTeam : enemyTeam;
            IReadOnlyList<Combatant> opponents = user.isPlayer ? enemyTeam : playerTeam;
            var targets = new List<Combatant>();
            if (stats.TargetType == TargetType.Self)
            {
                targets.Add(user);
            }
            else if (stats.TargetType == TargetType.EntireParty)
            {
                IReadOnlyList<Combatant> party = stats.TargetAllies ? allies : opponents;
                foreach (Combatant member in party)
                {
                    if (member != null && member.IsAlive) targets.Add(member);
                }
            }
            else
            {
                Combatant target = action.Target;
                IReadOnlyList<Combatant> party = stats.TargetAllies ? allies : opponents;
                if (target == null || !target.IsAlive || !Contains(party, target))
                {
                    target = FirstLiving(party);
                }
                if (target != null) targets.Add(target);
            }

            foreach (Combatant target in targets)
            {
                bool landed = false;
                for (int hit = 0; hit < stats.Hits && target.IsAlive; hit++)
                {
                    if (UnityEngine.Random.Range(0, 100) >= Mathf.Clamp(attack.accuracy, 0, 100)) continue;
                    landed = true;
                    if (stats.AmmoRestore > 0) target.ReplenishAmmo(stats.AmmoRestore);
                    if (!attack.isStatusAttack && stats.AmmoRestore == 0)
                    {
                        target.TakeDamage(user.currentAttack + stats.Power, attack.defensePercent);
                    }
                }

                if (!landed) continue;
                if (attack.isStatusAttack && attack.statModifierValue != 0)
                {
                    target.ModifyStat(attack.modifiedStat, attack.statModifierValue);
                }
                if (attack.statusEffect != StatusEffectType.None &&
                    UnityEngine.Random.value < Mathf.Clamp01(attack.statusChance))
                {
                    target.ApplyStatusEffect(attack.statusEffect);
                }
                for (int level = 0; level < action.UpgradeLevel; level++)
                {
                    AttackUpgradeData upgrade = attack.GetUpgrade(level);
                    if (upgrade != null && upgrade.bonusStatusEffect != StatusEffectType.None &&
                        UnityEngine.Random.value < Mathf.Clamp01(upgrade.bonusStatusChance))
                        target.ApplyStatusEffect(upgrade.bonusStatusEffect);
                }
            }
            Debug.Log($"[Battle] {user.name} used {attack.attackName}.");
        }

        private static Combatant FirstLiving(IReadOnlyList<Combatant> party)
        {
            foreach (Combatant member in party)
            {
                if (member != null && member.IsAlive) return member;
            }
            return null;
        }

        private static bool Contains(IReadOnlyList<Combatant> party, Combatant target)
        {
            foreach (Combatant member in party)
            {
                if (member == target) return true;
            }
            return false;
        }
    }
}
