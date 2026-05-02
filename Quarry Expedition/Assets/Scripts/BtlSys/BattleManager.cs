using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JRPG.Core;          
using JRPG.Core.AI;
using JRPG.Core.Attacks;
using JRPG.Core.Battle;
using JRPG.Core.Interfaces;
using JRPG.Data;
using UnityEngine;


namespace JRPG.Battle
{
    /// <summary>
    /// MonoBehaviour entry-point for a battle scene.
    /// Responsibilities (SRP — each bullet is thin):
    ///   1. Collects hero/enemy CharacterView references from the scene.
    ///   2. Builds CharacterRuntime objects from their ScriptableObject data.
    ///   3. Instantiates the BattleStateMachine and starts it.
    ///   4. Exposes internal methods called by the state machine.
    ///   5. Receives player action choices from BattleUIManager via events.
    ///
    /// It does NOT: calculate damage, resolve status effects, pick AI moves.
    /// Those concerns live in the core layer.
    ///
    /// Inspector setup:
    ///   - Add HeroCharacterView components in the scene; they register themselves.
    ///   - Add EnemyCharacterView components; they register themselves.
    ///   - This MB auto-discovers both at Awake.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float _actionDelay = 1.2f;

        // ── Runtime state ──────────────────────────────────────────────────────
        private List<ICharacter>      _playerTeam  = new();
        private List<ICharacter>      _enemyTeam   = new();
        private List<ICharacter>      _allCombatants = new();
        private BattleStateMachine    _stateMachine;

        // Pending action table keyed by combatant
        private Dictionary<ICharacter, (IAttack attack, IReadOnlyList<ICharacter> targets)>
            _pendingActions = new();

        // Enemy AI map: one IEnemyAI strategy per enemy
        private Dictionary<ICharacter, IEnemyAI> _enemyAI = new();

        // ── Context implementation (inline, avoids extra class) ────────────────
        private sealed class BattleContext : IBattleContext
        {
            public IReadOnlyList<ICharacter> PlayerTeam { get; set; }
            public IReadOnlyList<ICharacter> EnemyTeam  { get; set; }
            public ICharacter                Attacker   { get; set; }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            CollectTeams();
        }

        private void Start()
        {
            _stateMachine = new BattleStateMachine(_playerTeam, _enemyTeam, this);
            _stateMachine.Start();
        }

        // ── Team collection (auto-discovery) ─────────────────────────────────

        /// <summary>
        /// Finds all CharacterView MBs in the scene and separates them into teams.
        /// CharacterView tags itself as "Player" or "Enemy" via its data.
        /// </summary>
        private void CollectTeams()
        {
            var views = FindObjectsOfType<CharacterView>();
            foreach (CharacterView view in views)
            {
                CharacterRuntime runtime = new(view.StatsSO);
                view.Bind(runtime);

                if (view.IsPlayerCharacter)
                {
                    _playerTeam.Add(runtime);
                }
                else
                {
                    _enemyTeam.Add(runtime);
                    EnemyAIBehaviourSO behaviourData = view.AIBehaviourSO;
                    _enemyAI[runtime] = EnemyAIStrategyFactory.Create(behaviourData);
                }
            }

            _allCombatants = _playerTeam.Concat(_enemyTeam).ToList();
        }

        // ── Internal methods exposed to BattleStateMachine ───────────────────

        /// <summary>Called by EnemyDecisionState to populate pending actions for all enemies.</summary>
        internal void ResolveEnemyActions_Internal()
        {
            BattleContext ctx = new()
            {
                PlayerTeam = _playerTeam,
                EnemyTeam  = _enemyTeam
            };

            foreach (ICharacter enemy in _enemyTeam)
            {
                if (!enemy.IsAlive) continue;
                ctx.Attacker = enemy;
                var (attack, targets) = _enemyAI[enemy].ChooseAction(enemy, ctx);
                if (attack != null)
                    _pendingActions[enemy] = (attack, targets);
            }
        }

        /// <summary>Called by PlayerInputState (via BattleUIManager) when player chooses.</summary>
        internal void SubmitPlayerAction_Internal(
            ICharacter actor, IAttack attack, IReadOnlyList<ICharacter> targets)
        {
            _pendingActions[actor] = (attack, targets);
            // Once all heroes have submitted, stop awaiting
            bool allIn = _playerTeam.Where(c => c.IsAlive)
                                     .All(c => _pendingActions.ContainsKey(c));
            if (allIn) _stateMachine.IsAwaitingInput = false;

            // Transition to enemy decision
            _stateMachine.TransitionTo(new EnemyDecisionState());
        }

        /// <summary>Executes all pending actions in Speed order, with per-action delay.</summary>
        internal IEnumerator ExecuteAllActions_Internal()
        {
            Dictionary<ICharacter, IAttack> attackMap = _pendingActions
                .ToDictionary(kv => kv.Key, kv => kv.Value.attack);

            List<ICharacter> ordered = TurnOrderSystem.BuildTurnOrder(_allCombatants, attackMap);

            BattleContext ctx = new()
            {
                PlayerTeam = _playerTeam,
                EnemyTeam  = _enemyTeam
            };

            foreach (ICharacter actor in ordered)
            {
                if (!actor.IsAlive) continue;
                if (!_pendingActions.TryGetValue(actor, out var actionData)) continue;

                actor.TickStatusEffects();
                if (!actor.IsAlive) { HandleDefeat(actor); continue; }

                ctx.Attacker = actor;
                BattleEvents.RaiseTurnStarted(actor);
                BattleEvents.RaiseAttackExecuted(actor, actionData.attack.Data, actionData.targets);

                actionData.attack.Execute(actor, actionData.targets, ctx);

                // Check for newly defeated targets
                foreach (ICharacter target in actionData.targets)
                {
                    if (!target.IsAlive) HandleDefeat(target);
                }

                yield return new WaitForSeconds(_actionDelay);
            }

            _pendingActions.Clear();
        }

        private void HandleDefeat(ICharacter character)
        {
            BattleEvents.RaiseCharacterDefeated(character);
        }
    }
}