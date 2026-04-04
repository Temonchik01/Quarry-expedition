using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Orchestrates a single battle encounter.
/// Uses the State Pattern for turn phases and the Observer Pattern
/// (C# events) to notify UI / animations without coupling.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ── Inspector references ─────────────────────────────────────
    [SerializeField] private List<BaseCharacterUnit> _playerUnits;
    [SerializeField] private List<BaseCharacterUnit> _enemyUnits;
    [SerializeField] private float _actionDelaySeconds = 1.2f;

    // ── Events (Observer Pattern) ────────────────────────────────
    public event Action<ICharacterUnit>      OnTurnStarted;
    public event Action<ICharacterUnit, AttackDefinition, IReadOnlyList<ICharacterUnit>>
                                             OnActionExecuted;
    public event Action<bool>               OnBattleEnded;  // true = player wins

    // ── Internal State Pattern ───────────────────────────────────
    private BattlePhase _currentPhase = BattlePhase.Idle;
    private Queue<ICharacterUnit> _turnQueue;
    private ICharacterUnit _activeUnit;

    private enum BattlePhase { Idle, PlayerTurn, EnemyTurn, Victory, Defeat }

    // ── Cached team lists ────────────────────────────────────────
    private List<ICharacterUnit> _playerTeam;
    private List<ICharacterUnit> _enemyTeam;

    // ── Unity lifecycle ──────────────────────────────────────────

    private void Start() => StartBattle();

    // ── Public API ───────────────────────────────────────────────

    /// <summary>Starts a fresh battle.</summary>
    public void StartBattle()
    {
        _playerTeam = _playerUnits.Cast<ICharacterUnit>().ToList();
        _enemyTeam  = _enemyUnits.Cast<ICharacterUnit>().ToList();

        Debug.Log("=== BATTLE START ===");
        Debug.Log($"Player team: {string.Join(", ", _playerTeam.Select(u => u.Name))}");
        Debug.Log($"Enemy team:  {string.Join(", ", _enemyTeam.Select(u => u.Name))}");

        _currentPhase = BattlePhase.Idle;
        StartCoroutine(RunBattle());
    }

    /// <summary>
    /// Called by player UI to submit an action for the active turn.
    /// </summary>
    public void SubmitPlayerAction(AttackDefinition attack, List<ICharacterUnit> targets)
    {
        if (_currentPhase != BattlePhase.PlayerTurn) return;
        StartCoroutine(ExecuteAction(_activeUnit, attack, targets));
    }

    // ── Battle Loop ──────────────────────────────────────────────

    private IEnumerator RunBattle()
    {
        while (!IsBattleOver())
        {
            _turnQueue = TurnOrderSystem.BuildTurnQueue(
                _playerTeam.Concat(_enemyTeam));

            while (_turnQueue.Count > 0 && !IsBattleOver())
            {
                _activeUnit = _turnQueue.Dequeue();
                if (!_activeUnit.IsAlive) continue;

                _activeUnit.Stats.TickStatusEffects();

                if (_activeUnit.IsPlayerControlled)
                    yield return StartCoroutine(HandlePlayerTurn(_activeUnit));
                else
                    yield return StartCoroutine(HandleEnemyTurn(_activeUnit));
            }
        }

        bool playerWon = _enemyTeam.All(e => !e.IsAlive);
        _currentPhase = playerWon ? BattlePhase.Victory : BattlePhase.Defeat;
        Debug.Log(playerWon ? "=== VICTORY ===" : "=== DEFEAT ===");
        OnBattleEnded?.Invoke(playerWon);
    }

    private IEnumerator HandlePlayerTurn(ICharacterUnit unit)
    {
        _currentPhase = BattlePhase.PlayerTurn;
        OnTurnStarted?.Invoke(unit);
        Debug.Log($"[{unit.Name}] awaiting player input...");

        // Yield until player submits an action via SubmitPlayerAction()
        yield return new WaitUntil(() => _currentPhase != BattlePhase.PlayerTurn);
    }

    private IEnumerator HandleEnemyTurn(ICharacterUnit unit)
    {
        _currentPhase = BattlePhase.EnemyTurn;
        OnTurnStarted?.Invoke(unit);

        yield return new WaitForSeconds(_actionDelaySeconds);

        // Simple AI: pick a random available attack, target a random player
        var attacks     = unit.GetAvailableAttacks();
        var attack      = attacks[UnityEngine.Random.Range(0, attacks.Count)];
        var livingPlayers = _playerTeam.Where(p => p.IsAlive).ToList();

        if (livingPlayers.Count == 0) yield break;

        List<ICharacterUnit> targets = ResolveTargets(attack, unit, livingPlayers, _enemyTeam);
        yield return StartCoroutine(ExecuteAction(unit, attack, targets));
    }

    private IEnumerator ExecuteAction(
        ICharacterUnit attacker,
        AttackDefinition attack,
        List<ICharacterUnit> targets)
    {
        bool hit = attack.Execute(attacker, targets);
        OnActionExecuted?.Invoke(attacker, attack, targets);

        yield return new WaitForSeconds(_actionDelaySeconds);

        // Return control: if it was a player turn, advance phase
        if (_currentPhase == BattlePhase.PlayerTurn)
            _currentPhase = BattlePhase.Idle;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool IsBattleOver()
        => _playerTeam.All(p => !p.IsAlive) || _enemyTeam.All(e => !e.IsAlive);

    /// <summary>
    /// Resolves the target list based on attack target type and teams.
    /// </summary>
    private List<ICharacterUnit> ResolveTargets(
        AttackDefinition attack,
        ICharacterUnit attacker,
        List<ICharacterUnit> playerTeam,
        List<ICharacterUnit> enemyTeam)
    {
        bool attackerIsPlayer = attacker.IsPlayerControlled;

        // For the active unit: enemies = opposite team, allies = same team
        var enemies = attackerIsPlayer
            ? enemyTeam.Where(e => e.IsAlive).ToList()
            : playerTeam.Where(p => p.IsAlive).ToList();

        var allies = attackerIsPlayer
            ? playerTeam.Where(p => p.IsAlive).ToList()
            : enemyTeam.Where(e => e.IsAlive).ToList();

        // Note: for player-initiated actions, UI passes targets directly.
        // This overload is used by AI.
        return new List<ICharacterUnit>
        {
            enemies[UnityEngine.Random.Range(0, enemies.Count)]
        };
    }
}