using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime mutable stats for a battle unit. Initialized from
/// <see cref="CharacterDataSO"/> and modified by effects/buffs.
/// Raises events so the UI reacts without polling.
/// </summary>
public class CharacterStats
{
    // ── Events ──────────────────────────────────────────────────────
    public event Action<int, int> OnHealthChanged;   // (current, max)
    public event Action<CharacterStats> OnDied;

    // ── Identity ────────────────────────────────────────────────────
    public string Name { get; private set; }

    // ── Current values (affected by buffs/effects) ───────────────
    private int _maxHealth;
    private int _currentHealth;
    private int _defense;
    private int _attack;
    private int _currentAmmo;
    private int _maxAmmo;
    private int _speed;

    // ── Base values (restored when effects expire) ───────────────
    private int _baseDefense;
    private int _baseAttack;
    private int _baseSpeed;

    // ── Public accessors ─────────────────────────────────────────
    public int MaxHealth     => _maxHealth;
    public int CurrentHealth => _currentHealth;
    public int Defense       => _defense;
    public int Attack        => _attack;
    public int CurrentAmmo   => _currentAmmo;
    public int MaxAmmo       => _maxAmmo;
    public int Speed         => _speed;
    public bool IsAlive      => _currentHealth > 0;

    // ── Active status effects ────────────────────────────────────
    private readonly List<IStatusEffect> _activeEffects = new();

    // ── Constructor ──────────────────────────────────────────────

    /// <summary>
    /// Initializes runtime stats from a ScriptableObject template.
    /// </summary>
    public CharacterStats(CharacterDataSO data)
    {
        Name        = data.CharacterName;
        _maxHealth  = data.BaseHealth;
        _baseDefense  = data.BaseDefense;
        _baseAttack   = data.BaseAttack;
        _maxAmmo      = data.BaseAmmo;
        _baseSpeed    = data.BaseSpeed;

        _currentHealth = _maxHealth;
        _defense       = _baseDefense;
        _attack        = _baseAttack;
        _currentAmmo   = _maxAmmo;
        _speed         = _baseSpeed;
    }

    // ── Health ───────────────────────────────────────────────────

    /// <summary>
    /// Applies final damage after defense reduction.
    /// Defense absorbs damage: finalDamage = max(1, rawDamage - Defense).
    /// </summary>
    public void TakeDamage(int rawDamage)
    {
        int absorbed    = Mathf.Max(0, rawDamage - _defense);
        int finalDamage = Mathf.Max(1, absorbed);          // always at least 1
        _currentHealth  = Mathf.Max(0, _currentHealth - finalDamage);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        Debug.Log($"[{Name}] took {finalDamage} dmg (raw:{rawDamage} def:{_defense}). HP:{_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
            OnDied?.Invoke(this);
    }

    /// <summary>Restores HP, capped at MaxHealth.</summary>
    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    // ── Ammo ─────────────────────────────────────────────────────

    /// <summary>Returns false if insufficient ammo.</summary>
    public bool TryConsumeAmmo(int cost)
    {
        if (_currentAmmo < cost) return false;
        _currentAmmo -= cost;
        return true;
    }

    // ── Stat modifiers (used by buffs/debuffs) ───────────────────

    public void ModifyAttack(int delta)  { _attack  = Mathf.Max(0, _attack  + delta); }
    public void ModifyDefense(int delta) { _defense = Mathf.Max(0, _defense + delta); }
    public void ModifySpeed(int delta)   { _speed   = Mathf.Max(1, _speed   + delta); }

    // ── Status Effects ───────────────────────────────────────────

    /// <summary>Attaches a status effect if not already present.</summary>
    public void ApplyStatusEffect(IStatusEffect effect)
    {
        if (_activeEffects.Exists(e => e.EffectType == effect.EffectType))
        {
            Debug.Log($"[{Name}] already has {effect.EffectType}. Refreshing duration.");
            _activeEffects.Find(e => e.EffectType == effect.EffectType)?.Refresh();
            return;
        }
        effect.Apply(this);
        _activeEffects.Add(effect);
        Debug.Log($"[{Name}] gained status: {effect.EffectType}");
    }

    /// <summary>Called at the end of a unit's turn to tick all effects.</summary>
    public void TickStatusEffects()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Tick(this);
            if (_activeEffects[i].IsExpired)
            {
                _activeEffects[i].Remove(this);
                _activeEffects.RemoveAt(i);
            }
        }
    }

    public IReadOnlyList<IStatusEffect> ActiveEffects => _activeEffects;
}