using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BtlSys
{
    public class BattleAction
    {
        public Combatant user;
        public List<Combatant> targets;
        public AttackData attack;
        public bool isDefend;
        public bool isReload;
        public int speed;

        public BattleAction(Combatant user, List<Combatant> targets, AttackData attack)
        {
            this.user = user;
            this.targets = targets;
            this.attack = attack;
            
            if (user != null)
            {
                this.speed = user.currentSpeed + (attack != null && attack.hasPriority ? 1000 : 0);
            }
            else
            {
                this.speed = 0;
            }
        }
    }

    public class BattleManager : MonoBehaviour
    {
        private static BattleManager _instance;
        public static BattleManager Instance 
        { 
            get 
            {
                if (_instance == null) _instance = Object.FindFirstObjectByType<BattleManager>();
                return _instance;
            }
        }

        public List<Combatant> playerTeam = new List<Combatant>();
        public List<Combatant> enemyTeam = new List<Combatant>();

        public BattleState currentState = BattleState.Initializing;
        
        private List<BattleAction> roundActions = new List<BattleAction>();
        private int currentHeroInputIndex = 0;

        public System.Action<Combatant> onHeroTurnStarted;
        public System.Action onRoundExecutionStarted;
        public System.Action<string> onBattleEnded;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            StartBattle();
        }

        public void StartBattle()
        {
            Debug.Log("[BattleManager] Starting Battle...");
            
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.hasHealedInCamp = false;
            }

            // Clear lists to ensure fresh start
            playerTeam.Clear();
            enemyTeam.Clear();

            var allCombatants = Object.FindObjectsByType<Combatant>(FindObjectsSortMode.None);
            Debug.Log($"[BattleManager] Found {allCombatants.Length} combatants in scene.");

            foreach (var c in allCombatants)
            {
                c.Initialize(); // Ensure they are initialized
                if (c.isPlayer) playerTeam.Add(c);
                else enemyTeam.Add(c);
            }

            // Load persistent state if it exists
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.LoadPlayerState(playerTeam);
            }
            
            Debug.Log($"[BattleManager] Teams populated: Players={playerTeam.Count}, Enemies={enemyTeam.Count}");

            foreach (var c in playerTeam.Concat(enemyTeam))
            {
                c.onDeath += () => CheckBattleEnd();
            }

            if (playerTeam.Count == 0 || enemyTeam.Count == 0)
            {
                Debug.LogError("[BattleManager] Battle cannot start: One or more teams are empty!");
            }

            StartNewRound();
        }

        private void StartNewRound()
        {
            Debug.Log("[BattleManager] Starting New Round...");
            roundActions.Clear();
            currentHeroInputIndex = 0;
            
            foreach (var c in playerTeam.Concat(enemyTeam))
            {
                c.ResetTemporaryStats();
                c.TickStatusEffects();
            }

            if (CheckBattleEnd()) 
            {
                Debug.Log("[BattleManager] Battle ended during round start check.");
                return;
            }

            currentState = BattleState.PlayerInput;
            RequestNextHeroInput();
        }

        private void RequestNextHeroInput()
        {
            if (currentHeroInputIndex < playerTeam.Count)
            {
                var hero = playerTeam[currentHeroInputIndex];
                if (hero.currentHP > 0)
                {
                    Debug.Log($"[BattleManager] Requesting input for hero: {hero.name} (HP: {hero.currentHP})");
                    onHeroTurnStarted?.Invoke(hero);
                }
                else
                {
                    Debug.Log($"[BattleManager] Hero {hero.name} is down, skipping turn.");
                    currentHeroInputIndex++;
                    RequestNextHeroInput();
                }
            }
            else
            {
                Debug.Log("[BattleManager] All heroes have acted. Generating enemy actions...");
                GenerateEnemyActions();
                ExecuteRound();
            }
        }

        public void RegisterHeroAction(BattleAction action)
        {
            roundActions.Add(action);
            currentHeroInputIndex++;
            RequestNextHeroInput();
        }

        private void GenerateEnemyActions()
        {
            foreach (var enemy in enemyTeam)
            {
                if (enemy.currentHP <= 0) continue;

                // Simple AI: Attack random player
                var target = playerTeam.Where(p => p.currentHP > 0).OrderBy(x => Random.value).FirstOrDefault();
                if (target != null)
                {
                    // For now, assume enemy has a basic attack
                    // In a real game, enemies would have their own AttackData
                    roundActions.Add(new BattleAction(enemy, new List<Combatant> { target }, null));
                }
            }
        }

        private async void ExecuteRound()
        {
            currentState = BattleState.ActionPhase;
            onRoundExecutionStarted?.Invoke();

            // Sort by speed (higher speed acts first)
            var sortedActions = roundActions.OrderByDescending(a => a.speed).ToList();

            foreach (var action in sortedActions)
            {
                if (action == null || action.user == null) continue;
                if (action.user.currentHP <= 0) continue;
                if (CheckBattleEnd()) break;

                await PerformAction(action);
                
                // Small delay between actions
                await System.Threading.Tasks.Task.Delay(1000);
            }

            if (!CheckBattleEnd())
            {
                StartNewRound();
            }
        }

        private async System.Threading.Tasks.Task PerformAction(BattleAction action)
        {
            Debug.Log($"{action.user.name} is acting!");

            if (action.isDefend)
            {
                action.user.Defend();
            }
            else if (action.isReload)
            {
                action.user.ReplenishAmmo(20);
            }
            else if (action.attack != null)
            {
                action.user.ConsumeAmmo(action.attack.ammoCost);
                
                foreach (var target in action.targets)
                {
                    if (target.currentHP <= 0) continue;

                    // Accuracy check
                    if (Random.Range(0, 100) < action.attack.accuracy)
                    {
                        if (action.attack.isStatusAttack)
                        {
                            target.ModifyStat(action.attack.modifiedStat, action.attack.statModifierValue);
                        }
                        else
                        {
                            for (int i = 0; i < action.attack.hits; i++)
                            {
                                target.TakeDamage(action.user.currentAttack + action.attack.power);
                            }
                        }

                        // Apply Status Effect
                        if (action.attack.statusEffect != StatusEffectType.None && Random.value < action.attack.statusChance)
                        {
                            target.ApplyStatusEffect(action.attack.statusEffect, 40);
                        }
                    }
                    else
                    {
                        Debug.Log("Missed!");
                    }
                }
            }
            else // Basic Enemy Attack (fallback)
            {
                foreach (var target in action.targets)
                {
                    target.TakeDamage(action.user.currentAttack);
                }
            }

            await System.Threading.Tasks.Task.Delay(500);
        }

        private bool CheckBattleEnd()
        {
            if (playerTeam.Count > 0 && playerTeam.All(p => p.currentHP <= 0))
            {
                currentState = BattleState.Defeat;
                onBattleEnded?.Invoke("You Lost");
                return true;
            }
            if (enemyTeam.Count > 0 && enemyTeam.All(e => e.currentHP <= 0))
            {
                currentState = BattleState.Victory;

                // Save state upon winning
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.SavePlayerState(playerTeam);
                }

                onBattleEnded?.Invoke("You Won");
                return true;
            }
            return false;
        }
    }
}
