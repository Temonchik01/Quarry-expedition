using UnityEngine;

namespace JRPG.Data
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Attack targeting / type taxonomy
    // ─────────────────────────────────────────────────────────────────────────────

    public enum AttackTargetType
    {
        SingleTarget,
        EntireParty
    }

    public enum StatusApplicationMode
    {
        None,
        Chance,
        Guaranteed
    }

    // Utility / stat-buff attacks
    public enum UtilityStatChange
    {
        None,
        IncreaseSingleTarget,
        DecreaseSingleTarget,
        IncreaseEntireTeam,
        DecreaseEntireTeam
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Data-only asset describing one attack.
    /// The Factory pattern (AttackFactory) reads this asset and produces a
    /// concrete IAttack strategy at runtime.
    ///
    /// Designer workflow:
    ///   Right-click → Create → JRPG → Attack Data
    ///   Fill in the Inspector fields, drag into CharacterStatsSO.AvailableAttacks.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttack", menuName = "JRPG/Attack Data")]
    public sealed class AttackDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _attackName  = "Attack";
        [SerializeField] private string _description = "";

        [Header("Core Stats")]
        [Tooltip("Raw power fed into DamageCalculator")]
        [SerializeField, Min(0)] private int _power    = 50;

        [Tooltip("0–100. 100 = always hits.")]
        [SerializeField, Range(0, 100)] private int _accuracy = 90;

        [Tooltip("Ammo units consumed per use. 0 = free.")]
        [SerializeField, Min(0)] private int _ammoCost = 1;

        [Tooltip("How many times this attack strikes per turn")]
        [SerializeField, Min(1)] private int _hitCount = 1;

        [Header("Targeting")]
        [SerializeField] private AttackTargetType _targetType = AttackTargetType.SingleTarget;

        [Header("Status Effect")]
        [SerializeField] private StatusApplicationMode _statusMode  = StatusApplicationMode.None;
        [Tooltip("Ignored when StatusMode is None")]
        [SerializeField, Range(0f, 1f)] private float _statusChance = 0.3f;
        [SerializeField] private StatusEffectSO       _statusEffect;

        [Header("Special Features")]
        [Tooltip("If true, this attack always executes before others regardless of Speed")]
        [SerializeField] private bool _hasTurnPriority = false;

        [Header("Utility / Stat Change")]
        [Tooltip("Marks this as a utility move instead of a damage move (Power = 0)")]
        [SerializeField] private UtilityStatChange _utilityChange  = UtilityStatChange.None;
        [SerializeField] private StatType          _utilityTarget  = StatType.Attack;
        [Tooltip("Flat amount to add or subtract (spec: 40)")]
        [SerializeField, Min(0)] private int       _utilityAmount  = 40;

        [Header("Animation")]
        [Tooltip("Animator trigger name played on the attacker")]
        [SerializeField] private string _animationTrigger = "Attack";
        [Tooltip("Optional VFX prefab spawned on target(s)")]
        [SerializeField] private GameObject _vfxPrefab;

        // ── Accessors ──────────────────────────────────────────────────────────
        public string              AttackName       => _attackName;
        public string              Description      => _description;
        public int                 Power            => _power;
        public int                 Accuracy         => _accuracy;
        public int                 AmmoCost         => _ammoCost;
        public int                 HitCount         => _hitCount;
        public AttackTargetType    TargetType       => _targetType;
        public StatusApplicationMode StatusMode     => _statusMode;
        public float               StatusChance     => _statusChance;
        public StatusEffectSO      StatusEffect     => _statusEffect;
        public bool                HasTurnPriority  => _hasTurnPriority;
        public UtilityStatChange   UtilityChange    => _utilityChange;
        public StatType            UtilityStat      => _utilityTarget;
        public int                 UtilityAmount    => _utilityAmount;
        public string              AnimationTrigger => _animationTrigger;
        public GameObject          VfxPrefab        => _vfxPrefab;

        public bool IsDamaging  => _power > 0 && _utilityChange == UtilityStatChange.None;
        public bool IsUtility   => _utilityChange != UtilityStatChange.None;
    }
}
