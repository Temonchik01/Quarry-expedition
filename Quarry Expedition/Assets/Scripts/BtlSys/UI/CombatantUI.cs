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

        [SerializeField] private Color activeCardColor = new Color(1f, 0.85f, 0.55f);
        private Combatant target;
        private Image cardImage;
        private Color normalCardColor;

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

        private void Awake()
        {
            // Set pivot to left center to prevent going off-screen when adjusting anchoredPosition
            RectTransform rect = GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0.5f);
            cardImage = GetComponent<Image>();
            if (cardImage != null) normalCardColor = cardImage.color;
        }

        private void OnDestroy()
        {
            if (target != null) target.onStatsChanged -= UpdateUI;
        }

        public void SetHighlight(bool active)
        {
            // Logic to move panel to the right
            RectTransform rect = GetComponent<RectTransform>();
            Vector2 anchoredPos = rect.anchoredPosition;
            // When not highlighted, x should be 0 (left edge of container)
            // When highlighted, x should be 50 (pushed to the right)
            anchoredPos.x = active ? 50 : 0;
            rect.anchoredPosition = anchoredPos;
            if (cardImage != null) cardImage.color = active ? activeCardColor : normalCardColor;
        }
    }
}
