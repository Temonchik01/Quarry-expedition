using System.Collections.Generic;

/// <summary>
/// Everything BattleManager needs to know about a combatant.
/// Segregated from animation, AI, and presentation concerns.
/// </summary>
public interface ICharacterUnit
{
    string Name              { get; }
    CharacterStats Stats     { get; }
    bool IsAlive             { get; }
    bool IsPlayerControlled  { get; }

    IReadOnlyList<AttackDefinition> GetAvailableAttacks();

    /// <summary>
    /// Plays the unit's idle / react animation.
    /// Decoupled from battle logic via this interface.
    /// </summary>
   /// void PlayAnimation(UnitAnimationType type);
}

public enum UnitAnimationType { Idle, Attack, Hit, Die, StatusEffect }