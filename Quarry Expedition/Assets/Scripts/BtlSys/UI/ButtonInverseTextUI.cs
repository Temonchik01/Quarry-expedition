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
        private bool pointerOver;
        private bool selected;

        private void Awake()
        {
            button = GetComponent<Button>();
            labels = GetComponentsInChildren<TMP_Text>(true);
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
            if (labels == null) return;
            bool inverse = button != null && button.IsInteractable() && (pointerOver || selected);
            Color color = inverse ? inverseTextColor : normalTextColor;
            foreach (TMP_Text label in labels)
                if (label != null) label.color = color;
        }
    }
}
