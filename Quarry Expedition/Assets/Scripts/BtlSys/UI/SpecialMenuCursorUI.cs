using UnityEngine;
using UnityEngine.UI;

namespace BtlSys.UI
{
    public class SpecialMenuCursorUI : MonoBehaviour
    {
        [SerializeField] private Vector2 offset = new Vector2(-8f, 0f);
        [SerializeField] private RectTransform viewport;
        private RectTransform canvasRect;
        private Image cursorImage;
        private RectTransform cursorRect;
        private RectTransform target;

        private void Awake()
        {
            canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
            cursorRect = transform as RectTransform;
            cursorImage = GetComponent<Image>();
            cursorImage.raycastTarget = false;
        }

        public void SetTarget(RectTransform button)
        {
            target = button;
            if (target == null) { Hide(); return; }
            gameObject.SetActive(true);
            UpdatePosition();
        }

        public void HideIfTarget(RectTransform button)
        {
            if (target == button) Hide();
        }

        public void Hide()
        {
            target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy) { Hide(); return; }
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 center = target.TransformPoint(target.rect.center);
            if (viewport != null)
            {
                Vector3 inViewport = viewport.InverseTransformPoint(center);
                cursorImage.enabled = viewport.rect.Contains(new Vector2(inViewport.x, inViewport.y));
            }
            Vector3 leftCenter = target.TransformPoint(new Vector3(target.rect.xMin, target.rect.center.y));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, leftCenter);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
                cursorRect.anchoredPosition = local + offset;
        }
    }
}
