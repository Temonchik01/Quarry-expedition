using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BtlSys.UI
{
    public class StatRowUI : MonoBehaviour
    {
        public TextMeshProUGUI statNameText;
        public TextMeshProUGUI valueText;
        public Button upgradeButton;
        public Slider progressBar;

        private ProgressionManager.CombatantState _state;
        private StatType _type;
        private UpgradeHeroPanel _parent;

        public void Setup(ProgressionManager.CombatantState state, StatType type, int baseVal, int boost, UpgradeHeroPanel parent)
        {
            _state = state;
            _type = type;
            _parent = parent;

            int total = baseVal + boost;
            statNameText.text = type.ToString().ToUpper();
            valueText.text = total.ToString();
            
            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = 250;
                progressBar.value = total;
            }

            bool canUpgrade = total < 250 && state.upgradePoints > 0;
            upgradeButton.gameObject.SetActive(canUpgrade);
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClick);
        }

        private void OnUpgradeClick()
        {
            UpgradeManager.Instance.UpgradeStat(_state, _type);
            _parent.Refresh();
        }
    }
}
