using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
        public SpecialAttackDetailsUI specialDetails;
        [SerializeField] private UnityEngine.UI.ScrollRect specialScrollRect;
        [SerializeField] private SpecialMenuCursorUI specialMenuCursor;
        [SerializeField, Min(0.3f)] private float actionCanvasDistance = 10f;

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
        private BattleManager subscribedBattle;
        private Combatant currentHero;
        private AttackData pendingAttack;
        private int pendingAttackUpgradeLevel;
        private bool isTargeting;
        private int currentTargetIndex;

        private void OnEnable()
        {
            SubscribeToBattle();
        }

        private void OnDisable()
        {
            if (subscribedBattle == null) return;
            subscribedBattle.onHeroTurnStarted -= ShowActionPanel;
            subscribedBattle.onRoundExecutionStarted -= HideAllPanels;
            subscribedBattle.onActionStarted -= ShowActionAnnouncement;
            subscribedBattle.onActionFinished -= HideActionAnnouncement;
            subscribedBattle.onBattleEnded -= ShowEndBattle;
            subscribedBattle = null;
        }

        private void SubscribeToBattle()
        {
            battle = BattleManager.Instance;
            if (battle == null || subscribedBattle == battle) return;
            subscribedBattle = battle;
            battle.onHeroTurnStarted += ShowActionPanel;
            battle.onRoundExecutionStarted += HideAllPanels;
            battle.onActionStarted += ShowActionAnnouncement;
            battle.onActionFinished += HideActionAnnouncement;
            battle.onBattleEnded += ShowEndBattle;
        }

        private void Start()
        {
            SubscribeToBattle();
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

            AddButtonTextStyle(attackButton);
            AddButtonTextStyle(specialsButton);
            AddButtonTextStyle(itemsButton);
            AddButtonTextStyle(reloadButton);
            AddButtonTextStyle(defendButton);
            AddButtonTextStyle(backButton);
            AddButtonTextStyle(continueButton);
            AddButtonTextStyle(restartButton);

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
            else if (subActionPanel != null && subActionPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) HideSubMenu();
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
            Camera camera = Camera.main;
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && camera != null)
                canvas.transform.position = camera.ViewportToWorldPoint(
                    new Vector3(0.5f, 0.5f, actionCanvasDistance));

            foreach (CombatantUI ui in heroUIs) ui.SetHighlight(ui.Target == hero);

            AttackData basicAttack = hero != null && hero.data != null ? hero.data.basicAttack : null;
            TextMeshProUGUI attackLabel = attackButton != null
                ? attackButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            if (attackLabel != null)
                attackLabel.text = "Attack" + (basicAttack != null && basicAttack.MaximumUpgradeLevel > 0
                    ? " <color=#25D65A>+</color>"
                    : string.Empty);
        }

        private void ShowActionAnnouncement(BattleAction action)
        {
            if (TooltipUI.Instance != null) TooltipUI.Instance.ShowAction(action);
        }

        private void HideActionAnnouncement()
        {
            if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
        }

        private void HideAllPanels()
        {
            if (actionPanel != null) actionPanel.SetActive(false);
            if (subActionPanel != null) subActionPanel.SetActive(false);
            foreach (CombatantUI ui in heroUIs) ui.SetHighlight(false);
            if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(null);
            if (specialMenuCursor != null) specialMenuCursor.Hide();
            isTargeting = false;
            pendingAttack = null;
            pendingAttackUpgradeLevel = 0;
            if (specialDetails != null) specialDetails.Clear();
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
            int upgradeLevel = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.GetEquippedUpgradeLevel(currentHero.data, attack) : 0;
            SelectOrTarget(attack, upgradeLevel);
        }

        private void ShowSpecials()
        {
            if (currentHero == null || currentHero.data == null || subButtonContainer == null || subButtonPrefab == null) return;
            actionPanel.SetActive(false);
            subActionPanel.SetActive(true);
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(null);
            if (specialMenuCursor != null) specialMenuCursor.Hide();
            if (specialDetails != null) specialDetails.Clear();

            foreach (Transform child in subButtonContainer)
            {
                if (backButton == null || child.gameObject != backButton.gameObject)
                {
                    child.gameObject.SetActive(false); // Remove it from layout before deferred destruction.
                    Destroy(child.gameObject);
                }
            }

            ProgressionManager progression = ProgressionManager.Instance;
            if (progression != null)
            {
                foreach (EquippedSkillState skill in progression.GetEquippedSpecials(currentHero.data))
                    CreateSpecialSlot(skill.attack, skill.upgradeLevel);
            }
            else if (currentHero.data.specials != null)
            {
                foreach (AttackData attack in currentHero.data.specials)
                    if (attack != null && attack != currentHero.data.basicAttack) CreateSpecialSlot(attack, 0);
            }

            if (specialScrollRect != null)
            {
                specialScrollRect.StopMovement();
                Canvas.ForceUpdateCanvases();
                specialScrollRect.verticalNormalizedPosition = 1f;
            }
            if (specialDetails != null && backButton != null)
            {
                SpecialAttackButtonUI backFocus = backButton.GetComponent<SpecialAttackButtonUI>();
                if (backFocus == null) backFocus = backButton.gameObject.AddComponent<SpecialAttackButtonUI>();
                backFocus.Bind(null, 0, specialDetails.Show, specialScrollRect, specialMenuCursor);
            }
            if (EventSystem.current != null && backButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
            }
        }

        private GameObject CreateSpecialSlot(AttackData attack, int upgradeLevel)
        {
            GameObject buttonObject = Instantiate(subButtonPrefab, subButtonContainer);
            buttonObject.name = attack != null ? $"{attack.attackName}Button" : "EmptySpecialSlot";
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect != null) rect.localScale = Vector3.one;
            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = attack == null ? string.Empty : attack.attackName +
                    (attack.MaximumUpgradeLevel > 0 ? " <color=#25D65A>+</color>" : string.Empty);
            }

            UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                AddButtonTextStyle(button);
                button.interactable = attack != null && currentHero.CanUse(attack, upgradeLevel);
                if (attack != null) button.onClick.AddListener(() => SelectOrTarget(attack, upgradeLevel));
            }
            if (specialDetails != null)
            {
                SpecialAttackButtonUI focus = buttonObject.AddComponent<SpecialAttackButtonUI>();
                focus.Bind(attack, upgradeLevel, specialDetails.Show, specialScrollRect, specialMenuCursor);
            }
            return buttonObject;
        }

        private static void AddButtonTextStyle(UnityEngine.UI.Button button)
        {
            if (button != null && button.GetComponent<ButtonInverseTextUI>() == null)
                button.gameObject.AddComponent<ButtonInverseTextUI>();
        }

        private void HideSubMenu()
        {
            subActionPanel.SetActive(false);
            if (specialDetails != null) specialDetails.Clear();
            if (specialMenuCursor != null) specialMenuCursor.Hide();
            if (currentHero != null && battle.currentState == BattleState.PlayerInput)
                actionPanel.SetActive(true);
            if (EventSystem.current != null && specialsButton != null)
                EventSystem.current.SetSelectedGameObject(specialsButton.gameObject);
        }

        private void SelectOrTarget(AttackData attack, int upgradeLevel = 0)
        {
            if (currentHero == null || !currentHero.CanUse(attack, upgradeLevel)) return;
            AttackRuntimeStats stats = attack.GetRuntimeStats(upgradeLevel);
            if (stats.TargetType != TargetType.SingleTarget)
            {
                SelectAction(BattleAction.UseAttack(currentHero, attack, stats.TargetType == TargetType.Self ? currentHero : null, upgradeLevel));
                return;
            }

            potentialTargets.Clear();
            IReadOnlyList<Combatant> party = stats.TargetAllies ? battle.playerTeam : battle.enemyTeam;
            foreach (Combatant target in party)
                if (target != null && target.IsAlive) potentialTargets.Add(target);
            if (potentialTargets.Count == 0) return;

            pendingAttack = attack;
            pendingAttackUpgradeLevel = upgradeLevel;
            currentTargetIndex = 0;
            isTargeting = true;
            actionPanel.SetActive(false);
            subActionPanel.SetActive(false);
            if (specialMenuCursor != null) specialMenuCursor.Hide();
            if (SelectionCursorUI.Instance != null)
                SelectionCursorUI.Instance.SetTarget(potentialTargets[0].transform, false, potentialTargets[0].isPlayer != currentHero.isPlayer);
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
                        if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(hovered.transform, false, hovered.isPlayer != currentHero.isPlayer);
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
                SelectionCursorUI.Instance.SetTarget(potentialTargets[currentTargetIndex].transform, false, potentialTargets[currentTargetIndex].isPlayer != currentHero.isPlayer);
        }

        private void ConfirmTarget()
        {
            Combatant target = potentialTargets[currentTargetIndex];
            if (target == null || !target.IsAlive) return;
            SelectAction(BattleAction.UseAttack(currentHero, pendingAttack, target, pendingAttackUpgradeLevel));
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            pendingAttack = null;
            pendingAttackUpgradeLevel = 0;
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
