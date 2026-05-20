using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BtlSys.UI
{
    public class CombatantUI : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public Slider hpSlider;
        public TextMeshProUGUI hpText;
        public Slider amSlider;
        public TextMeshProUGUI amText;

        private Combatant target;

        public Combatant Target => target;

        public void Bind(Combatant combatant)
        {
            target = combatant;
            nameText.text = combatant.data.characterName;
            UpdateUI();
            combatant.onStatsChanged += UpdateUI;
        }

        private void UpdateUI()
        {
            if (target == null) return;

            hpSlider.maxValue = target.data.maxHealth;
            hpSlider.value = target.currentHP;
            hpText.text = $"{target.currentHP}/{target.data.maxHealth}";

            amSlider.maxValue = target.data.maxAmmo;
            amSlider.value = target.currentAmmo;
            amText.text = $"{target.currentAmmo}/{target.data.maxAmmo}";

            // If dead, maybe grey out or hide?
            if (target.currentHP <= 0)
            {
                GetComponent<CanvasGroup>().alpha = 0.5f;
            }
        }

        public void SetHighlight(bool active)
        {
            // Logic to move panel to the right
            RectTransform rect = GetComponent<RectTransform>();
            Vector2 anchoredPos = rect.anchoredPosition;
            anchoredPos.x = active ? 50 : 0;
            rect.anchoredPosition = anchoredPos;
        }
    }
}
