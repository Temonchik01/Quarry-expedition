using System.Collections.Generic;
using System.Linq;
using JRPG.Core;
using JRPG.Core.Attacks;
using JRPG.Core.Interfaces;
using JRPG.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JRPG.Battle
{
    /// <summary>
    /// Manages all battle UI: HP bars, ammo display, attack selection menu,
    /// and turn indicator.  Subscribes to BattleEvents — never calls BattleManager.
    ///
    /// Inspector Setup:
    ///   - HPBarContainer: parent that holds one HPBarEntry prefab per hero
    ///   - AttackButtonContainer: parent for dynamically created attack buttons
    ///   - AttackButtonPrefab: a Button with a TextMeshProUGUI child
    ///   - TurnIndicatorText: shows whose turn it is
    ///   - ResultPanel + ResultText: shown when battle ends
    /// </summary>
    public sealed class BattleUIManager : MonoBehaviour
    {
        [Header("HP / Status Panel")]
        [SerializeField] private Transform   _hpBarContainer;
        [SerializeField] private GameObject  _hpBarEntryPrefab;   // Slider + name label

        [Header("Attack Menu")]
        [SerializeField] private Transform   _attackButtonContainer;
        [SerializeField] private GameObject  _attackButtonPrefab;

        [Header("Turn Indicator")]
        [SerializeField] private TextMeshProUGUI _turnIndicatorText;

        [Header("Result")]
        [SerializeField] private GameObject      _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultText;

        // ── Runtime state ──────────────────────────────────────────────────────
        private ICharacter             _currentActor;
        private BattleManager          _battleManager;
        private List<ICharacter>       _playerTeam = new();
        private Dictionary<ICharacter, Slider> _hpSliders = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            _battleManager = FindObjectOfType<BattleManager>();
            _resultPanel?.SetActive(false);
            SetAttackMenuVisible(false);
        }

        private void OnEnable()
        {
            BattleEvents.OnBattleStarted    += HandleBattleStarted;
            BattleEvents.OnTurnStarted      += HandleTurnStarted;
            BattleEvents.OnDamageDealt      += HandleDamageDealt;
            BattleEvents.OnBattleEnded      += HandleBattleEnded;
        }

        private void OnDisable()
        {
            BattleEvents.OnBattleStarted    -= HandleBattleStarted;
            BattleEvents.OnTurnStarted      -= HandleTurnStarted;
            BattleEvents.OnDamageDealt      -= HandleDamageDealt;
            BattleEvents.OnBattleEnded      -= HandleBattleEnded;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleBattleStarted()
        {
            // Build HP bars for all heroes — enemies shown separately if desired
            foreach (CharacterView view in FindObjectsOfType<CharacterView>())
            {
                if (!view.IsPlayerCharacter) continue;
                // (Assumes Bind has already been called — safe since BattleManager.Awake runs first)
            }
        }

        private void HandleTurnStarted(ICharacter actor)
        {
            _currentActor = actor;
            bool isPlayerTurn = _playerTeam.Contains(actor);
            _turnIndicatorText.text = $"{actor.CharacterName}'s turn";

            SetAttackMenuVisible(isPlayerTurn);
            if (isPlayerTurn) BuildAttackMenu(actor);
        }

        private void HandleDamageDealt(ICharacter target, int amount)
        {
            if (_hpSliders.TryGetValue(target, out Slider bar))
                bar.value = (float)target.CurrentHp / target.MaxHp;
        }

        private void HandleBattleEnded(bool playerWon)
        {
            _resultPanel?.SetActive(true);
            if (_resultText != null)
                _resultText.text = playerWon ? "Victory!" : "Defeat...";
            SetAttackMenuVisible(false);
        }

        // ── Attack menu construction ──────────────────────────────────────────

        private void BuildAttackMenu(ICharacter actor)
        {
            // Clear previous buttons
            foreach (Transform child in _attackButtonContainer) Destroy(child.gameObject);

            foreach (AttackDataSO attackData in actor.Attacks)
            {
                IAttack attack = AttackFactory.Create(attackData);
                bool usable    = attack.CanUse(actor);

                GameObject btn = Instantiate(_attackButtonPrefab, _attackButtonContainer);
                var label      = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"{attackData.AttackName} [{attackData.AmmoCost}]";

                var button = btn.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = usable;
                    // Capture for lambda
                    AttackDataSO captured = attackData;
                    button.onClick.AddListener(() => OnAttackSelected(actor, AttackFactory.Create(captured)));
                }
            }
        }

        private void OnAttackSelected(ICharacter actor, IAttack attack)
        {
            // For single-target: auto-pick first living enemy for now.
            // Full targeting UI would be a separate component (ISP).
            var enemies = FindObjectsOfType<CharacterView>()
                .Where(v => !v.IsPlayerCharacter)
                .Select(v => (ICharacter)null)   // placeholder — bind stores runtime
                .ToList();

            // In a real scene, you'd store the runtime ref on CharacterView.
            // Simplified: submit with empty targets and let BattleManager resolve.
            SetAttackMenuVisible(false);
            // _battleManager.SubmitPlayerAction_Internal(actor, attack, targets);
            // (Called through BattleStateMachine in production)
        }

        private void SetAttackMenuVisible(bool visible)
        {
            if (_attackButtonContainer != null)
                _attackButtonContainer.gameObject.SetActive(visible);
        }
    }
}