using System;
using System.Collections.Generic;
using System.Linq;
using JRPG.Core.Attacks;
using JRPG.Core.Interfaces;
using JRPG.Data;
using UnityEngine;

namespace JRPG.Core.AI
{
    // ═════════════════════════════════════════════════════════════════════════════
    // Concrete AI Strategies (Strategy Pattern)
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Picks a random usable attack and a random valid target. (Boulder)</summary>
    public sealed class RandomAI : IEnemyAI
    {
        public (IAttack attack, IReadOnlyList<ICharacter> targets) ChooseAction(
            ICharacter self, IBattleContext context)
        {
            List<IAttack> usable = GetUsableAttacks(self);
            if (usable.Count == 0) return (null, null);

            IAttack chosen = usable[UnityEngine.Random.Range(0, usable.Count)];
            IReadOnlyList<ICharacter> targets = ResolveTargets(chosen, self, context);
            return (chosen, targets);
        }

        private static List<IAttack> GetUsableAttacks(ICharacter self)
        {
            var list = new List<IAttack>();
            foreach (AttackDataSO data in self.Attacks)
            {
                IAttack atk = AttackFactory.Create(data);
                if (atk.CanUse(self)) list.Add(atk);
            }
            return list;
        }

        internal static IReadOnlyList<ICharacter> ResolveTargets(
            IAttack attack, ICharacter self, IBattleContext ctx)
        {
            bool targetsPlayers = ctx.EnemyTeam.Contains(self);
            IReadOnlyList<ICharacter> pool = targetsPlayers ? ctx.PlayerTeam : ctx.EnemyTeam;
            List<ICharacter> alive = pool.Where(c => c.IsAlive).ToList();

            return attack.Data.TargetType == AttackTargetType.EntireParty
                ? alive
                : new List<ICharacter> { alive[UnityEngine.Random.Range(0, alive.Count)] };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prefers utility/status attacks when they exist in the moveset. (Golem)
    /// Falls back to RandomAI behaviour if no utility moves are available.
    /// </summary>
    public sealed class StatusFirstAI : IEnemyAI
    {
        private readonly float _utilityBias;
        private readonly RandomAI _fallback = new();

        public StatusFirstAI(float utilityBias) => _utilityBias = utilityBias;

        public (IAttack attack, IReadOnlyList<ICharacter> targets) ChooseAction(
            ICharacter self, IBattleContext context)
        {
            bool preferUtility = UnityEngine.Random.value <= _utilityBias;

            if (preferUtility)
            {
                // Try to find a utility move first
                foreach (AttackDataSO data in self.Attacks)
                {
                    if (!data.IsUtility) continue;
                    IAttack atk = AttackFactory.Create(data);
                    if (!atk.CanUse(self)) continue;

                    var targets = RandomAI.ResolveTargets(atk, self, context);
                    return (atk, targets);
                }
            }

            // Fallback: random attack
            return _fallback.ChooseAction(self, context);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // Factory
    // ═════════════════════════════════════════════════════════════════════════════

    public static class EnemyAIStrategyFactory
    {
        public static IEnemyAI Create(EnemyAIBehaviourSO behaviourData)
        {
            if (behaviourData == null) throw new ArgumentNullException(nameof(behaviourData));

            return behaviourData.StrategyType switch
            {
                AIStrategyType.Random        => new RandomAI(),
                AIStrategyType.StatusFirst   => new StatusFirstAI(behaviourData.UtilityMoveBias),
                AIStrategyType.TargetWeakest => new RandomAI(),   // future expansion slot
                _                            => new RandomAI()
            };
        }
    }
}