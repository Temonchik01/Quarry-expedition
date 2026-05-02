using System;
using System.Collections.Generic;
using JRPG.Core.Interfaces;
using JRPG.Core.StatusEffects;
using JRPG.Data;
using UnityEngine;

namespace JRPG.Core.Attacks
{
    // ═════════════════════════════════════════════════════════════════════════════
    // Concrete attack strategies
    // Each class is small, focused, and Open/Closed — adding a new attack type
    // means a new class, not editing existing ones.
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Standard damage attack (single or party).</summary>
    public sealed class DamageAttack : IAttack
    {
        public AttackDataSO Data { get; }

        public DamageAttack(AttackDataSO data) => Data = data;

        public bool CanUse(ICharacter user) => user.CurrentAmmo >= Data.AmmoCost;

        public void Execute(ICharacter attacker, IReadOnlyList<ICharacter> targets, IBattleContext ctx)
        {
            attacker.ConsumeAmmo(Data.AmmoCost);

            foreach (ICharacter target in targets)
            {
                if (!target.IsAlive) continue;

                for (int hit = 0; hit < Data.HitCount; hit++)
                {
                    if (!DamageCalculator.RollAccuracy(Data))
                    {
                        BattleEvents.RaiseMiss(attacker, target);
                        continue;
                    }

                    int dmg = DamageCalculator.Calculate(attacker, Data);
                    target.ApplyDamage(dmg);
                    BattleEvents.RaiseDamageDealt(target, dmg);
                }

                TryApplyStatus(attacker, target);
            }
        }

        private void TryApplyStatus(ICharacter attacker, ICharacter target)
        {
            if (Data.StatusMode == StatusApplicationMode.None || Data.StatusEffect == null) return;
            bool apply = Data.StatusMode == StatusApplicationMode.Guaranteed
                         || DamageCalculator.RollStatusChance(Data);
            if (apply)
                target.ApplyStatusEffect(StatusEffectFactory.Create(Data.StatusEffect));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Utility / stat-buff attack. Power = 0; modifies stats instead.
    /// </summary>
    public sealed class UtilityAttack : IAttack
    {
        public AttackDataSO Data { get; }

        public UtilityAttack(AttackDataSO data) => Data = data;

        public bool CanUse(ICharacter user) => user.CurrentAmmo >= Data.AmmoCost;

        public void Execute(ICharacter attacker, IReadOnlyList<ICharacter> targets, IBattleContext ctx)
        {
            attacker.ConsumeAmmo(Data.AmmoCost);

            bool increase = Data.UtilityChange is UtilityStatChange.IncreaseSingleTarget
                                               or UtilityStatChange.IncreaseEntireTeam;

            int delta = increase ? Data.UtilityAmount : -Data.UtilityAmount;

            foreach (ICharacter target in targets)
            {
                if (!target.IsAlive) continue;
                target.ModifyStat(Data.UtilityStat, delta);
                BattleEvents.RaiseStatChanged(target, Data.UtilityStat, delta);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Decorator pattern: wraps any IAttack and sets HasPriority = true so
    /// TurnOrderSystem can sort it to the front of the queue.
    /// Follows OCP — no existing class is modified.
    /// </summary>
    public sealed class PriorityAttackDecorator : IAttack
    {
        private readonly IAttack _inner;

        public AttackDataSO Data     => _inner.Data;

        public PriorityAttackDecorator(IAttack inner) => _inner = inner;

        public bool CanUse(ICharacter user) => _inner.CanUse(user);

        public void Execute(ICharacter attacker, IReadOnlyList<ICharacter> targets, IBattleContext ctx)
            => _inner.Execute(attacker, targets, ctx);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // Factory Pattern — single entry point for attack construction
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reads AttackDataSO and returns the appropriate IAttack concrete.
    /// Adding a new attack type = new else-if branch here + new class above.
    /// No existing classes are touched (OCP).
    /// </summary>
    public static class AttackFactory
    {
        public static IAttack Create(AttackDataSO data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            IAttack attack = data.IsUtility
                ? new UtilityAttack(data)
                : new DamageAttack(data);

            if (data.HasTurnPriority)
                attack = new PriorityAttackDecorator(attack);

            return attack;
        }
    }
}