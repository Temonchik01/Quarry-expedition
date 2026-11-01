using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BtlSys.UI
{
    /// <summary>Keeps button label contrast aligned with the Button/ButtonInverse sprite state.</summary>
    public sealed class ButtonInverseTextUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Color normalTextColor = new Color32(227, 230, 255, 255);
        [SerializeField] private Color inverseTextColor = new Color32(6, 6, 8, 255);

        private Button button;
        private TMP_Text[] labels;
        private Image[] selectableIcons;
        private bool pointerOver;
        private bool selected;
        private bool persistentInverse;
        [SerializeField] private bool hideBackgroundWhenIdle;

        public void SetPersistentInverse(bool value)
        {
            EnsureInitialized();
            persistentInverse = value;
            if (!value) selected = false;
            ApplyTextColor();
        }

        public void SetHideBackgroundWhenIdle(bool value)
        {
            hideBackgroundWhenIdle = value;
            ApplyTextColor();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (button == null) button = GetComponent<Button>();
            if (labels == null) labels = GetComponentsInChildren<TMP_Text>(true);
            if (selectableIcons == null) selectableIcons = GetComponentsInChildren<Image>(true);
        }

        private void OnEnable()
        {
            selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            ApplyTextColor();
        }

        private void OnDisable()
        {
            pointerOver = false;
            selected = false;
            ApplyTextColor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOver = true;
            ApplyTextColor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
            ApplyTextColor();
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            ApplyTextColor();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            ApplyTextColor();
        }

        private void ApplyTextColor()
        {
            EnsureInitialized();
            if (labels == null) return;
            bool inverse = button != null && button.IsInteractable() && (pointerOver || selected || persistentInverse);
            Color color = inverse ? inverseTextColor : normalTextColor;
            foreach (TMP_Text label in labels)
                if (label != null) label.color = color;
            if (hideBackgroundWhenIdle && button != null && button.targetGraphic is Image background)
                background.color = inverse ? Color.white : new Color(1f, 1f, 1f, 0f);
            if (selectableIcons == null) return;
            foreach (Image icon in selectableIcons)
                if (icon != null && icon.gameObject.name == "NewAttack")
                    icon.color = inverse ? inverseTextColor : Color.white;
        }
    }
}
