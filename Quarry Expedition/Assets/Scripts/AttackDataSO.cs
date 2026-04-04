using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "JRPG/AttackData")]
public class AttackDataSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _attackName;
    [SerializeField] private string _description;
    [SerializeField] private AnimationClip _attackAnimation;

    [Header("Core Stats")]
    [SerializeField, Range(0, 999)]  private int   _power       = 50;
    [SerializeField, Range(0f, 1f)]  private float _accuracy    = 0.95f;
    [SerializeField, Range(0, 99)]   private int   _ammoCost    = 5;

    [Header("Targeting")]
    [SerializeField] private AttackTargetType _targetType;
    [SerializeField] private bool _hasTurnPriority;

    [Header("Status Effect")]
    [SerializeField] private bool             _appliesStatusEffect;
    [SerializeField] private StatusEffectType _statusEffectType;
    [SerializeField, Range(0f, 1f)] private float _statusEffectChance = 1f;
    [SerializeField] private bool             _statusEffectGuaranteed;
    [SerializeField, Range(1, 40)]  private int   _statusEffectDuration = 3;

    [Header("Utility (non-damage)")]
    [SerializeField] private bool          _isUtility;
    [SerializeField] private StatType      _utilityStatTarget;
    [SerializeField] private int           _utilityAmount = 40;
    [SerializeField] private UtilityScope  _utilityScope;

    // ── Public accessors ──────────────────────────────────────────
    public string        AttackName              => _attackName;
    public string        Description             => _description;
    public AnimationClip AttackAnimation         => _attackAnimation;
    public int           Power                   => _power;
    public float         Accuracy                => _accuracy;
    public int           AmmoCost                => _ammoCost;
    public AttackTargetType TargetType            => _targetType;
    public bool          HasTurnPriority          => _hasTurnPriority;
    public bool          AppliesStatusEffect      => _appliesStatusEffect;
    public StatusEffectType StatusEffectType      => _statusEffectType;
    public float         StatusEffectChance       => _statusEffectChance;
    public bool          StatusEffectGuaranteed   => _statusEffectGuaranteed;
    public int           StatusEffectDuration     => _statusEffectDuration;
    public bool          IsUtility                => _isUtility;    
    public StatType      UtilityStatTarget        => _utilityStatTarget;
    public int           UtilityAmount            => _utilityAmount;
    public UtilityScope  UtilityScope             => _utilityScope;
}

public enum AttackTargetType
{
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    AllAllies
}

public enum StatType    { Attack, Defense, Speed }
public enum UtilityScope { SingleTarget, WholeTeam }