using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BtlSys.UI
{
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        public TextMeshProUGUI tooltipText;
        public RectTransform panelRect;
        public Vector2 offset = new Vector2(0, 50);

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        public void Show(string text, Vector2 position)
        {
            gameObject.SetActive(true);
            tooltipText.text = text;
            transform.position = position + offset;
            
            // Layout refresh to resize panel
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
