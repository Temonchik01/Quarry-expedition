using System.Collections.Generic;
using UnityEngine;

namespace BtlSys
{
    public sealed class EnemyActionPlanner
    {
        public BattleAction Choose(Combatant enemy, IReadOnlyList<Combatant> opponents)
        {
            if (enemy == null || !enemy.IsAlive) return null;

            var living = new List<Combatant>();
            foreach (Combatant opponent in opponents)
            {
                if (opponent != null && opponent.IsAlive) living.Add(opponent);
            }
            if (living.Count == 0) return null;

            AttackData attack = enemy.data != null ? enemy.data.basicAttack : null;
            if (enemy.data != null && enemy.data.specials != null)
            {
                var affordable = new List<AttackData>();
                foreach (AttackData skill in enemy.data.specials)
                {
                    if (skill != null && skill != attack && enemy.CanUse(skill)) affordable.Add(skill);
                }
                if (affordable.Count > 0 && Random.value < 0.5f)
                {
                    attack = affordable[Random.Range(0, affordable.Count)];
                }
            }

            Combatant target = attack != null && attack.targetType == TargetType.Self
                ? enemy
                : attack != null && attack.targetAllies ? enemy
                : living[Random.Range(0, living.Count)];
            return BattleAction.UseAttack(enemy, attack, target);
        }
    }
}
