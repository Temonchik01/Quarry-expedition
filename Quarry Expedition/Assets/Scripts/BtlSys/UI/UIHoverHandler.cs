using UnityEngine;
using UnityEngine.EventSystems;

namespace BtlSys.UI
{
    public class UIHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string tooltipContent;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipUI.Instance.Show(tooltipContent, transform.position);
            SelectionCursorUI.Instance.SetTarget(transform, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipUI.Instance.Hide();
            SelectionCursorUI.Instance.SetTarget(null);
        }
    }
}
