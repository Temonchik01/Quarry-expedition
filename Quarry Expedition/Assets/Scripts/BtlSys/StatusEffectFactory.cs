using System;

/// <summary>
/// Centralizes IStatusEffect creation. Add new effects here without
/// touching call sites (OCP + Factory Pattern).
/// </summary>
public static class StatusEffectFactory
{
    /// <summary>
    /// Creates a status effect by type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown effect type.</exception>
    public static IStatusEffect Create(StatusEffectType type, int durationTurns = 3)
    {
        return type switch
        {
            StatusEffectType.Ignite    => new IgniteEffect(durationTurns),
            StatusEffectType.Paralysis => new ParalysisEffect(durationTurns),
            StatusEffectType.Corrosion  => new CorrosionEffect(durationTurns),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown status effect.")
        };
    }
}