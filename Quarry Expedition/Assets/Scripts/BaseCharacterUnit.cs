using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract MonoBehaviour that all hero/enemy units extend.
/// Handles component caching, animation binding, and stat init.
/// Never becomes a God Class: presentation, AI, input are separate.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseCharacterUnit : MonoBehaviour, ICharacterUnit
{
    [SerializeField] protected CharacterDataSO _characterData;

    // ── Cached components ────────────────────────────────────────
    protected Animator       _animator;
    protected SpriteRenderer _spriteRenderer;

    // ── Runtime state ────────────────────────────────────────────
    private CharacterStats           _stats;
    private List<AttackDefinition>   _attacks;

    // ── ICharacterUnit ───────────────────────────────────────────
    public string          Name                 => _stats.Name;
    public CharacterStats  Stats                => _stats;
    public bool            IsAlive              => _stats.IsAlive;
    public abstract bool   IsPlayerControlled   { get; }

    public IReadOnlyList<AttackDefinition> GetAvailableAttacks() => _attacks;

    // ── Unity lifecycle ──────────────────────────────────────────

    protected virtual void Awake()
    {
        _animator       = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_characterData == null)
        {
            Debug.LogError($"[{gameObject.name}] CharacterDataSO is not assigned!", this);
            return;
        }

        _stats   = new CharacterStats(_characterData);
        _attacks = _characterData.Attacks
                                 .Select(a => new AttackDefinition(a))
                                 .ToList();

        _stats.OnDied += OnUnitDied;
    }

    protected virtual void OnDestroy()
    {
        if (_stats != null)
            _stats.OnDied -= OnUnitDied;
    }

    // ── Animation ────────────────────────────────────────────────

    /// <inheritdoc/>
    public virtual void PlayAnimation(UnitAnimationType type)
    {
        // Animator parameter names match enum names for simplicity
        _animator.SetTrigger(type.ToString());
    }

    // ── Overrideable hooks ───────────────────────────────────────

    protected virtual void OnUnitDied(CharacterStats stats)
    {
        PlayAnimation(UnitAnimationType.Die);
        Debug.Log($"[{Name}] has been defeated.");
    }
}