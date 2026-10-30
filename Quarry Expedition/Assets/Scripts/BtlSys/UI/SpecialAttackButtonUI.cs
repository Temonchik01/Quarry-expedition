using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BtlSys.UI
{
    public class SpecialAttackButtonUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private AttackData attack;
        private Action<AttackData> onFocused;

        public void Bind(AttackData data, Action<AttackData> focusHandler)
        {
            attack = data;
            onFocused = focusHandler;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onFocused?.Invoke(attack);
        }

        public void OnSelect(BaseEventData eventData)
        {
            onFocused?.Invoke(attack);
        }
    }
}
