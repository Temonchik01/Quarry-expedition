using System;
using System.Collections.Generic;
using JRPG.Core.Interfaces;
using JRPG.Data;

namespace JRPG.Core
{
    /// <summary>
    /// Observer Pattern: static event bus that decouples the core battle layer
    /// from all Unity presentation (UI, animations, audio).
    ///
    /// Why static events here instead of UnityEvent?
    ///   - Core logic has zero Unity API dependency.
    ///   - Any MonoBehaviour can subscribe without holding a manager reference.
    ///   - Trivially unit-testable (subscribe a lambda, assert calls).
    ///
    /// Presenters subscribe in OnEnable, unsubscribe in OnDisable to avoid leaks.
    /// </summary>
    public static class BattleEvents
    {
        public static event Action                                     OnBattleStarted;
        public static event Action<ICharacter>                        OnTurnStarted;
        public static event Action<ICharacter, AttackDataSO,
                                  IReadOnlyList<ICharacter>>          OnAttackExecuted;
        public static event Action<ICharacter, int>                   OnDamageDealt;
        public static event Action<ICharacter, ICharacter>            OnMiss;           // attacker, target
        public static event Action<ICharacter, StatType, int>         OnStatChanged;
        public static event Action<ICharacter, IStatusEffect>         OnStatusApplied;
        public static event Action<ICharacter>                        OnCharacterDefeated;
        public static event Action<bool>                              OnBattleEnded;    // true = player won

        // ── Raise helpers (internal so only BattleManager / tests call them) ──

        internal static void RaiseBattleStarted()                                   => OnBattleStarted?.Invoke();
        internal static void RaiseTurnStarted(ICharacter actor)                     => OnTurnStarted?.Invoke(actor);
        internal static void RaiseAttackExecuted(ICharacter a, AttackDataSO atk,
                                                  IReadOnlyList<ICharacter> targets) => OnAttackExecuted?.Invoke(a, atk, targets);
        internal static void RaiseDamageDealt(ICharacter t, int dmg)               => OnDamageDealt?.Invoke(t, dmg);
        internal static void RaiseMiss(ICharacter a, ICharacter t)                  => OnMiss?.Invoke(a, t);
        internal static void RaiseStatChanged(ICharacter t, StatType s, int d)     => OnStatChanged?.Invoke(t, s, d);
        internal static void RaiseStatusApplied(ICharacter t, IStatusEffect e)     => OnStatusApplied?.Invoke(t, e);
        internal static void RaiseCharacterDefeated(ICharacter c)                  => OnCharacterDefeated?.Invoke(c);
        internal static void RaiseBattleEnded(bool playerWon)                      => OnBattleEnded?.Invoke(playerWon);
    }
}