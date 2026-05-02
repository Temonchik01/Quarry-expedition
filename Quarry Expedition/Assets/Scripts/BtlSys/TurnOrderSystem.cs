using System.Collections.Generic;
using System.Linq;
using JRPG.Core.Attacks;
using JRPG.Core.Interfaces;

namespace JRPG.Core.Battle
{
    /// <summary>
    /// Single Responsibility: knows only how to produce an ordered list of actors.
    /// Priority attacks (from PriorityAttackDecorator) jump to the front.
    /// Tie-breaking is by index (insertion order) for determinism.
    /// </summary>
    public static class TurnOrderSystem
    {
        /// <summary>
        /// Returns all living combatants sorted descending by EffectiveSpeed.
        /// Characters with a pending priority action are moved to the front.
        /// </summary>
        public static List<ICharacter> BuildTurnOrder(
            IReadOnlyList<ICharacter> allCombatants,
            IReadOnlyDictionary<ICharacter, IAttack> pendingActions)
        {
            return allCombatants
                .Where(c => c.IsAlive)
                .OrderByDescending(c => HasPriority(c, pendingActions) ? int.MaxValue : c.EffectiveSpeed)
                .ToList();
        }

        private static bool HasPriority(
            ICharacter character,
            IReadOnlyDictionary<ICharacter, IAttack> pending)
        {
            return pending.TryGetValue(character, out IAttack atk)
                   && atk is PriorityAttackDecorator;
        }
    }
}