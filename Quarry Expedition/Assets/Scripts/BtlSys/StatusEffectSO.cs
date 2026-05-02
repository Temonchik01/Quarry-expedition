using UnityEngine;

namespace JRPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Enums — kept in the Data namespace so all layers can reference them without
    // pulling in MonoBehaviour dependencies.
    // ─────────────────────────────────────────────────────────────────────────────

    public enum StatType { Attack, Defense, Speed, Ammo }

    public enum StatusEffectKind
    {
        /// <summary>Reduces Attack by 40 per stack.</summary>
        Ignite,
        /// <summary>Reduces Speed by 40 per stack.</summary>
        Paralysis,
        /// <summary>Reduces Defense by 40 per stack.</summary>
        Corrosion
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Data asset describing a single status effect.
    /// The runtime behaviour (Apply / Tick / Remove) is implemented in
    /// <see cref="JRPG.Core.StatusEffects.ConcreteStatusEffect"/> via the
    /// Strategy pattern — this SO is purely the data contract.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "JRPG/Status Effect")]
    public sealed class StatusEffectSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName   = "Status";
        [SerializeField] private string _description   = "";
        [SerializeField] private Color  _tintColor     = Color.white;
        [SerializeField] private Sprite _icon;

        [Header("Behaviour")]
        [SerializeField] private StatusEffectKind _kind = StatusEffectKind.Ignite;

        [Tooltip("How many turns the effect lasts (spec: 40)")]
        [SerializeField, Min(1)] private int _duration = 40;

        [Tooltip("Flat stat penalty applied to the affected stat")]
        [SerializeField, Min(0)] private int _statPenalty = 40;

        // ── Accessors ──────────────────────────────────────────────────────────
        public string           DisplayName  => _displayName;
        public string           Description  => _description;
        public Color            TintColor    => _tintColor;
        public Sprite           Icon         => _icon;
        public StatusEffectKind Kind         => _kind;
        public int              Duration     => _duration;
        public int              StatPenalty  => _statPenalty;

        /// <summary>Returns which stat this effect penalises.</summary>
        public StatType AffectedStat => _kind switch
        {
            StatusEffectKind.Ignite    => StatType.Attack,
            StatusEffectKind.Paralysis => StatType.Speed,
            StatusEffectKind.Corrosion => StatType.Defense,
            _                          => StatType.Attack
        };
    }
}