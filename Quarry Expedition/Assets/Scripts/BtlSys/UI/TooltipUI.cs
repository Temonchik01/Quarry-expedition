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

        private Vector3 defaultPanelPosition;

        private void Awake()
        {
            Instance = this;
            defaultPanelPosition = panelRect.anchoredPosition3D;
            gameObject.SetActive(false);
        }

        public void ShowAction(BattleAction action)
        {
            if (action == null || action.User == null || tooltipText == null || panelRect == null) return;

            string actorName = action.User.data != null && !string.IsNullOrWhiteSpace(action.User.data.characterName)
                ? action.User.data.characterName
                : action.User.name;
            string actionName = ActionName(action);

            tooltipText.text = $"{actorName}\n{actionName}";
            panelRect.anchoredPosition3D = defaultPanelPosition;
            gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
        }

        private static string ActionName(BattleAction action)
        {
            switch (action.Type)
            {
                case BattleActionType.Attack:
                    AttackData attack = action.Attack ?? action.User.data?.basicAttack;
                    return attack != null ? attack.attackName : "Attack";
                case BattleActionType.Reload:
                    return "Reload";
                case BattleActionType.Defend:
                    return "Defend";
                default:
                    return action.Type.ToString();
            }
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
