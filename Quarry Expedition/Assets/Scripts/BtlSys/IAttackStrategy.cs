using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strategy interface: each attack type has its own execution rule.
/// </summary>
public interface IAttackStrategy
{
    bool Execute(AttackDataSO data, ICharacterUnit attacker, IReadOnlyList<ICharacterUnit> targets);
}

/// <summary>Hits a single target. Handles accuracy roll + status effect.</summary>
public class SingleTargetStrategy : IAttackStrategy
{
    public bool Execute(AttackDataSO data, ICharacterUnit attacker, IReadOnlyList<ICharacterUnit> targets)
    {
        if (targets.Count == 0) return false;
        var target = targets[0];

        if (!AccuracyRoll(data.Accuracy)) { LogMiss(attacker, data, target); return false; }

        ApplyDamage(data, attacker, target);
        TryApplyStatus(data, target);
        return true;
    }

    protected void ApplyDamage(AttackDataSO data, ICharacterUnit attacker, ICharacterUnit target)
    {
        if (data.Power <= 0) return;
        int raw = attacker.Stats.Attack + data.Power;
        target.Stats.TakeDamage(raw);
        /// target.PlayAnimation(UnitAnimationType.Hit);
    }

    protected void TryApplyStatus(AttackDataSO data, ICharacterUnit target)
    {
        if (!data.AppliesStatusEffect) return;
        float roll = Random.value;
        bool  hit  = data.StatusEffectGuaranteed || roll <= data.StatusEffectChance;
        if (!hit) return;
        var effect = StatusEffectFactory.Create(data.StatusEffectType, data.StatusEffectDuration);
        target.Stats.ApplyStatusEffect(effect);
        /// target.PlayAnimation(UnitAnimationType.StatusEffect);
    }

    protected static bool AccuracyRoll(float accuracy) => Random.value <= accuracy;

    protected static void LogMiss(ICharacterUnit src, AttackDataSO data, ICharacterUnit tgt)
        => Debug.Log($"[{src.Name}] {data.AttackName} missed {tgt.Name}!");
}

/// <summary>Hits all targets in the list.</summary>
public class AllTargetsStrategy : IAttackStrategy
{
    private readonly SingleTargetStrategy _single = new();

    public bool Execute(AttackDataSO data, ICharacterUnit attacker, IReadOnlyList<ICharacterUnit> targets)
    {
        bool anyHit = false;
        foreach (var t in targets)
            if (t.IsAlive) anyHit |= _single.Execute(data, attacker, new List<ICharacterUnit> { t });
        return anyHit;
    }
}

/// <summary>
/// Utility (buff/debuff): modifies a stat on one or all targets.
/// No damage dealt.
/// </summary>
public class UtilityStrategy : IAttackStrategy
{
    public bool Execute(AttackDataSO data, ICharacterUnit attacker, IReadOnlyList<ICharacterUnit> targets)
    {
        var affected = data.UtilityScope == UtilityScope.WholeTeam
            ? targets
            : targets.Count > 0 ? new List<ICharacterUnit> { targets[0] } : targets;

        foreach (var t in affected)
        {
            ApplyStat(data, t.Stats);
            Debug.Log($"[Utility] {data.AttackName} → {t.Name} {data.UtilityStatTarget} {(data.UtilityAmount >= 0 ? "+" : "")}{data.UtilityAmount}");
        }
        return true;
    }

    private static void ApplyStat(AttackDataSO data, CharacterStats stats)
    {
        switch (data.UtilityStatTarget)
        {
            case StatType.Attack:  stats.ModifyAttack(data.UtilityAmount);  break;
            case StatType.Defense: stats.ModifyDefense(data.UtilityAmount); break;
            case StatType.Speed:   stats.ModifySpeed(data.UtilityAmount);   break;
        }
    }
}