using System.Collections;
using JRPG.Core.Interfaces;
using JRPG.Battle;
using UnityEngine;

namespace JRPG.Core.Battle
{
    // ─────────────────────────────────────────────────────────────────────────────
    // State Pattern: each battle phase is a self-contained object.
    // BattleStateMachine owns the current state and drives transitions.
    // ─────────────────────────────────────────────────────────────────────────────

    public interface IBattleState
    {
        IEnumerator Execute(BattleStateMachine machine);
    }

    // ─────────────────────────────────────────────────────────────────────────────

    public sealed class BattleStartState : IBattleState
    {
        public IEnumerator Execute(BattleStateMachine m)
        {
            BattleEvents.RaiseBattleStarted();
            yield return new WaitForSeconds(0.5f);   // presentation pause
            m.TransitionTo(new PlayerInputState());
        }
    }

    public sealed class PlayerInputState : IBattleState
    {
        public IEnumerator Execute(BattleStateMachine m)
        {
            // Signal UI to show the attack menu.
            // UI writes back via BattleStateMachine.SubmitPlayerAction().
            m.IsAwaitingInput = true;
            yield return new WaitUntil(() => !m.IsAwaitingInput);
        }
    }

    public sealed class EnemyDecisionState : IBattleState
    {
        public IEnumerator Execute(BattleStateMachine m)
        {
            m.ResolveEnemyActions();
            yield return null;
            m.TransitionTo(new ExecuteTurnState());
        }
    }

    public sealed class ExecuteTurnState : IBattleState
    {
        public IEnumerator Execute(BattleStateMachine m)
        {
            yield return m.ExecuteAllActionsCoroutine();
            m.TransitionTo(m.CheckBattleOver()
                ? (IBattleState) new BattleEndState()
                : new PlayerInputState());
        }
    }

    public sealed class BattleEndState : IBattleState
    {
        public IEnumerator Execute(BattleStateMachine m)
        {
            bool playerWon = m.PlayerTeam.Exists(c => c.IsAlive);
            BattleEvents.RaiseBattleEnded(playerWon);
            yield break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hosts the state machine and drives coroutines.
    /// MonoBehaviour wrapper (BattleManager) calls StartCoroutine on this.
    /// </summary>
    public sealed class BattleStateMachine
    {
        public  bool IsAwaitingInput { get; set; }
        public  System.Collections.Generic.List<ICharacter> PlayerTeam { get; }
        public  System.Collections.Generic.List<ICharacter> EnemyTeam  { get; }

        private IBattleState _currentState;
        private readonly BattleManager _manager;   // back-reference for coroutine hosting

        public BattleStateMachine(
            System.Collections.Generic.List<ICharacter> players,
            System.Collections.Generic.List<ICharacter> enemies,
            BattleManager manager)
        {
            PlayerTeam = players;
            EnemyTeam  = enemies;
            _manager   = manager;
        }

        public void Start()
        {
            TransitionTo(new BattleStartState());
        }

        public void TransitionTo(IBattleState next)
        {
            _currentState = next;
            _manager.StartCoroutine(next.Execute(this));
        }

        public bool CheckBattleOver()
        {
            return !PlayerTeam.Exists(c => c.IsAlive)
                || !EnemyTeam.Exists(c => c.IsAlive);
        }

        public void ResolveEnemyActions()     => _manager.ResolveEnemyActions_Internal();
        public IEnumerator ExecuteAllActionsCoroutine() => _manager.ExecuteAllActions_Internal();
        public void SubmitPlayerAction(ICharacter actor, IAttack attack,
                                        System.Collections.Generic.IReadOnlyList<ICharacter> targets)
            => _manager.SubmitPlayerAction_Internal(actor, attack, targets);
    }
}