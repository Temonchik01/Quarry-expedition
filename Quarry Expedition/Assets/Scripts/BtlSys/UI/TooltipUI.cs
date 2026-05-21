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
        public Vector2 offset = new Vector2(0, 0.35f);

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        public void Show(string text, Vector3 position)
        {
            gameObject.SetActive(true);
            tooltipText.text = text;
            
            // position is the world position of the button center
            // We move it up by the offset and slightly forward (-0.1f) to ensure it's in front of the menu
            transform.position = position + (Vector3)offset + Camera.main.transform.forward * -0.1f;
            
            // Layout refresh to resize panel
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
