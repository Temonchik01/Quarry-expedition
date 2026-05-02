using System.Collections.Generic;
using JRPG.Data;
using UnityEngine;

namespace JRPG.Core.Interfaces
{
    // ═════════════════════════════════════════════════════════════════════════════
    // ICharacter — the minimal contract every battle participant must satisfy.
    // MonoBehaviours are NOT required; heroes and enemies both implement this
    // through CharacterRuntime (plain C# class) injected into a MB wrapper.
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Read-only stat view used by targeting and UI queries.</summary>
    public interface ICharacterStats
    {
        string CharacterName { get; }
        int    CurrentHp     { get; }
        int    MaxHp         { get; }
        int    CurrentAmmo   { get; }
        int    MaxAmmo       { get; }
        int    EffectiveAttack  { get; }   // base + modifiers
        int    EffectiveDefense { get; }
        int    EffectiveSpeed   { get; }
        bool   IsAlive          { get; }
    }

    /// <summary>Mutation surface — applied damage, healing, ammo consumption.</summary>
    public interface ICharacterMutator
    {
        void ApplyDamage(int rawDamage);
        void Heal(int amount);
        void ConsumeAmmo(int amount);
        void ModifyStat(StatType stat, int delta);     // +/- permanent (for status)
        void ApplyStatusEffect(IStatusEffect effect);
        void RemoveStatusEffect(StatusEffectKind kind);
    }

    /// <summary>Full character contract used by the battle layer.</summary>
    public interface ICharacter : ICharacterStats, ICharacterMutator
    {
        AttackDataSO[]          Attacks        { get; }
        IReadOnlyList<IStatusEffect> ActiveEffects  { get; }
        void                    TickStatusEffects();  // called at start of each turn
        CharacterStatsSO        BaseData       { get; }
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // IAttack — Strategy pattern: each concrete attack is a self-contained strategy
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Context bag passed to IAttack.Execute. Provides read-only access to
    /// both teams so attacks can determine valid targets without coupling to BattleManager.
    /// </summary>
    public interface IBattleContext
    {
        IReadOnlyList<ICharacter> PlayerTeam { get; }
        IReadOnlyList<ICharacter> EnemyTeam  { get; }
        ICharacter Attacker                  { get; }
    }

    /// <summary>
    /// Every attack (damage, utility, status) implements this.
    /// The Factory pattern produces the correct concrete from AttackDataSO.
    /// </summary>
    public interface IAttack
    {
        AttackDataSO Data    { get; }
        bool         CanUse  (ICharacter user);
        /// <summary>Execute the attack and apply all effects.</summary>
        void         Execute (ICharacter attacker, IReadOnlyList<ICharacter> targets, IBattleContext ctx);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // IStatusEffect — ISP: tick/apply/remove are separate from identity
    // ═════════════════════════════════════════════════════════════════════════════

    public interface IStatusEffect
    {
        StatusEffectSO  Data           { get; }
        StatusEffectKind Kind          { get; }
        int             RemainingTurns { get; }
        bool            IsExpired      { get; }

        void Apply  (ICharacter target);
        void Tick   (ICharacter target);    // called each turn
        void Remove (ICharacter target);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // IEnemyAI — Strategy pattern for enemy decision-making
    // ═════════════════════════════════════════════════════════════════════════════

    public interface IEnemyAI
    {
        /// <summary>
        /// Selects the attack and target(s) for this turn.
        /// Returns null targets if no valid action exists.
        /// </summary>
        (IAttack attack, IReadOnlyList<ICharacter> targets) ChooseAction(
            ICharacter self, IBattleContext context);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // IBattleView — Observer contract: battle logic → presentation layer
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// MonoBehaviour presenters (CharacterView, BattleUIManager) implement this
    /// and subscribe via BattleEvents.  Keeps core logic free of Unity APIs.
    /// </summary>
    public interface IBattleView
    {
        void OnBattleStarted();
        void OnTurnStarted(ICharacter actor);
        void OnAttackExecuted(ICharacter attacker, AttackDataSO attack, IReadOnlyList<ICharacter> targets);
        void OnDamageDealt(ICharacter target, int amount);
        void OnCharacterDefeated(ICharacter character);
        void OnBattleEnded(bool playerWon);
    }
}