using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BtlSys.UI
{
    public class SpecialAttackButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private AttackData attack;
        private int upgradeLevel;
        private Action<AttackData, int> onFocused;
        private ScrollRect scrollRect;
        private SpecialMenuCursorUI cursor;

        public void Bind(AttackData data, int level, Action<AttackData, int> focusHandler, ScrollRect scroll, SpecialMenuCursorUI menuCursor)
        {
            attack = data;
            upgradeLevel = level;
            onFocused = focusHandler;
            scrollRect = scroll;
            cursor = menuCursor;
        }

        public void OnPointerEnter(PointerEventData eventData) => Focus();
        public void OnPointerExit(PointerEventData eventData) => Blur();
        public void OnSelect(BaseEventData eventData)
        {
            EnsureVisible();
            Focus();
        }
        public void OnDeselect(BaseEventData eventData) => Blur();

        private void Focus()
        {
            onFocused?.Invoke(attack, upgradeLevel);
            if (cursor != null) cursor.SetTarget(transform as RectTransform);
        }

        private void Blur()
        {
            onFocused?.Invoke(null, 0);
            if (cursor != null) cursor.HideIfTarget(transform as RectTransform);
        }

        private void EnsureVisible()
        {
            if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null) return;
            Canvas.ForceUpdateCanvases();
            RectTransform item = transform as RectTransform;
            if (item == null) return;
            Vector3[] corners = new Vector3[4];
            item.GetWorldCorners(corners);
            float bottom = scrollRect.viewport.InverseTransformPoint(corners[0]).y;
            float top = scrollRect.viewport.InverseTransformPoint(corners[1]).y;
            Rect view = scrollRect.viewport.rect;
            Vector2 position = scrollRect.content.anchoredPosition;
            if (bottom < view.yMin) position.y += view.yMin - bottom;
            else if (top > view.yMax) position.y -= top - view.yMax;
            scrollRect.content.anchoredPosition = position;
        }
    }
}
