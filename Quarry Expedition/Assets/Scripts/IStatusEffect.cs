/// <summary>
/// Strategy interface for all status effects.
/// New effects extend without modifying existing code (OCP).
/// </summary>
public interface IStatusEffect
{
    StatusEffectType EffectType { get; }
    bool IsExpired  { get; }
    void Apply(CharacterStats target);
    void Tick(CharacterStats target);
    void Remove(CharacterStats target);
    void Refresh();
}

public enum StatusEffectType { Ignite, Paralysis, Corrosion }

/// <summary>
/// Ignite: reduces Attack by 40 for a duration.
/// </summary>
public class IgniteEffect : IStatusEffect
{
    private const int STAT_PENALTY = 40;
    private int _remainingTurns;

    public StatusEffectType EffectType => StatusEffectType.Ignite;
    public bool IsExpired => _remainingTurns <= 0;

    public IgniteEffect(int durationTurns = 3) => _remainingTurns = durationTurns;

    public void Apply(CharacterStats target)  => target.ModifyAttack(-STAT_PENALTY);
    public void Tick(CharacterStats target)   => _remainingTurns--;
    public void Remove(CharacterStats target) => target.ModifyAttack(+STAT_PENALTY);
    public void Refresh() => _remainingTurns = 3;
}

/// <summary>Paralysis: reduces Speed by 40.</summary>
public class ParalysisEffect : IStatusEffect
{
    private const int STAT_PENALTY = 40;
    private int _remainingTurns;

    public StatusEffectType EffectType => StatusEffectType.Paralysis;
    public bool IsExpired => _remainingTurns <= 0;

    public ParalysisEffect(int durationTurns = 3) => _remainingTurns = durationTurns;

    public void Apply(CharacterStats target)  => target.ModifySpeed(-STAT_PENALTY);
    public void Tick(CharacterStats target)   => _remainingTurns--;
    public void Remove(CharacterStats target) => target.ModifySpeed(+STAT_PENALTY);
    public void Refresh() => _remainingTurns = 3;
}

/// <summary>Corrosion: reduces Defense by 40.</summary>
public class CorrosionEffect : IStatusEffect
{
    private const int STAT_PENALTY = 40;
    private int _remainingTurns;

    public StatusEffectType EffectType => StatusEffectType.Corrosion;
    public bool IsExpired => _remainingTurns <= 0;

    public CorrosionEffect(int durationTurns = 3) => _remainingTurns = durationTurns;

    public void Apply(CharacterStats target)  => target.ModifyDefense(-STAT_PENALTY);
    public void Tick(CharacterStats target)   => _remainingTurns--;
    public void Remove(CharacterStats target) => target.ModifyDefense(+STAT_PENALTY);
    public void Refresh() => _remainingTurns = 3;
}