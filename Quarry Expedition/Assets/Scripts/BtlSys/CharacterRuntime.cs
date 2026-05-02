using System;
using System.Collections.Generic;
using JRPG.Core.Interfaces;
using JRPG.Data;
using UnityEngine;

namespace JRPG.Core
{
    /// <summary>
    /// Pure C# runtime representation of a character in battle.
    /// No MonoBehaviour dependency — fully unit-testable.
    ///
    /// Stat Formulas (from spec):
    ///   MaxHp   = BaseDefense * 10
    ///   MaxAmmo = BaseAmmo    * 5
    ///
    /// Effective stats account for status effect penalties.
    /// </summary>
    public sealed class CharacterRuntime : ICharacter
    {
        // ── Base values (from ScriptableObject, immutable after construction) ──
        private readonly int _baseAttack;
        private readonly int _baseDefense;
        private readonly int _baseSpeed;

        // ── Mutable battle state ───────────────────────────────────────────────
        private int _currentHp;
        private int _currentAmmo;

        // Flat modifiers from status effects (can be negative)
        private int _attackModifier;
        private int _defenseModifier;
        private int _speedModifier;

        private readonly List<IStatusEffect> _activeEffects = new();

        // ── ICharacter implementation ─────────────────────────────────────────
        public CharacterStatsSO BaseData       { get; }
        public string           CharacterName  => BaseData.CharacterName;
        public int              MaxHp          { get; }
        public int              MaxAmmo        { get; }
        public int              CurrentHp      => _currentHp;
        public int              CurrentAmmo    => _currentAmmo;
        public bool             IsAlive        => _currentHp > 0;
        public AttackDataSO[]   Attacks        => BaseData.AvailableAttacks;

        public IReadOnlyList<IStatusEffect> ActiveEffects => _activeEffects;

        public int EffectiveAttack  => Mathf.Max(0, _baseAttack  + _attackModifier);
        public int EffectiveDefense => Mathf.Max(0, _baseDefense + _defenseModifier);
        public int EffectiveSpeed   => Mathf.Max(0, _baseSpeed   + _speedModifier);

        // ── Constructor ───────────────────────────────────────────────────────
        public CharacterRuntime(CharacterStatsSO data)
        {
            BaseData      = data ?? throw new ArgumentNullException(nameof(data));
            _baseAttack   = data.BaseAttack;
            _baseDefense  = data.BaseDefense;
            _baseSpeed    = data.BaseSpeed;

            MaxHp         = data.BaseDefense * 10;    // spec formula
            MaxAmmo       = data.BaseAmmo    * 5;     // spec formula
            _currentHp    = MaxHp;
            _currentAmmo  = MaxAmmo;
        }

        // ── ICharacterMutator ─────────────────────────────────────────────────

        /// <summary>
        /// Applies damage after defense absorption.
        /// Final damage = max(1, rawDamage − EffectiveDefense)
        /// </summary>
        public void ApplyDamage(int rawDamage)
        {
            int absorbed    = Mathf.Max(1, rawDamage - EffectiveDefense);
            _currentHp      = Mathf.Max(0, _currentHp - absorbed);
        }

        public void Heal(int amount)
        {
            _currentHp = Mathf.Min(MaxHp, _currentHp + amount);
        }

        public void ConsumeAmmo(int amount)
        {
            _currentAmmo = Mathf.Max(0, _currentAmmo - amount);
        }

        /// <summary>Flat modifier — status effects call this with negative deltas.</summary>
        public void ModifyStat(StatType stat, int delta)
        {
            switch (stat)
            {
                case StatType.Attack:  _attackModifier  += delta; break;
                case StatType.Defense: _defenseModifier += delta; break;
                case StatType.Speed:   _speedModifier   += delta; break;
                case StatType.Ammo:    _currentAmmo = Mathf.Max(0, _currentAmmo + delta); break;
            }
        }

        public void ApplyStatusEffect(IStatusEffect effect)
        {
            // One stack of each kind maximum
            if (_activeEffects.Exists(e => e.Kind == effect.Kind)) return;
            _activeEffects.Add(effect);
            effect.Apply(this);
        }

        public void RemoveStatusEffect(StatusEffectKind kind)
        {
            int idx = _activeEffects.FindIndex(e => e.Kind == kind);
            if (idx < 0) return;
            _activeEffects[idx].Remove(this);
            _activeEffects.RemoveAt(idx);
        }

        /// <summary>
        /// Called by BattleManager at the start of each turn for this character.
        /// Ticks all active effects and removes expired ones.
        /// </summary>
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
    }
}