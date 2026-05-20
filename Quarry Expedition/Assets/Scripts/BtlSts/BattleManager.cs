using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.Cinemachine;
using System.Collections;

namespace BtlSts
{
    public enum BattleState { Start, TurnOrder, PlayerDecision, Action, EnemyTurn, End }

    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        public List<BattleCharacter> playerTeam = new List<BattleCharacter>();
        public List<BattleCharacter> enemyTeam = new List<BattleCharacter>();
        
        private List<BattleCharacter> turnQueue = new List<BattleCharacter>();
        private BattleCharacter currentCharacter;
        private BattleState currentState;

        public CinemachineCamera vcam;

        public event Action<BattleState> OnStateChanged;
        public event Action<BattleCharacter> OnTurnStarted;

        private void Awake()
        {
            Instance = this;
            if (vcam == null) vcam = FindFirstObjectByType<CinemachineCamera>();
        }

        private void Start()
        {
            SetupBattle();
        }

        public void SetupBattle()
        {
            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
            {
                var allChars = FindObjectsByType<BattleCharacter>(FindObjectsSortMode.None);
                playerTeam.Clear();
                enemyTeam.Clear();
                foreach (var c in allChars)
                {
                    if (c.IsEnemy) enemyTeam.Add(c);
                    else playerTeam.Add(c);
                }
            }

            // Initialize the Party UI
            BattleUI ui = FindFirstObjectByType<BattleUI>();
            if (ui != null)
            {
                ui.InitializePartyList(playerTeam);
                ui.InitializeEnemyUI(enemyTeam);
            }
            
            ChangeState(BattleState.TurnOrder);
        }

        private void ChangeState(BattleState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);

            switch (currentState)
            {
                case BattleState.TurnOrder:
                    DetermineTurnOrder();
                    break;
                case BattleState.PlayerDecision:
                    currentCharacter.currentDefenseModifier = 0; // Reset defend bonus
                    SetCameraFocus(currentCharacter.transform);
                    OnTurnStarted?.Invoke(currentCharacter);
                    break;
                case BattleState.EnemyTurn:
                    currentCharacter.currentDefenseModifier = 0; // Reset defend bonus
                    SetCameraFocus(currentCharacter.transform);
                    ExecuteEnemyAI();
                    break;
            }
        }

        private void SetCameraFocus(Transform target)
        {
            if (vcam != null)
            {
                vcam.LookAt = target;
                vcam.Follow = target;
            }
        }

        private void DetermineTurnOrder()
        {
            // At the start of a new round (when turnQueue is empty), add 1 BP to everyone
            foreach (var c in playerTeam.Concat(enemyTeam).Where(c => !c.IsDead))
            {
                c.AddBP(1);
            }

            turnQueue = playerTeam.Concat(enemyTeam)
                .Where(c => !c.IsDead)
                .OrderByDescending(c => c.GetEffectiveSpeed())
                .ToList();

            if (turnQueue.Count > 0)
            {
                NextTurn();
            }
        }

        private void NextTurn()
        {
            if (turnQueue.Count == 0)
            {
                ChangeState(BattleState.TurnOrder);
                return;
            }

            currentCharacter = turnQueue[0];
            turnQueue.RemoveAt(0);

            if (currentCharacter.IsDead)
            {
                NextTurn();
                return;
            }

            if (!currentCharacter.IsEnemy)
            {
                ChangeState(BattleState.PlayerDecision);
            }
            else
            {
                ChangeState(BattleState.EnemyTurn);
            }
        }

        public void ExecuteAction(AttackActionData action, BattleCharacter user, List<BattleCharacter> targets)
        {
            ChangeState(BattleState.Action);
            
            // Camera focus on target during action
            if (targets.Count > 0) SetCameraFocus(targets[0].transform);

            user.ConsumeAmmo(action.ammoCost);

            foreach (var target in targets)
            {
                for (int i = 0; i < action.hitCount; i++)
                {
                    if (UnityEngine.Random.Range(0, 100) <= action.accuracy)
                    {
                        if (action.power > 0)
                        {
                            int damage = (int)(user.GetEffectiveAttack() * (action.power / 100f));
                            target.TakeDamage(damage);
                        }

                        if (action.statusEffect != StatusEffectType.None)
                        {
                            if (UnityEngine.Random.value <= action.statusChance)
                            {
                                target.ApplyStatusEffect(action.statusEffect, action.statModifierValue, action.statusDuration);
                            }
                        }
                    }
                }
            }

            Invoke(nameof(NextTurn), 1.5f);
        }

        public void SkipTurn(BattleCharacter user, bool isReload)
        {
            if (isReload)
            {
                user.RestoreAmmo(20);
                // Log or show feedback
                Debug.Log($"{user.stats.characterName} reloaded and gained 20 Ammo/SP.");
            }
            else
            {
                user.currentDefenseModifier += 100;
                Debug.Log($"{user.stats.characterName} is defending (+100 Def).");
            }
            
            NextTurn();
        }

        private void ExecuteEnemyAI()
        {
            var availablePlayers = playerTeam.Where(p => !p.IsDead).ToList();
            if (availablePlayers.Count == 0) return;

            // Pick a random player target
            var targets = new List<BattleCharacter> { availablePlayers[UnityEngine.Random.Range(0, availablePlayers.Count)] };
            
            // Basic AI: Pick a random attack from the character's stats
            AttackActionData attack;
            if (currentCharacter.stats.availableAttacks != null && currentCharacter.stats.availableAttacks.Count > 0)
            {
                attack = currentCharacter.stats.availableAttacks[UnityEngine.Random.Range(0, currentCharacter.stats.availableAttacks.Count)];
            }
            else
            {
                // Fallback if no attacks defined
                Debug.LogWarning($"{currentCharacter.stats.characterName} has no attacks defined!");
                NextTurn();
                return;
            }
            
            StartCoroutine(EnemyAIDelay(attack, targets));
        }

        private IEnumerator EnemyAIDelay(AttackActionData attack, List<BattleCharacter> targets)
        {
            yield return new WaitForSeconds(1.0f);
            ExecuteAction(attack, currentCharacter, targets);
        }
    }
}

