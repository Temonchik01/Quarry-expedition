using JRPG.Core.Interfaces;
using JRPG.Data;

namespace JRPG.Core.StatusEffects
{
    /// <summary>
    /// Single concrete IStatusEffect implementation.
    /// All three status types (Ignite, Paralysis, Corrosion) share this class —
    /// the behaviour difference is entirely driven by StatusEffectSO.AffectedStat.
    ///
    /// Strategy pattern: the SO data IS the strategy selector.
    /// </summary>
    public sealed class ConcreteStatusEffect : IStatusEffect
    {
        private int _remainingTurns;

        public StatusEffectSO    Data           { get; }
        public StatusEffectKind  Kind           => Data.Kind;
        public int               RemainingTurns => _remainingTurns;
        public bool              IsExpired      => _remainingTurns <= 0;

        public ConcreteStatusEffect(StatusEffectSO data)
        {
            Data            = data;
            _remainingTurns = data.Duration;
        }

        /// <summary>Applies the stat penalty immediately when inflicted.</summary>
        public void Apply(ICharacter target)
        {
            target.ModifyStat(Data.AffectedStat, -Data.StatPenalty);
        }

        /// <summary>Counts down each turn. No per-tick damage in current spec.</summary>
        public void Tick(ICharacter target)
        {
            _remainingTurns--;
        }

        /// <summary>Reverses the stat penalty on expiry or manual removal.</summary>
        public void Remove(ICharacter target)
        {
            target.ModifyStat(Data.AffectedStat, +Data.StatPenalty);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Factory Pattern: produces IStatusEffect instances from SO data.
    /// Centralises construction — if we ever add per-type subclasses,
    /// only this factory changes, not call-sites.
    /// </summary>
    public static class StatusEffectFactory
    {
        public static IStatusEffect Create(StatusEffectSO data)
        {
            return new ConcreteStatusEffect(data);
        }
    }
}