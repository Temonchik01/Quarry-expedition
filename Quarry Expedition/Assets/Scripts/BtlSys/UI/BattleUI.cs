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

        private Combatant currentHero;
        private List<CombatantUI> heroUIs = new List<CombatantUI>();

        private void Start()
        {
            BattleManager.Instance.onHeroTurnStarted += ShowActionPanel;
            BattleManager.Instance.onRoundExecutionStarted += HideAllPanels;
            BattleManager.Instance.onBattleEnded += ShowEndBattle;

            InitializeHeroInfo();
            HideAllPanels();

            attackButton.onClick.AddListener(() => OnMainActionClicked("Attack"));
            specialsButton.onClick.AddListener(() => ShowSubMenu("Specials"));
            itemsButton.onClick.AddListener(() => ShowSubMenu("Items"));
            reloadButton.onClick.AddListener(() => SelectAction(new BattleAction(currentHero, null, null) { isReload = true }));
            defendButton.onClick.AddListener(() => SelectAction(new BattleAction(currentHero, null, null) { isDefend = true }));
            backButton.onClick.AddListener(HideSubMenu);
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
            currentHero = hero;
            actionPanel.SetActive(true);
            subActionPanel.SetActive(false);

            // Position Action Panel to the right of the hero
            Vector3 screenPos = Camera.main.WorldToScreenPoint(hero.transform.position);
            RectTransform panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.position = screenPos + new Vector3(150, 0, 0);

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
        }

        private void OnMainActionClicked(string actionType)
        {
            if (actionType == "Attack")
            {
                var target = BattleManager.Instance.enemyTeam.FirstOrDefault(e => e.currentHP > 0);
                if (target != null)
                {
                    SelectAction(new BattleAction(currentHero, new List<Combatant> { target }, null));
                    SelectionCursorUI.Instance.SetTarget(target.transform);
                }
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

            if (category == "Specials")
            {
                foreach (var spec in currentHero.data.specials)
                {
                    string label = $"{spec.attackName} (Acc: {spec.accuracy}, Pow: {spec.power})";
                    CreateSubButton(label, () => SelectAction(new BattleAction(currentHero, GetTargets(spec), spec)));
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
        }
    }
}
