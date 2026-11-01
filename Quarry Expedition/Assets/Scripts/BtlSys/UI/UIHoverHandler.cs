using UnityEngine;
using UnityEngine.EventSystems;

namespace BtlSys.UI
{
    public class UIHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string tooltipContent;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return;
            if (TooltipUI.Instance != null) TooltipUI.Instance.Show(tooltipContent, transform.position);
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(transform, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return;
            if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
            if (SelectionCursorUI.Instance != null) SelectionCursorUI.Instance.SetTarget(null);
        }
    }
}
