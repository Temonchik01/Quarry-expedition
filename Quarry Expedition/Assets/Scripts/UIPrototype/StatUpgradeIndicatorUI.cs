using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BtlSys.UIPrototype
{
    /// <summary>Shows a stat's available upgrade and focused preview.</summary>
    public sealed class StatUpgradeIndicatorUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text preview;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite inverseSprite;

        private bool pointerOver;
        private bool selected;
        private bool full;
        private bool available;
        private int increase;

        public void Configure(Image upgradeIcon, TMP_Text previewText, Sprite normal, Sprite inverse)
        {
            icon = upgradeIcon;
            preview = previewText;
            normalSprite = normal;
            inverseSprite = inverse;
        }

        public void SetState(bool canUpgrade, bool isFull, int actualIncrease)
        {
            available = canUpgrade;
            full = isFull;
            increase = actualIncrease;
            preview.text = full ? "FULL" : available ? $"+{increase}" : string.Empty;
            preview.enabled = true;
            gameObject.SetActive(available);
            RefreshVisual();
        }

        private void OnEnable()
        {
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            RefreshVisual();
        }

        private void OnDisable()
        {
            pointerOver = false;
            selected = false;
            RefreshVisual();
        }

        private void Update()
        {
            if (!available || !(pointerOver || selected)) return;
            if (icon != null) icon.sprite = Mathf.FloorToInt(Time.unscaledTime) % 2 == 0 ? normalSprite : inverseSprite;
            if (preview != null) preview.enabled = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; RefreshVisual(); }
        public void OnPointerExit(PointerEventData eventData) { pointerOver = false; RefreshVisual(); }
        public void OnSelect(BaseEventData eventData) { selected = true; RefreshVisual(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; RefreshVisual(); }

        private void RefreshVisual()
        {
            if (icon == null || preview == null) return;
            bool focused = available && (pointerOver || selected);
            icon.sprite = focused ? inverseSprite : normalSprite;
            preview.gameObject.SetActive(full || focused);
            preview.enabled = true;
        }
    }
}
