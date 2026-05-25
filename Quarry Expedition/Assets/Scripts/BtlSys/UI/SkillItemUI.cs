using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace BtlSys.UI
{
    public class SkillItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI skillNameText;
        public Button upgradeButton;
        public GameObject winkingCorner; // Visual element to wink
        public GameObject placeholderIcon;

        private ProgressionManager.CombatantState _state;
        private AttackData _skill;
        private UpgradeHeroPanel _parent;
        private bool _isHovered;
        private bool _isUpgraded;

        public void Bind(ProgressionManager.CombatantState state, AttackData skill, UpgradeHeroPanel parent)
        {
            _state = state;
            _skill = skill;
            _parent = parent;
            _isUpgraded = state.upgradedSkills.Contains(skill.attackName);

            skillNameText.text = skill.attackName + (_isUpgraded ? "<color=green>+</color>" : "");
            
            bool canUpgrade = !_isUpgraded && state.upgradePoints > 0;
            upgradeButton.gameObject.SetActive(canUpgrade);
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClick);
            
            if (placeholderIcon != null) placeholderIcon.SetActive(false);
            if (winkingCorner != null) winkingCorner.SetActive(false);
        }

        public void SetPlaceholder()
        {
            skillNameText.text = "";
            upgradeButton.gameObject.SetActive(true); // User said buttons with + in it
            upgradeButton.GetComponentInChildren<TextMeshProUGUI>().text = "+";
            upgradeButton.interactable = false; // Placeholder
            if (placeholderIcon != null) placeholderIcon.SetActive(true);
            if (winkingCorner != null) winkingCorner.SetActive(false);
        }

        private void OnUpgradeClick()
        {
            UpgradeManager.Instance.UpgradeSkill(_state, _skill);
            _parent.Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skill == null) return;
            _isHovered = true;
            _parent.ShowDescription(_skill, _isUpgraded);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_skill == null) return;
            _isHovered = false;
            if (winkingCorner != null) winkingCorner.SetActive(false);
            _parent.HideDescription();
        }

        private void Update()
        {
            if (_isHovered && winkingCorner != null)
            {
                // Wink every 2 seconds
                // Use a sine wave or timer
                float val = Mathf.Repeat(Time.time, 2f);
                winkingCorner.SetActive(val < 1.0f);
            }
        }
    }
}
