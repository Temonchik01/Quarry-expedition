using System;
using System.Collections;
using BtlSys.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BtlSys.UIPrototype
{
    public enum MenuSection { None = -1, Squad, Skills, Database, Settings, Title }

    /// <summary>Arranges section buttons and the three fighter choices.</summary>
    public sealed class MenuPrototypeNavigationUI : MonoBehaviour
    {
        [SerializeField] private RectTransform navigationItems;
        [SerializeField] private RectTransform upperButtons;
        [SerializeField] private RectTransform bottomButtons;
        [SerializeField] private RectTransform mercenaryList;
        [SerializeField] private GameObject flexibleSpacer;
        [SerializeField] private Button[] sectionButtons;
        [SerializeField] private Button[] fighterButtons;
        [SerializeField] private float transitionDuration = 0.18f;

        public event Action<MenuSection> SectionSelected;
        public event Action<int> FighterSelected;

        public MenuSection ActiveSection { get; private set; } = MenuSection.None;
        public int SelectedFighter { get; private set; } = -1;

        private Coroutine transition;
        private CanvasGroup fighterCanvas;
        private bool initialized;

        public void Configure(RectTransform items, RectTransform upper, RectTransform bottom,
            RectTransform fighters, GameObject spacer, Button[] sections, Button[] fighterChoices)
        {
            navigationItems = items;
            upperButtons = upper;
            bottomButtons = bottom;
            mercenaryList = fighters;
            flexibleSpacer = spacer;
            sectionButtons = sections;
            fighterButtons = fighterChoices;
        }

        public void Initialize()
        {
            if (initialized) return;
            if (navigationItems == null || upperButtons == null || bottomButtons == null ||
                mercenaryList == null || flexibleSpacer == null || sectionButtons == null || fighterButtons == null)
            {
                Debug.LogError("[MenuPrototype] GlobalNavigation is not configured.", this);
                return;
            }

            fighterCanvas = mercenaryList.GetComponent<CanvasGroup>();
            for (int i = 0; i < sectionButtons.Length; i++)
            {
                int index = i;
                sectionButtons[i].onClick.AddListener(() => ChooseSection((MenuSection)index));
            }
            for (int i = 0; i < fighterButtons.Length; i++)
            {
                int index = i;
                fighterButtons[i].onClick.AddListener(() => ChooseFighter(index));
            }
            initialized = true;
            ShowSection(MenuSection.None, false);
        }

        private void ChooseSection(MenuSection next)
        {
            ShowSection(next, true);
            SectionSelected?.Invoke(next);
        }

        private void ChooseFighter(int index)
        {
            if (index < 0 || index >= fighterButtons.Length) return;
            ShowFighter(index);
            FighterSelected?.Invoke(index);
        }

        public void ShowSection(MenuSection next, bool animate)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
                RestoreLayout();
            }

            Vector3[] starts = new Vector3[sectionButtons.Length];
            for (int i = 0; i < sectionButtons.Length; i++) starts[i] = sectionButtons[i].transform.position;
            bool fightersWereVisible = mercenaryList.gameObject.activeSelf;
            Vector3 fighterStart = mercenaryList.position;

            ActiveSection = next;
            ShowFighter(-1);
            int activeIndex = (int)next;
            int upperCount = 0;
            int bottomCount = 0;
            for (int i = 0; i < sectionButtons.Length; i++)
            {
                bool inUpper = activeIndex < 0 || i <= activeIndex;
                Transform group = inUpper ? upperButtons : bottomButtons;
                sectionButtons[i].transform.SetParent(group, false);
                sectionButtons[i].transform.SetAsLastSibling();
                if (inUpper) upperCount++;
                else bottomCount++;
                StyleButton(sectionButtons[i], i == activeIndex);
            }

            SetGroupHeight(upperButtons, upperCount);
            SetGroupHeight(bottomButtons, bottomCount);
            bottomButtons.gameObject.SetActive(bottomCount > 0);
            flexibleSpacer.SetActive(bottomCount > 0);
            bool showFighters = next == MenuSection.Squad || next == MenuSection.Skills;
            mercenaryList.gameObject.SetActive(showFighters);
            if (fighterCanvas != null)
            {
                fighterCanvas.alpha = 1f;
                fighterCanvas.blocksRaycasts = true;
            }
            RebuildLayout();

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(activeIndex >= 0 ? sectionButtons[activeIndex].gameObject : null);

            if (!animate || !Application.isPlaying) return;
            Vector3[] targets = new Vector3[sectionButtons.Length];
            for (int i = 0; i < sectionButtons.Length; i++) targets[i] = sectionButtons[i].transform.position;
            Vector3 fighterTarget = mercenaryList.position;
            transition = StartCoroutine(AnimateLayout(starts, targets, fighterStart, fighterTarget,
                fightersWereVisible, showFighters));
        }

        public void ShowFighter(int index)
        {
            SelectedFighter = index;
            for (int i = 0; i < fighterButtons.Length; i++)
            {
                bool selected = i == index;
                StyleButton(fighterButtons[i], selected);
                if (EventSystem.current != null && selected)
                    EventSystem.current.SetSelectedGameObject(fighterButtons[i].gameObject);
            }
        }

        private static void StyleButton(Button button, bool active)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            SpriteState sprites = button.spriteState;
            image.sprite = active ? sprites.selectedSprite : sprites.disabledSprite;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = active ? new Color32(6, 6, 8, 255) : new Color32(227, 230, 255, 255);
            ButtonInverseTextUI style = button.GetComponent<ButtonInverseTextUI>();
            if (style != null) style.SetPersistentInverse(active);
        }

        private static void SetGroupHeight(RectTransform group, int count)
        {
            LayoutElement layout = group.GetComponent<LayoutElement>();
            layout.preferredHeight = count > 0 ? count * 22 + (count - 1) * 2 : 0;
        }

        private void RebuildLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(upperButtons);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bottomButtons);
            LayoutRebuilder.ForceRebuildLayoutImmediate(navigationItems);
            Canvas.ForceUpdateCanvases();
        }

        private IEnumerator AnimateLayout(Vector3[] starts, Vector3[] targets,
            Vector3 fighterStart, Vector3 fighterTarget, bool fightersWereVisible, bool showFighters)
        {
            VerticalLayoutGroup parentLayout = navigationItems.GetComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup upperLayout = upperButtons.GetComponent<VerticalLayoutGroup>();
            VerticalLayoutGroup bottomLayout = bottomButtons.GetComponent<VerticalLayoutGroup>();
            parentLayout.enabled = false;
            upperLayout.enabled = false;
            bottomLayout.enabled = false;
            for (int i = 0; i < sectionButtons.Length; i++) sectionButtons[i].transform.position = starts[i];
            if (showFighters)
            {
                mercenaryList.position = fightersWereVisible ? fighterStart : fighterTarget;
                if (fighterCanvas != null)
                {
                    fighterCanvas.alpha = fightersWereVisible ? 1f : 0f;
                    fighterCanvas.blocksRaycasts = false;
                }
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                for (int i = 0; i < sectionButtons.Length; i++)
                    sectionButtons[i].transform.position = Vector3.Lerp(starts[i], targets[i], amount);
                if (showFighters)
                {
                    mercenaryList.position = Vector3.Lerp(fightersWereVisible ? fighterStart : fighterTarget, fighterTarget, amount);
                    if (fighterCanvas != null) fighterCanvas.alpha = fightersWereVisible ? 1f : amount;
                }
                yield return null;
            }

            RestoreLayout();
            transition = null;
        }

        private void RestoreLayout()
        {
            navigationItems.GetComponent<VerticalLayoutGroup>().enabled = true;
            upperButtons.GetComponent<VerticalLayoutGroup>().enabled = true;
            bottomButtons.GetComponent<VerticalLayoutGroup>().enabled = true;
            if (fighterCanvas != null)
            {
                fighterCanvas.alpha = 1f;
                fighterCanvas.blocksRaycasts = true;
            }
            RebuildLayout();
        }
    }
}
