using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BtlSys
{
    public class BattleManager : MonoBehaviour
    {
        private static BattleManager instance;
        public static BattleManager Instance => instance != null ? instance : FindFirstObjectByType<BattleManager>();

        public List<Combatant> playerTeam = new List<Combatant>();
        public List<Combatant> enemyTeam = new List<Combatant>();
        public BattleState currentState = BattleState.Initializing;

        [SerializeField, Min(0f)] private float actionDelay = 0.5f;

        public Combatant CurrentHero { get; private set; }
        public event Action<Combatant> onHeroTurnStarted;
        public event Action onRoundExecutionStarted;
        public event Action<bool> onBattleEnded;

        private readonly List<BattleAction> roundActions = new List<BattleAction>();
        private readonly Dictionary<Combatant, BattleAction> pendingCharges = new Dictionary<Combatant, BattleAction>();
        private readonly BattleActionResolver resolver = new BattleActionResolver();
        private readonly EnemyActionPlanner enemyPlanner = new EnemyActionPlanner();
        private readonly BattleTurnOrder turnOrder = new BattleTurnOrder();
        private BattlePhase phase;
        private Coroutine roundRoutine;
        private int currentHeroInputIndex;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            StartBattle();
        }

        private void OnDestroy()
        {
            UnsubscribeDeaths();
            if (instance == this) instance = null;
        }

        public void StartBattle()
        {
            if (roundRoutine != null) StopCoroutine(roundRoutine);
            roundRoutine = null;
            UnsubscribeDeaths();
            phase = null;
            pendingCharges.Clear();
            roundActions.Clear();
            playerTeam.Clear();
            enemyTeam.Clear();
            currentState = BattleState.Initializing;

            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.hasHealedInCamp = false;

            Combatant[] combatants = FindObjectsByType<Combatant>(FindObjectsSortMode.InstanceID);
            foreach (Combatant combatant in combatants)
            {
                if (combatant == null || combatant.gameObject.scene != gameObject.scene) continue;
                combatant.Initialize();
                if (combatant.isPlayer) playerTeam.Add(combatant);
                else enemyTeam.Add(combatant);
                combatant.onDeath += HandleCombatantDeath;
            }

            if (ProgressionManager.Instance != null)
                ProgressionManager.Instance.LoadPlayerState(playerTeam);

            Debug.Log($"[Battle] Started with {playerTeam.Count} heroes and {enemyTeam.Count} enemies.");
            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
            {
                Debug.LogError("[Battle] Both teams need at least one combatant.", this);
                return;
            }
            if (CheckBattleEnd()) return;
            ChangePhase(new CollectActionsPhase(this));
        }

        public bool RegisterHeroAction(BattleAction action)
        {
            if (!(phase is CollectActionsPhase)) return false;
            if (action == null || action.User != CurrentHero || !CurrentHero.IsAlive) return false;

            if (action.Type == BattleActionType.Attack)
            {
                AttackData attack = action.Attack != null ? action.Attack : CurrentHero.data?.basicAttack;
                if (attack == null || !CurrentHero.CanUse(attack)) return false;
                if (attack != CurrentHero.data.basicAttack &&
                    (CurrentHero.data.specials == null || !CurrentHero.data.specials.Contains(attack))) return false;

                if (attack.targetType == TargetType.SingleTarget)
                {
                    IReadOnlyList<Combatant> party = attack.targetAllies ? playerTeam : enemyTeam;
                    if (action.Target == null || !action.Target.IsAlive || !Contains(party, action.Target)) return false;
                }
            }

            AddAction(action);
            currentHeroInputIndex++;
            CurrentHero = null;
            RequestNextHero();
            return true;
        }

        private void ChangePhase(BattlePhase next)
        {
            phase?.Exit();
            phase = next;
            currentState = next.State;
            next.Enter();
        }

        private void StartCollecting()
        {
            roundActions.Clear();
            currentHeroInputIndex = 0;
            CurrentHero = null;
            foreach (Combatant combatant in playerTeam) combatant.BeginRound();
            foreach (Combatant combatant in enemyTeam) combatant.BeginRound();
            if (!CheckBattleEnd()) RequestNextHero();
        }

        private void RequestNextHero()
        {
            while (currentHeroInputIndex < playerTeam.Count)
            {
                Combatant hero = playerTeam[currentHeroInputIndex];
                if (hero == null || !hero.IsAlive)
                {
                    currentHeroInputIndex++;
                    continue;
                }
                if (pendingCharges.TryGetValue(hero, out BattleAction charged))
                {
                    pendingCharges.Remove(hero);
                    AddAction(charged);
                    currentHeroInputIndex++;
                    continue;
                }
                CurrentHero = hero;
                onHeroTurnStarted?.Invoke(hero);
                return;
            }
            CurrentHero = null;
            ChangePhase(new ResolveActionsPhase(this));
        }

        private void StartResolving()
        {
            onRoundExecutionStarted?.Invoke();
            foreach (Combatant enemy in enemyTeam)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (pendingCharges.TryGetValue(enemy, out BattleAction charged))
                {
                    pendingCharges.Remove(enemy);
                    AddAction(charged);
                }
                else
                {
                    BattleAction choice = enemyPlanner.Choose(enemy, playerTeam);
                    if (choice != null) AddAction(choice);
                }
            }
            roundActions.Sort(turnOrder);
            roundRoutine = StartCoroutine(ResolveRound());
        }

        private IEnumerator ResolveRound()
        {
            foreach (BattleAction action in roundActions)
            {
                if (!(phase is ResolveActionsPhase) || CheckBattleEnd()) yield break;
                resolver.Resolve(action, playerTeam, enemyTeam, QueueCharge);
                if (!(phase is ResolveActionsPhase) || CheckBattleEnd()) yield break;
                if (actionDelay > 0f) yield return new WaitForSeconds(actionDelay);
            }
            roundRoutine = null;
            if (!CheckBattleEnd()) ChangePhase(new CollectActionsPhase(this));
        }

        private void AddAction(BattleAction action)
        {
            action.Sequence = roundActions.Count;
            roundActions.Add(action);
        }

        private void QueueCharge(BattleAction action)
        {
            pendingCharges[action.User] = action;
        }

        private void HandleCombatantDeath()
        {
            CheckBattleEnd();
        }

        private bool CheckBattleEnd()
        {
            if (phase is FinishedPhase) return true;
            if (AllDefeated(playerTeam))
            {
                ChangePhase(new FinishedPhase(this, false));
                return true;
            }
            if (AllDefeated(enemyTeam))
            {
                ChangePhase(new FinishedPhase(this, true));
                return true;
            }
            return false;
        }

        private void CompleteBattle(bool won)
        {
            if (won && ProgressionManager.Instance != null)
                ProgressionManager.Instance.SavePlayerState(playerTeam);
            Debug.Log(won ? "[Battle] Victory." : "[Battle] Defeat.");
            onBattleEnded?.Invoke(won);
        }

        private void UnsubscribeDeaths()
        {
            foreach (Combatant combatant in playerTeam)
                if (combatant != null) combatant.onDeath -= HandleCombatantDeath;
            foreach (Combatant combatant in enemyTeam)
                if (combatant != null) combatant.onDeath -= HandleCombatantDeath;
        }

        private static bool AllDefeated(IReadOnlyList<Combatant> team)
        {
            if (team.Count == 0) return false;
            foreach (Combatant combatant in team)
                if (combatant != null && combatant.IsAlive) return false;
            return true;
        }

        private static bool Contains(IReadOnlyList<Combatant> team, Combatant target)
        {
            foreach (Combatant combatant in team)
                if (combatant == target) return true;
            return false;
        }

        private abstract class BattlePhase
        {
            protected readonly BattleManager Battle;
            protected BattlePhase(BattleManager battle) { Battle = battle; }
            public abstract BattleState State { get; }
            public abstract void Enter();
            public virtual void Exit() { }
        }

        private sealed class CollectActionsPhase : BattlePhase
        {
            public CollectActionsPhase(BattleManager battle) : base(battle) { }
            public override BattleState State => BattleState.PlayerInput;
            public override void Enter() { Battle.StartCollecting(); }
        }

        private sealed class ResolveActionsPhase : BattlePhase
        {
            public ResolveActionsPhase(BattleManager battle) : base(battle) { }
            public override BattleState State => BattleState.ActionPhase;
            public override void Enter() { Battle.StartResolving(); }
        }

        private sealed class FinishedPhase : BattlePhase
        {
            private readonly bool won;
            public FinishedPhase(BattleManager battle, bool won) : base(battle) { this.won = won; }
            public override BattleState State => won ? BattleState.Victory : BattleState.Defeat;
            public override void Enter() { Battle.CompleteBattle(won); }
        }
    }
}
