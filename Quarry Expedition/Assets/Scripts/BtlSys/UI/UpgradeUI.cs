using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BtlSys;

namespace BtlSys.UI
{
    public class UpgradeUI : MonoBehaviour
    {
        public static UpgradeUI Instance { get; private set; }

        public GameObject mainPanel;
        public Transform heroPanelsContainer;
        public UpgradeHeroPanel heroPanelPrefab;
        public Button closeButton;
        public List<CharacterData> allHeroData = new List<CharacterData>();

        private List<UpgradeHeroPanel> spawnedPanels = new List<UpgradeHeroPanel>();

        private void Awake()
        {
            Instance = this;
            mainPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            mainPanel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            mainPanel.SetActive(false);
        }

        public void Refresh()
        {
            // Clear existing
            foreach (var p in spawnedPanels) Destroy(p.gameObject);
            spawnedPanels.Clear();

            if (ProgressionManager.Instance == null) return;

            foreach (var state in ProgressionManager.Instance.playerStates)
            {
                var panel = Instantiate(heroPanelPrefab, heroPanelsContainer);
                panel.Bind(state);
                spawnedPanels.Add(panel);
            }
        }
    }
}
