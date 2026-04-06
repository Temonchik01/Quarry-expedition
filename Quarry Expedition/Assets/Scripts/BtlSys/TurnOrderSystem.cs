using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds the turn order queue from a combined combatant list.
/// Priority attacks jump the queue. Purely functional — no Unity deps.
/// </summary>
public static class TurnOrderSystem
{
    /// <summary>
    /// Sorts all living combatants by Speed descending.
    /// Units with priority attacks are placed first.
    /// </summary>
    public static Queue<ICharacterUnit> BuildTurnQueue(
        IEnumerable<ICharacterUnit> allCombatants,
        ICharacterUnit priorityUnit = null)
    {
        var sorted = allCombatants
            .Where(c => c.IsAlive)
            .OrderByDescending(c => c.Stats.Speed)
            .ToList();

        // Move priority unit to front if set
        if (priorityUnit != null && sorted.Contains(priorityUnit))
        {
            sorted.Remove(priorityUnit);
            sorted.Insert(0, priorityUnit);
        }

        return new Queue<ICharacterUnit>(sorted);
    }
}