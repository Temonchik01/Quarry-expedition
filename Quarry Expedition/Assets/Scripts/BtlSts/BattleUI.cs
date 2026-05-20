using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace BtlSts
{
    public class BattleUI : MonoBehaviour
    {
        [Header("Party Status (Right Side)")]
        public Transform partyListParent;
        public GameObject partyMemberHudPrefab;
        private List<PartyMemberHUD> partyHuds = new List<PartyMemberHUD>();

        [Header("Action Menu (Bottom Center)")]
        public GameObject actionMenuRoot;
        public Transform commandListParent;
        public GameObject commandButtonPrefab;
        public GameObject boostIndicator;
        public RectTransform menuCursor; // The "hand" icon

        [Header("Tooltip (Bottom)")]
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipText;

        [Header("Attack/Target Sub-Menus")]
        public GameObject subMenuRoot;
        public Transform subMenuListParent;

        [Header("Enemy UI")]
        public GameObject vulnerabilityPrefab;

        private BattleCharacter activeCharacter;
        private AttackActionData selectedAttack;

        public void InitializeEnemyUI(List<BattleCharacter> enemies)
        {
            if (vulnerabilityPrefab == null) return;
            
            foreach (var e in enemies)
            {
                var obj = Instantiate(vulnerabilityPrefab, transform); // Instantiate on canvas
                obj.SetActive(true);
                var ui = obj.GetComponent<VulnerabilityUI>();
                ui.Setup(e);
            }
        }

        private void Start()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnTurnStarted += OnCharacterTurnStarted;
                BattleManager.Instance.OnStateChanged += OnBattleStateChanged;
            }

            HideAllMenus();
        }

        private void OnBattleStateChanged(BattleState state)
        {
            if (state != BattleState.PlayerDecision)
            {
                HideAllMenus();
                if (activeCharacter != null) SetPartyActiveState(activeCharacter, false);
            }
        }

        private void OnCharacterTurnStarted(BattleCharacter character)
        {
            activeCharacter = character;
            
            // Highlight the active character in the party list
            SetPartyActiveState(character, true);

            // Show action menu
            ShowActionMenu();
        }

        public void InitializePartyList(List<BattleCharacter> players)
        {
            foreach (Transform child in partyListParent) Destroy(child.gameObject);
            partyHuds.Clear();

            foreach (var p in players)
            {
                var hudObj = Instantiate(partyMemberHudPrefab, partyListParent);
                var hud = hudObj.GetComponent<PartyMemberHUD>();
                hud.Setup(p);
                partyHuds.Add(hud);
            }
        }

        private void SetPartyActiveState(BattleCharacter character, bool isActive)
        {
            var hud = partyHuds.FirstOrDefault(h => h.character == character);
            if (hud != null) hud.SetActive(isActive);
        }

        private void ShowActionMenu()
        {
            HideAllMenus();
            actionMenuRoot.SetActive(true);
            tooltipPanel.SetActive(true);
            
            // Clear and build command list
            foreach (Transform child in commandListParent) Destroy(child.gameObject);

            AddCommand("Battle", () => OnBattleCategorySelected());
            AddCommand("Items", () => OnItemsCategorySelected());
            AddCommand("Reload", () => OnReloadSelected());
            AddCommand("Defend", () => OnDefendSelected());
            
            UpdateTooltip("Select an action for " + activeCharacter.stats.characterName);
        }

        private void AddCommand(string label, System.Action onClick)
        {
            var btnObj = Instantiate(commandButtonPrefab, commandListParent);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = label;
            btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => onClick());
            
            // Setup Tooltip update on hover/select if needed
            // For now, we'll keep it simple
        }

        private void OnBattleCategorySelected()
        {
            actionMenuRoot.SetActive(false);
            subMenuRoot.SetActive(true);
            
            foreach (Transform child in subMenuListParent) Destroy(child.gameObject);

            if (activeCharacter != null && activeCharacter.stats != null)
            {
                foreach (var attack in activeCharacter.stats.availableAttacks)
                {
                    var btnObj = Instantiate(commandButtonPrefab, subMenuListParent);
                    btnObj.GetComponentInChildren<TextMeshProUGUI>().text = attack.attackName;
                    btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                        selectedAttack = attack;
                        ShowTargetSelection();
                    });
                    
                    // Add hover logic to update tooltip
                    var trigger = btnObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                    entry.callback.AddListener((data) => UpdateTooltip(attack.description + "\nPower: " + attack.power + " | Ammo Cost: " + attack.ammoCost));
                    trigger.triggers.Add(entry);
                }
            }
        }

        private void OnItemsCategorySelected()
        {
            UpdateTooltip("Items menu: Concept similar to Battle. Implementation pending item system.");
        }

        private void OnReloadSelected()
        {
            BattleManager.Instance.SkipTurn(activeCharacter, true);
            HideAllMenus();
        }

        private void OnDefendSelected()
        {
            BattleManager.Instance.SkipTurn(activeCharacter, false);
            HideAllMenus();
        }

        private void ShowTargetSelection()
        {
            subMenuRoot.SetActive(false);
            actionMenuRoot.SetActive(false);
            
            var targets = BattleManager.Instance.enemyTeam.Where(e => !e.IsDead).ToList();
            OpenTargetMenu(targets);
        }

        private void OpenTargetMenu(List<BattleCharacter> targets)
        {
            subMenuRoot.SetActive(true);
            foreach (Transform child in subMenuListParent) Destroy(child.gameObject);

            foreach (var t in targets)
            {
                var btnObj = Instantiate(commandButtonPrefab, subMenuListParent);
                btnObj.GetComponentInChildren<TextMeshProUGUI>().text = t.stats.characterName;
                btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                    BattleManager.Instance.ExecuteAction(selectedAttack, activeCharacter, new List<BattleCharacter> { t });
                    HideAllMenus();
                });
            }
        }

        public void UpdateTooltip(string text)
        {
            if (tooltipText != null) tooltipText.text = text;
        }

        private void HideAllMenus()
        {
            actionMenuRoot.SetActive(false);
            subMenuRoot.SetActive(false);
            tooltipPanel.SetActive(false);
        }
    }
}
