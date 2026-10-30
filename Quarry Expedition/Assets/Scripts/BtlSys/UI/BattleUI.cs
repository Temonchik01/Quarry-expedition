using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BtlSys.UI
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject actionPanel;
        public GameObject subActionPanel;
        public Transform heroInfoContainer;
        public CombatantUI heroInfoPrefab;

        [Header("Buttons")]
        public UnityEngine.UI.Button attackButton;
        public UnityEngine.UI.Button specialsButton;
        public UnityEngine.UI.Button itemsButton;
        public UnityEngine.UI.Button reloadButton;
        public UnityEngine.UI.Button defendButton;
        public UnityEngine.UI.Button backButton;

        [Header("Sub-Panel Buttons Container")]
        public Transform subButtonContainer;
        public GameObject subButtonPrefab;

        [Header("End Battle UI")]
        public GameObject endBattlePanel;
        public TextMeshProUGUI endBattleText;
        public UnityEngine.UI.Button continueButton;
        public UnityEngine.UI.Button restartButton;
        [SerializeField] private string victoryText = "You Won";
        [SerializeField] private string defeatText = "You Lost";

        private readonly List<CombatantUI> heroUIs = new List<CombatantUI>();
        private readonly List<Combatant> potentialTargets = new List<Combatant>();
        private BattleManager battle;
        private Combatant currentHero;
        private AttackData pendingAttack;
        private bool isTargeting;
        private int currentTargetIndex;

        private void OnEnable()
        {
            battle = BattleManager.Instance;
            if (battle == null) return;
            battle.onHeroTurnStarted += ShowActionPanel;
            battle.onRoundExecutionStarted += HideAllPanels;
            battle.onBattleEnded += ShowEndBattle;
        }

        private void OnDisable()
        {
            if (battle == null) return;
            battle.onHeroTurnStarted -= ShowActionPanel;
            battle.onRoundExecutionStarted -= HideAllPanels;
            battle.onBattleEnded -= ShowEndBattle;
        }

        private void Start()
        {
            if (battle == null) battle = BattleManager.Instance;
            if (battle == null || actionPanel == null || subActionPanel == null || heroInfoContainer == null || heroInfoPrefab == null)
            {
                Debug.LogError("[BattleUI] Required battle UI references are missing.", this);
                enabled = false;
                return;
            }

            EnsureHeroInfo();

            subActionPanel.SetActive(false);
            if (endBattlePanel != null) endBattlePanel.SetActive(false);
            attackButton.onClick.AddListener(SelectBasicAttack);
            specialsButton.onClick.AddListener(ShowSpecials);
            reloadButton.onClick.AddListener(() => SelectAction(BattleAction.Reload(currentHero)));
            defendButton.onClick.AddListener(() => SelectAction(BattleAction.Defend(currentHero)));
            backButton.onClick.AddListener(HideSubMenu);
            if (itemsButton != null) itemsButton.interactable = false; // Item actions are not implemented yet.
            if (continueButton != null) continueButton.onClick.AddListener(ReturnToMap);
            if (restartButton != null) restartButton.onClick.AddListener(RestartRun);

            if (battle.currentState == BattleState.PlayerInput && battle.CurrentHero != null)
                ShowActionPanel(battle.CurrentHero);
            else if (battle.currentState == BattleState.Victory || battle.currentState == BattleState.Defeat)
                ShowEndBattle(battle.currentState == BattleState.Victory);
            else
                HideAllPanels();
        }

        private void Update()
        {
            if (isTargeting) HandleTargeting();
        }

        private void EnsureHeroInfo()
        {
            if (heroUIs.Count > 0 || battle == null || heroInfoPrefab == null || heroInfoContainer == null) return;
            foreach (Combatant hero in battle.playerTeam)
            {
                CombatantUI ui = Instantiate(heroInfoPrefab, heroInfoContainer);
                ui.Bind(hero);
                heroUIs.Add(ui);
            }
        }

        private void ShowActionPanel(Combatant hero)
        {
            EnsureHeroInfo();
            currentHero = hero;
            if (actionPanel == null) return;
            actionPanel.SetActive(true);
            subActionPanel.SetActive(false);
            isTargeting = false;

            Canvas canvas = actionPanel.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                canvas.transform.position = hero.transform.position + new Vector3(3.5f, 1.5f, -1f);

            foreach (CombatantUI ui in heroUIs) ui.SetHighlight(ui.Target == hero);
        }

        private void HideAllPanels()
        {
            if (actionPanel != null) actionPanel.SetActive(false);
            if (subActionPanel != null) subActionPanel.SetActive(false);
            foreach (CombatantUI ui in heroUIs) ui.SetHighlight(false);
            if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(null);
            isTargeting = false;
        }

        private void SelectBasicAttack()
        {
            if (currentHero == null || currentHero.data == null) return;
            AttackData attack = currentHero.data.basicAttack;
            if (attack == null)
            {
                Debug.LogError($"[BattleUI] {currentHero.name} has no basic attack.");
                return;
            }
            SelectOrTarget(attack);
        }

        private void ShowSpecials()
        {
            if (currentHero == null || currentHero.data == null || subButtonContainer == null || subButtonPrefab == null) return;
            subActionPanel.SetActive(true);
            foreach (Transform child in subButtonContainer)
                if (backButton == null || child.gameObject != backButton.gameObject) Destroy(child.gameObject);

            if (currentHero.data.specials == null) return;
            foreach (AttackData attack in currentHero.data.specials)
            {
                if (attack == null || attack == currentHero.data.basicAttack) continue;
                AttackData selected = attack;
                GameObject buttonObject = Instantiate(subButtonPrefab, subButtonContainer);
                TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"{selected.attackName} ({selected.ammoCost})";
                UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.interactable = currentHero.CanUse(selected);
                    button.onClick.AddListener(() => SelectOrTarget(selected));
                }
                UIHoverHandler hover = buttonObject.GetComponent<UIHoverHandler>();
                if (hover == null) hover = buttonObject.AddComponent<UIHoverHandler>();
                hover.tooltipContent = selected.attackName;
            }
        }

        private void HideSubMenu()
        {
            if (subActionPanel != null) subActionPanel.SetActive(false);
        }

        private void SelectOrTarget(AttackData attack)
        {
            if (currentHero == null || !currentHero.CanUse(attack)) return;
            if (attack.targetType != TargetType.SingleTarget)
            {
                SelectAction(BattleAction.UseAttack(currentHero, attack, attack.targetType == TargetType.Self ? currentHero : null));
                return;
            }

            potentialTargets.Clear();
            IReadOnlyList<Combatant> party = attack.targetAllies ? battle.playerTeam : battle.enemyTeam;
            foreach (Combatant target in party)
                if (target != null && target.IsAlive) potentialTargets.Add(target);
            if (potentialTargets.Count == 0) return;

            pendingAttack = attack;
            currentTargetIndex = 0;
            isTargeting = true;
            actionPanel.SetActive(false);
            subActionPanel.SetActive(false);
            if (SelectionCursorUI.Instance != null)
                SelectionCursorUI.Instance.SetTarget(potentialTargets[0].transform);
        }

        private void HandleTargeting()
        {
            if (potentialTargets.Count == 0)
            {
                CancelTargeting();
                return;
            }

            if (Camera.main != null)
            {
                RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (hit.collider != null)
                {
                    Combatant hovered = hit.collider.GetComponentInParent<Combatant>();
                    if (hovered != null && hovered.IsAlive && potentialTargets.Contains(hovered))
                    {
                        currentTargetIndex = potentialTargets.IndexOf(hovered);
                        if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(hovered.transform);
                        if (Input.GetMouseButtonDown(0))
                        {
                            ConfirmTarget();
                            return;
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                CancelTargeting();
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                CycleTarget(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                CycleTarget(-1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                ConfirmTarget();
        }

        private void CycleTarget(int direction)
        {
            currentTargetIndex = (currentTargetIndex + direction + potentialTargets.Count) % potentialTargets.Count;
            if (SelectionCursorUI.Instance != null)
                SelectionCursorUI.Instance.SetTarget(potentialTargets[currentTargetIndex].transform);
        }

        private void ConfirmTarget()
        {
            Combatant target = potentialTargets[currentTargetIndex];
            if (target == null || !target.IsAlive) return;
            SelectAction(BattleAction.UseAttack(currentHero, pendingAttack, target));
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            pendingAttack = null;
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(null);
            if (actionPanel != null) actionPanel.SetActive(true);
        }

        private void SelectAction(BattleAction action)
        {
            if (currentHero == null || battle == null) return;
            Combatant hero = currentHero;
            HideAllPanels();
            if (!battle.RegisterHeroAction(action))
            {
                Debug.LogWarning($"[BattleUI] Invalid action for {hero.name}.");
                ShowActionPanel(hero);
            }
        }

        private void ShowEndBattle(bool won)
        {
            HideAllPanels();
            if (endBattlePanel != null)
            {
                endBattlePanel.SetActive(true);
                RectTransform rect = endBattlePanel.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = Vector2.zero;
            }
            if (endBattleText != null) endBattleText.text = won ? victoryText : defeatText;
            if (continueButton != null) continueButton.gameObject.SetActive(won);
            if (restartButton != null) restartButton.gameObject.SetActive(!won);
        }

        private void ReturnToMap()
        {
            if (MapGenerator.Instance != null)
            {
                MapGenerator.Instance.ReactivateMap();
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(gameObject.scene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
            }
        }

        private void RestartRun()
        {
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.ResetProgression();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
        }
    }
}
