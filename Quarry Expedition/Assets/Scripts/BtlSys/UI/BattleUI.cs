using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

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
        public Button attackButton;
        public Button specialsButton;
        public Button itemsButton;
        public Button reloadButton;
        public Button defendButton;
        public Button backButton;

        [Header("Sub-Panel Buttons Container")]
        public Transform subButtonContainer;
        public GameObject subButtonPrefab;

        [Header("End Battle UI")]
        public GameObject endBattlePanel;
        public TextMeshProUGUI endBattleText;
        public Button continueButton;
        public Button restartButton;

        private Combatant currentHero;
        private List<CombatantUI> heroUIs = new List<CombatantUI>();

        private bool isTargeting = false;
        private AttackData pendingAttack = null;
        private List<Combatant> potentialTargets = new List<Combatant>();
        private int currentTargetIndex = 0;

        private void OnEnable()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.onHeroTurnStarted += ShowActionPanel;
                BattleManager.Instance.onRoundExecutionStarted += HideAllPanels;
                BattleManager.Instance.onBattleEnded += ShowEndBattle;
            }
        }

        private void OnDisable()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.onHeroTurnStarted -= ShowActionPanel;
                BattleManager.Instance.onRoundExecutionStarted -= HideAllPanels;
                BattleManager.Instance.onBattleEnded -= ShowEndBattle;
            }
        }

        private void Start()
        {
            InitializeHeroInfo();
            
            subActionPanel.SetActive(false);
            foreach (var ui in heroUIs) ui.SetHighlight(false);

            attackButton.onClick.AddListener(() => OnMainActionClicked("Attack"));
            specialsButton.onClick.AddListener(() => ShowSubMenu("Specials"));
            itemsButton.onClick.AddListener(() => ShowSubMenu("Items"));
            reloadButton.onClick.AddListener(() => {
                if (currentHero != null) SelectAction(new BattleAction(currentHero, null, null) { isReload = true });
            });
            defendButton.onClick.AddListener(() => {
                if (currentHero != null) SelectAction(new BattleAction(currentHero, null, null) { isDefend = true });
            });
            backButton.onClick.AddListener(HideSubMenu);

            if (continueButton != null) continueButton.onClick.AddListener(ReturnToMap);
            if (restartButton != null) restartButton.onClick.AddListener(RestartRun);
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
                // Fallback if Map scene wasn't additive
                UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
            }
        }

        private void RestartRun()
        {
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.ResetProgression();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Map");
        }

        private void InitializeHeroInfo()
        {
            foreach (var hero in BattleManager.Instance.playerTeam)
            {
                var ui = Instantiate(heroInfoPrefab, heroInfoContainer);
                ui.Bind(hero);
                heroUIs.Add(ui);
            }
        }

        private void ShowActionPanel(Combatant hero)
        {
            Debug.Log($"[BattleUI] ShowActionPanel called for {hero.name}");
            currentHero = hero;
            
            if (actionPanel == null)
            {
                Debug.LogError("[BattleUI] ActionPanel reference is MISSING!");
                return;
            }

            actionPanel.SetActive(true);
            subActionPanel.SetActive(false);

            // Position Action Panel in world space next to the hero
            // Move it further to the right (3.5f) to avoid the screen-space HeroesInfo HUD
            // and clearly forward (-1.0f) to prevent any clipping with world sprites
            Canvas canvas = actionPanel.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.transform.position = hero.transform.position + new Vector3(3.5f, 1.5f, -1.0f);
                Debug.Log($"[BattleUI] World Space ActionPanel positioned at {canvas.transform.position}");
            }
            else
            {
                Debug.LogWarning("[BattleUI] ActionPanel is not in a World Space Canvas! Position might be wrong.");
            }

            // Highlight the active hero UI
            foreach (var ui in heroUIs)
            {
                ui.SetHighlight(ui.Target == hero);
            }
        }

        private void HideAllPanels()
        {
            actionPanel.SetActive(false);
            subActionPanel.SetActive(false);
            foreach (var ui in heroUIs) ui.SetHighlight(false);
            TooltipUI.Instance.Hide();
            SelectionCursorUI.Instance.SetTarget(null);
            isTargeting = false;
        }

        private void Update()
        {
            if (isTargeting)
            {
                HandleTargeting();
            }
        }

        private void HandleTargeting()
        {
            // Mouse hover highlighting
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null)
            {
                Combatant hovered = hit.collider.GetComponentInParent<Combatant>();
                if (hovered != null && potentialTargets.Contains(hovered))
                {
                    SelectionCursorUI.Instance.SetTarget(hovered.transform);
                    
                    if (Input.GetMouseButtonDown(0))
                    {
                        ConfirmTarget(hovered);
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
            }

            // Keyboard cycling
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                CycleTarget(1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                CycleTarget(-1);
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmTarget(potentialTargets[currentTargetIndex]);
            }
        }

        private void CycleTarget(int direction)
        {
            currentTargetIndex = (currentTargetIndex + direction + potentialTargets.Count) % potentialTargets.Count;
            SelectionCursorUI.Instance.SetTarget(potentialTargets[currentTargetIndex].transform);
        }

        private void ConfirmTarget(Combatant target)
        {
            SelectAction(new BattleAction(currentHero, new List<Combatant> { target }, pendingAttack));
            isTargeting = false;
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            SelectionCursorUI.Instance.SetTarget(null);
            actionPanel.SetActive(true);
        }

        private void EnterTargetingMode(AttackData attack)
        {
            actionPanel.SetActive(false);
            subActionPanel.SetActive(false);
            isTargeting = true;
            pendingAttack = attack;
            
            // For single target moves, target enemies
            potentialTargets = BattleManager.Instance.enemyTeam.Where(e => e.currentHP > 0).ToList();
            if (potentialTargets.Count > 0)
            {
                currentTargetIndex = 0;
                SelectionCursorUI.Instance.SetTarget(potentialTargets[currentTargetIndex].transform);
            }
        }

        private void OnMainActionClicked(string actionType)
        {
            if (currentHero == null) return;

            if (actionType == "Attack")
            {
                EnterTargetingMode(null); // Basic attack
            }
        }

        private void ShowSubMenu(string category)
        {
            subActionPanel.SetActive(true);
            // Clear old buttons (except Back button if it's in the same container, but I'll manage)
            foreach (Transform child in subButtonContainer)
            {
                if (child.gameObject != backButton.gameObject) Destroy(child.gameObject);
            }

            if (category == "Specials" && currentHero != null && currentHero.data != null && currentHero.data.specials != null)
            {
                foreach (var spec in currentHero.data.specials)
                {
                    AttackData localSpec = spec;
                    string label = $"{localSpec.attackName} (Acc: {localSpec.accuracy}, Pow: {localSpec.power})";
                    CreateSubButton(label, () => {
                        if (localSpec.targetType == TargetType.SingleTarget)
                        {
                            EnterTargetingMode(localSpec);
                        }
                        else
                        {
                            SelectAction(new BattleAction(currentHero, GetTargets(localSpec), localSpec));
                        }
                    });
                }
            }
            else if (category == "Items")
            {
                // Placeholder items
                CreateSubButton("Grenade x10", () => { });
                CreateSubButton("Bandage x5", () => { });
            }
        }

        private void HideSubMenu()
        {
            subActionPanel.SetActive(false);
        }

        private void SelectAction(BattleAction action)
        {
            HideAllPanels();
            BattleManager.Instance.RegisterHeroAction(action);
        }

        private void CreateSubButton(string label, System.Action onClick)
        {
            var btnObj = Instantiate(subButtonPrefab, subButtonContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
            btnObj.GetComponent<Button>().onClick.AddListener(() => onClick());
            
            var hover = btnObj.AddComponent<UIHoverHandler>();
            hover.tooltipContent = label;
        }

        private List<Combatant> GetTargets(AttackData attack)
        {
            if (attack.targetType == TargetType.EntireParty)
            {
                return BattleManager.Instance.enemyTeam.Where(e => e.currentHP > 0).ToList();
            }
            // For single target, just pick first healthy enemy for now
            var target = BattleManager.Instance.enemyTeam.FirstOrDefault(e => e.currentHP > 0);
            return target != null ? new List<Combatant> { target } : new List<Combatant>();
        }

        private void ShowEndBattle(string message)
        {
            HideAllPanels();
            endBattlePanel.SetActive(true);
            endBattleText.text = message;

            // Center the end panel in screen space (assuming it's in an Overlay Canvas)
            RectTransform rt = endBattlePanel.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;

            bool won = message.Contains("Won");
            if (continueButton != null) continueButton.gameObject.SetActive(won);
            if (restartButton != null) restartButton.gameObject.SetActive(!won);
        }
    }
}
