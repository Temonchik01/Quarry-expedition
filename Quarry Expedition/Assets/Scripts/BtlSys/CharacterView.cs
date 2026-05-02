using System.Collections.Generic;
using JRPG.Core;
using JRPG.Core.Interfaces;
using JRPG.Data;
using UnityEngine;

namespace JRPG.Battle
{
    /// <summary>
    /// MonoBehaviour presenter for a single character (hero or enemy).
    /// Owns the sprite renderer and animator.
    /// Subscribes to BattleEvents for animation triggers.
    ///
    /// Inspector Setup:
    ///   - Assign StatsSO (CharacterStatsSO asset)
    ///   - Assign AIBehaviourSO (EnemyAIBehaviourSO asset) — enemies only
    ///   - Toggle IsPlayerCharacter
    ///   - Wire the Animator reference (or leave null; it will TryGetComponent)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterView : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CharacterStatsSO     _statsSO;
        [SerializeField] private EnemyAIBehaviourSO   _aiBehaviourSO;
        [SerializeField] private bool                 _isPlayerCharacter = true;

        [Header("Visuals")]
        [SerializeField] private Animator             _animator;
        [SerializeField] private SpriteRenderer       _spriteRenderer;

        // ── Runtime binding ───────────────────────────────────────────────────
        private ICharacter _runtime;

        // ── Public accessors for BattleManager ───────────────────────────────
        public CharacterStatsSO   StatsSO          => _statsSO;
        public EnemyAIBehaviourSO AIBehaviourSO    => _aiBehaviourSO;
        public bool               IsPlayerCharacter => _isPlayerCharacter;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_animator == null) TryGetComponent(out _animator);
            if (_spriteRenderer == null) TryGetComponent(out _spriteRenderer);
        }

        private void OnEnable()
        {
            BattleEvents.OnAttackExecuted   += HandleAttackExecuted;
            BattleEvents.OnDamageDealt      += HandleDamageDealt;
            BattleEvents.OnCharacterDefeated += HandleDefeated;
        }

        private void OnDisable()
        {
            BattleEvents.OnAttackExecuted   -= HandleAttackExecuted;
            BattleEvents.OnDamageDealt      -= HandleDamageDealt;
            BattleEvents.OnCharacterDefeated -= HandleDefeated;
        }

        /// <summary>Called by BattleManager after creating the CharacterRuntime.</summary>
        public void Bind(ICharacter runtime)
        {
            _runtime = runtime;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleAttackExecuted(
            ICharacter attacker, AttackDataSO attack, IReadOnlyList<ICharacter> targets)
        {
            if (attacker != _runtime) return;
            if (_animator != null && !string.IsNullOrEmpty(attack.AnimationTrigger))
                _animator.SetTrigger(attack.AnimationTrigger);
        }

        private void HandleDamageDealt(ICharacter target, int amount)
        {
            if (target != _runtime) return;
            // Flash effect — quick & allocation-free
            _spriteRenderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.15f);
        }

        private void HandleDefeated(ICharacter character)
        {
            if (character != _runtime) return;
            if (_animator != null) _animator.SetTrigger("Defeat");
            _spriteRenderer.color = new Color(1f, 1f, 1f, 0.3f);
        }

        private void ResetColor() => _spriteRenderer.color = Color.white;
    }
}