using System;
using BtlSys.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BtlSys.UIPrototype
{
    /// <summary>Runs the mock Squad and Skills screens in MenuPrototype.unity.</summary>
    public sealed class MenuPrototypeController : MonoBehaviour
    {
        [SerializeField] private Sprite[] portraits = new Sprite[3];

        private enum SkillMode { None = -1, Install, Replace, Remove, Recycle, Inspect }
        private enum PendingKind { None, Install, ReplaceFromReserve, Swap, Remove, RecycleFromReserve, RecycleEquipped }

        private sealed class SkillState
        {
            public string id;
            public string name;
            public int count;
            public int upgradeLevel;
            public string target;
            public string power;
            public int accuracy;
            public int ammoCost;
            public string description;
            public string currentEffect;
            public string nextEffect;

            public SkillState(string id, string name, int count, int level, string target, string power,
                int accuracy, int ammoCost, string description, string currentEffect, string nextEffect)
            {
                this.id = id;
                this.name = name;
                this.count = count;
                upgradeLevel = level;
                this.target = target;
                this.power = power;
                this.accuracy = accuracy;
                this.ammoCost = ammoCost;
                this.description = description;
                this.currentEffect = currentEffect;
                this.nextEffect = nextEffect;
            }

            public SkillState CopyForEquipped()
            {
                return new SkillState(id, name, 1, upgradeLevel, target, power, accuracy, ammoCost,
                    description, currentEffect, nextEffect);
            }
        }

        private sealed class MercenaryState
        {
            public string name;
            public int[] stats;
            public int[] statUpgrades = new int[5];
            public Sprite portrait;
            public SkillState[] equipped = new SkillState[8];
            public List<SkillState> reserve = new List<SkillState>();

            public MercenaryState(string name, int[] stats, Sprite portrait)
            {
                this.name = name;
                this.stats = stats;
                this.portrait = portrait;
            }
        }

        private readonly string[] statNames = { "HEALTH", "ATTACK", "DEFENCE", "AMMO", "SPEED" };
        private readonly List<MercenaryState> roster = new List<MercenaryState>();
        private MenuSection section = MenuSection.None;
        private SkillMode skillMode = SkillMode.None;
        private PendingKind pendingKind;
        private int selectedMercenary = -1;
        private int upgradePoints = 10;
        private int selectedReserveVisible = -1;
        private SkillState selectedReserveSkill;
        private int selectedSquadSkill = -1;
        private int selectedSkillSlot = -1;
        private int reserveStart;
        private int targetSlot = -1;
        private int targetOtherSlot = -1;
        private int targetReserve = -1;
        private int scrap;

        private Transform menuRoot;
        private MenuPrototypeNavigationUI navigation;
        private GameObject sectionMainWindow;
        private GameObject squadContent;
        private GameObject skillsContent;
        private GameObject placeholderContent;
        private GameObject confirmationPanel;
        private TMP_Text confirmationMessage;
        private TMP_Text rewardText;
        private TMP_Text scrapText;
        private TMP_Text statusText;

        private void Start()
        {
            InitializePrototype();
        }

        public void InitializePrototype()
        {
            menuRoot = transform;
            navigation = menuRoot.Find("GlobalNavigation").GetComponent<MenuPrototypeNavigationUI>();
            sectionMainWindow = menuRoot.Find("SectionMainWindow").gameObject;
            squadContent = sectionMainWindow.transform.Find("SquadContent").gameObject;
            skillsContent = sectionMainWindow.transform.Find("SkillsContent").gameObject;
            placeholderContent = sectionMainWindow.transform.Find("PlaceholderContent").gameObject;
            confirmationPanel = skillsContent.transform.Find("FighterColumn/ConfirmationPanel").gameObject;
            confirmationMessage = Text("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/MessageText");
            rewardText = Text("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/RewardText");
            scrapText = Text("SectionMainWindow/SkillsContent/FighterColumn/ScrapText");
            statusText = Text("SectionMainWindow/SkillsContent/FighterColumn/StatusText");

            BuildMockRoster();
            BindNavigation();
            BindSquadControls();
            BindSkillsControls();
            SetSection(MenuSection.None);
        }

        private void BuildMockRoster()
        {
            roster.Clear();
            upgradePoints = 10;
            scrap = 0;
            MercenaryState mecha = new MercenaryState("Mecha", new[] { 100, 20, 60, 60, 250 }, Portrait(0));
            mecha.equipped[0] = NewSkill("drill", "Drill", 1);
            mecha.equipped[1] = NewSkill("tackle", "Tackle", 0);
            mecha.reserve.AddRange(new[]
            {
                NewSkill("grenade", "Grenade", 0, 10), NewSkill("emp", "EMP", 0),
                NewSkill("teamheal", "TeamHeal", 0), NewSkill("bandage", "Bandage", 0),
                NewSkill("charge", "Charge", 0), NewSkill("fort", "Fort", 0),
                NewSkill("drive", "Drive", 0), NewSkill("corrode", "Corrode", 0),
                NewSkill("ignite", "Ignite", 0), NewSkill("reload", "Reload", 0),
                NewSkill("zap", "Zap", 0)
            });
            roster.Add(mecha);

            MercenaryState drilldozer = new MercenaryState("Drilldozer", new[] { 200, 35, 80, 30, 140 }, Portrait(1));
            drilldozer.equipped[0] = NewSkill("charge", "Charge", 0);
            drilldozer.equipped[1] = NewSkill("fort", "Fortification", 0);
            drilldozer.reserve.AddRange(new[] { NewSkill("drill", "Drill", 0), NewSkill("ram", "Ram", 0), NewSkill("tackle", "Tackle", 0) });
            roster.Add(drilldozer);

            MercenaryState tank = new MercenaryState("Tank", new[] { 150, 40, 75, 40, 110 }, Portrait(2));
            tank.equipped[0] = NewSkill("fire", "Fire Barrage", 1);
            tank.equipped[1] = NewSkill("shrapnel", "Shrapnel", 0);
            tank.reserve.AddRange(new[] { NewSkill("charge", "Charge", 0), NewSkill("reload", "Reload", 0), NewSkill("corrode", "Corrode", 0) });
            roster.Add(tank);
        }

        private static SkillState NewSkill(string id, string name, int level, int count = 1)
        {
            string description = name == "Drill"
                ? "Attempts three hits on one enemy."
                : $"Uses {name} to support the squad in combat.";
            string current = level > 0 ? "+ Hits every enemy once." : string.Empty;
            string next = level < 2 ? "+ Ignite enemy" : string.Empty;
            return new SkillState(id, name, count, level, "1 Enemy", "Low", 100, 20,
                description, current, next);
        }

        private Sprite Portrait(int index) => portraits != null && index < portraits.Length ? portraits[index] : null;

        private void BindNavigation()
        {
            navigation.SectionSelected += SetSection;
            navigation.FighterSelected += SelectMercenary;
            navigation.Initialize();
        }

        private void BindSquadControls()
        {
            Transform slots = menuRoot.Find("SectionMainWindow/SquadContent/OverviewPanel/EquippedSlots");
            for (int i = 0; i < slots.childCount; i++)
            {
                int index = i;
                Transform slot = slots.GetChild(i);
                slot.GetComponent<Button>().onClick.AddListener(() => SelectSquadSkill(index));
                slot.Find("PlusUpgrade").GetComponent<Button>().onClick.AddListener(() => UpgradeSquadSkillAt(index));
            }

            Transform stats = menuRoot.Find("SectionMainWindow/SquadContent/DetailsColumn/StatsPanel/StatsRows");
            for (int i = 0; i < statNames.Length; i++)
            {
                int index = i;
                stats.Find(statNames[i]).Find("UpgradeButton").GetComponent<Button>()
                    .onClick.AddListener(() => UpgradeStat(index));
            }
        }

        private void BindSkillsControls()
        {
            string[] modes = { "Install", "Replace", "Remove", "Recycle", "Inspect" };
            for (int i = 0; i < modes.Length; i++)
            {
                int index = i;
                ButtonAt($"SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/{modes[i]}")
                    .onClick.AddListener(() => SetSkillMode((SkillMode)index));
            }

            Transform equipped = menuRoot.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots");
            for (int i = 0; i < equipped.childCount; i++)
            {
                int index = i;
                equipped.GetChild(i).GetComponent<Button>().onClick.AddListener(() => SelectEquippedSlot(index));
            }

            ButtonAt("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/YesButton").onClick.AddListener(ConfirmPendingAction);
            ButtonAt("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/NoButton").onClick.AddListener(CancelPendingAction);
        }

        private void SetSection(MenuSection next)
        {
            section = next;
            selectedMercenary = -1;
            selectedSquadSkill = -1;
            selectedSkillSlot = -1;
            selectedReserveVisible = -1;
            selectedReserveSkill = null;
            reserveStart = 0;
            skillMode = SkillMode.None;
            ResetPendingAction();
            statusText.text = "Choose a mode, then a skill.";
            if (next == MenuSection.Database || next == MenuSection.Settings || next == MenuSection.Title)
                placeholderContent.transform.Find("Heading").GetComponent<TMP_Text>().text = next.ToString().ToUpperInvariant();
            RefreshContentVisibility();
        }

        private void RefreshContentVisibility()
        {
            bool fighterChosen = selectedMercenary >= 0;
            bool placeholder = section == MenuSection.Database || section == MenuSection.Settings || section == MenuSection.Title;
            squadContent.SetActive(section == MenuSection.Squad && fighterChosen);
            skillsContent.SetActive(section == MenuSection.Skills && fighterChosen);
            placeholderContent.SetActive(placeholder);
            sectionMainWindow.SetActive(placeholder || (fighterChosen &&
                (section == MenuSection.Squad || section == MenuSection.Skills)));
        }

        private void SelectMercenary(int index)
        {
            if ((section != MenuSection.Squad && section != MenuSection.Skills) || index < 0 || index >= roster.Count) return;
            selectedMercenary = index;
            selectedSquadSkill = -1;
            selectedSkillSlot = -1;
            selectedReserveVisible = -1;
            selectedReserveSkill = null;
            skillMode = SkillMode.None;
            reserveStart = 0;
            ResetPendingAction();
            RefreshContentVisibility();
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (selectedMercenary < 0 || selectedMercenary >= roster.Count) return;
            MercenaryState fighter = roster[selectedMercenary];
            Text("SectionMainWindow/SquadContent/OverviewPanel/NameText").text = fighter.name.ToUpperInvariant();
            Text("SectionMainWindow/SkillsContent/FighterColumn/NameText").text = fighter.name.ToUpperInvariant();
            Image("SectionMainWindow/SquadContent/OverviewPanel/PortraitFrame/Portrait").sprite = fighter.portrait;
            Image("SectionMainWindow/SkillsContent/FighterColumn/PortraitFrame/Portrait").sprite = fighter.portrait;
            Text("SectionMainWindow/SquadContent/OverviewPanel/PointsText").text = $"POINTS:{upgradePoints}";
            Text("SectionMainWindow/SkillsContent/FighterColumn/ScrapText").text = $"SCRAP:{scrap}";
            RefreshStats(fighter);
            RefreshSquadSlots(fighter);
            RefreshEquippedSkills(fighter);
            RefreshReserve(fighter);
            RefreshInstallAvailability(fighter);
            RefreshDetails();
        }

        private void RefreshInstallAvailability(MercenaryState fighter)
        {
            bool canInstall = Array.Exists(fighter.equipped, skill => skill == null) &&
                              fighter.reserve.Exists(skill => skill.count > 0);
            ButtonAt("SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/Install")
                .gameObject.SetActive(canInstall);
            if (!canInstall && skillMode == SkillMode.Install) skillMode = SkillMode.None;
        }

        private void RefreshStats(MercenaryState fighter)
        {
            Transform rows = menuRoot.Find("SectionMainWindow/SquadContent/DetailsColumn/StatsPanel/StatsRows");
            for (int i = 0; i < statNames.Length; i++)
            {
                Transform row = rows.Find(statNames[i]);
                int value = fighter.stats[i];
                bool full = value >= 250 || fighter.statUpgrades[i] >= 2;
                int increase = Mathf.Min(20, 250 - value);
                Text(row, "ValueText").text = value.ToString();
                Text(row, "Label").text = statNames[i] + (fighter.statUpgrades[i] > 0 ? "<color=#14A02E>+</color>" : string.Empty);
                row.Find("UpgradeButton").GetComponent<StatUpgradeIndicatorUI>()
                    .SetState(!full && upgradePoints > 0, full, increase);
                Image fill = row.Find("Bar/Fill").GetComponent<Image>();
                fill.fillAmount = Mathf.Clamp01(value / 250f);
            }
        }

        private void RefreshSquadSlots(MercenaryState fighter)
        {
            Transform slots = menuRoot.Find("SectionMainWindow/SquadContent/OverviewPanel/EquippedSlots");
            for (int i = 0; i < slots.childCount; i++)
            {
                SkillState skill = fighter.equipped[i];
                SetSkillSlot(slots.GetChild(i), skill, false);
            }
            UpdateDetails(menuRoot.Find("SectionMainWindow/SquadContent/DetailsColumn/SkillDescriptionPanel"),
                selectedSquadSkill >= 0 && selectedSquadSkill < fighter.equipped.Length
                    ? fighter.equipped[selectedSquadSkill] : null);
        }

        private void RefreshEquippedSkills(MercenaryState fighter)
        {
            Transform slots = menuRoot.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots");
            for (int i = 0; i < slots.childCount; i++)
            {
                SkillState skill = fighter.equipped[i];
                SetSkillSlot(slots.GetChild(i), skill, false);
            }
        }

        private void RefreshReserve(MercenaryState fighter)
        {
            Transform slots = menuRoot.Find("SectionMainWindow/SkillsContent/ReservePanel/ReserveSlots");
            int visible = slots.childCount;
            int maxStart = Mathf.Max(0, fighter.reserve.Count - (visible - 1));
            reserveStart = Mathf.Clamp(reserveStart, 0, maxStart);
            bool showUp = reserveStart > 0;
            bool showDown = reserveStart < maxStart;
            int firstContentSlot = showUp ? 1 : 0;

            for (int i = 0; i < visible; i++)
            {
                int visibleIndex = i;
                Button button = slots.GetChild(i).GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                if (i == 0 && showUp)
                {
                    SetSkillSlot(slots.GetChild(i), null, true);
                    button.GetComponentInChildren<TMP_Text>().text = "↑";
                    button.interactable = true;
                    button.onClick.AddListener(() => ScrollReserve(fighter, -1, visibleIndex));
                    continue;
                }
                if (i == visible - 1 && showDown)
                {
                    SetSkillSlot(slots.GetChild(i), null, true);
                    button.GetComponentInChildren<TMP_Text>().text = "↓";
                    button.interactable = true;
                    button.onClick.AddListener(() => ScrollReserve(fighter, 1, visibleIndex));
                    continue;
                }

                int reserveIndex = reserveStart + i - firstContentSlot;
                SkillState skill = reserveIndex < fighter.reserve.Count ? fighter.reserve[reserveIndex] : null;
                SetSkillSlot(slots.GetChild(i), skill, true);
                button.interactable = skill != null;
                if (skill != null)
                    button.onClick.AddListener(() => SelectReserveSkill(reserveIndex, visibleIndex));
            }
        }

        private void ScrollReserve(MercenaryState fighter, int direction, int visibleIndex)
        {
            reserveStart += direction;
            RefreshReserve(fighter);
            FocusReserveRow(visibleIndex);
        }

        private void FocusReserveRow(int visibleIndex)
        {
            if (EventSystem.current == null || visibleIndex < 0) return;
            Transform slot = menuRoot.Find($"SectionMainWindow/SkillsContent/ReservePanel/ReserveSlots/ReserveSlot{visibleIndex}");
            if (slot != null && slot.GetComponent<Button>().IsInteractable())
                EventSystem.current.SetSelectedGameObject(slot.gameObject);
        }

        private void SetSkillSlot(Transform slot, SkillState skill, bool reserve)
        {
            Button button = slot.GetComponent<Button>();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            Transform plus = slot.Find("NewAttack");
            if (skill == null)
            {
                label.text = string.Empty;
                if (plus != null) plus.gameObject.SetActive(!reserve);
            }
            else
            {
                string suffix = skill.upgradeLevel > 0
                    ? $" <color=#25D65A>{new string('+', skill.upgradeLevel)}</color>" : string.Empty;
                string count = reserve ? $" x{skill.count}" : string.Empty;
                label.text = skill.name + suffix + count;
                if (plus != null) plus.gameObject.SetActive(false);
            }
            Transform upgradeAction = slot.Find("PlusUpgrade");
            if (upgradeAction != null)
                upgradeAction.gameObject.SetActive(skill != null && upgradePoints > 0 &&
                    !string.IsNullOrEmpty(skill.nextEffect));
        }

        private void SelectSquadSkill(int index)
        {
            if (index < 0 || index >= roster[selectedMercenary].equipped.Length) return;
            selectedSquadSkill = index;
            UpdateDetails(menuRoot.Find("SectionMainWindow/SquadContent/DetailsColumn/SkillDescriptionPanel"),
                roster[selectedMercenary].equipped[index]);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(menuRoot.Find($"SectionMainWindow/SquadContent/OverviewPanel/EquippedSlots/Slot{index}").gameObject);
        }

        private void UpgradeStat(int index)
        {
            if (selectedMercenary < 0) return;
            MercenaryState fighter = roster[selectedMercenary];
            if (upgradePoints <= 0 || fighter.statUpgrades[index] >= 2 || fighter.stats[index] >= 250) return;
            fighter.stats[index] = Mathf.Min(250, fighter.stats[index] + 20);
            fighter.statUpgrades[index]++;
            upgradePoints--;
            RefreshAll();
            Debug.Log($"[MenuPrototype] {fighter.name} {statNames[index]} upgraded to {fighter.stats[index]}.");
        }

        private void UpgradeSquadSkillAt(int index)
        {
            if (selectedMercenary < 0 || index < 0 || index >= roster[selectedMercenary].equipped.Length) return;
            selectedSquadSkill = index;
            UpgradeSkill(roster[selectedMercenary].equipped[index]);
            RefreshAll();
        }

        private void UpgradeSkill(SkillState skill)
        {
            if (skill == null || upgradePoints <= 0 || skill.upgradeLevel >= 2 || string.IsNullOrEmpty(skill.nextEffect)) return;
            skill.upgradeLevel++;
            upgradePoints--;
            skill.currentEffect = skill.upgradeLevel == 1
                ? "+ Hits every enemy once."
                : "+ Hits every enemy once.\n+ Ignite enemy";
            skill.nextEffect = skill.upgradeLevel < 2 ? "+ Ignite enemy" : string.Empty;
            Debug.Log($"[MenuPrototype] {skill.name} upgraded to level {skill.upgradeLevel}.");
        }

        private void SetSkillMode(SkillMode mode)
        {
            skillMode = mode;
            targetSlot = -1;
            targetOtherSlot = -1;
            targetReserve = -1;
            ResetPendingAction();
            if (mode == SkillMode.Inspect)
            {
                statusText.text = "Select a reserve or installed skill to inspect.";
            }
            else statusText.text = $"{mode}: select a skill or slot.";
            ButtonAt($"SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/{mode}").Select();
        }

        private void SelectEquippedSlot(int index)
        {
            MercenaryState fighter = roster[selectedMercenary];
            SkillState skill = fighter.equipped[index];
            selectedSkillSlot = skill != null ? index : -1;
            selectedReserveSkill = null;
            UpdateDetails(menuRoot.Find("SectionMainWindow/SkillsContent/SkillDescriptionPanel"), skill);

            switch (skillMode)
            {
                case SkillMode.Install:
                    if (skill != null) { statusText.text = "Choose an empty special slot."; return; }
                    targetSlot = index;
                    statusText.text = "Empty slot chosen. Select one reserve skill.";
                    if (targetReserve >= 0) StageInstall();
                    break;
                case SkillMode.Replace:
                    if (targetSlot < 0)
                    {
                        if (skill == null) { statusText.text = "Choose an installed skill to replace."; return; }
                        targetSlot = index;
                        statusText.text = "Choose a reserve skill or another installed slot.";
                    }
                    else if (index != targetSlot && skill != null)
                    {
                        targetOtherSlot = index;
                        pendingKind = PendingKind.Swap;
                        ShowConfirmation($"Swap {fighter.equipped[targetSlot].name} and {skill.name}?", string.Empty);
                    }
                    break;
                case SkillMode.Remove:
                    if (skill == null) { statusText.text = "Choose an installed skill."; return; }
                    targetSlot = index;
                    pendingKind = PendingKind.Remove;
                    ShowConfirmation($"Return {skill.name} to Reserve?", string.Empty);
                    break;
                case SkillMode.Recycle:
                    if (skill == null) { statusText.text = "Choose an installed skill to recycle."; return; }
                    targetSlot = index;
                    pendingKind = PendingKind.RecycleEquipped;
                    ShowRecycleConfirmation(skill, false);
                    break;
                case SkillMode.Inspect:
                    statusText.text = skill != null ? $"Inspecting {skill.name}. No data changed." : "Empty slot.";
                    break;
            }
        }

        private void SelectReserveSkill(int reserveIndex, int visibleIndex)
        {
            selectedReserveVisible = visibleIndex;
            MercenaryState fighter = roster[selectedMercenary];
            if (reserveIndex < 0 || reserveIndex >= fighter.reserve.Count) return;
            SkillState skill = fighter.reserve[reserveIndex];
            selectedReserveSkill = skill;
            targetReserve = reserveIndex;
            UpdateDetails(menuRoot.Find("SectionMainWindow/SkillsContent/SkillDescriptionPanel"), skill);
            selectedSkillSlot = -1;

            switch (skillMode)
            {
                case SkillMode.Install:
                    if (targetSlot < 0) statusText.text = "Choose an empty special slot first.";
                    else StageInstall();
                    break;
                case SkillMode.Replace:
                    if (targetSlot < 0) statusText.text = "Choose an installed skill first.";
                    else
                    {
                        pendingKind = PendingKind.ReplaceFromReserve;
                        ShowConfirmation($"Replace {fighter.equipped[targetSlot].name} with {skill.name}?", "The old skill returns to Reserve.");
                    }
                    break;
                case SkillMode.Recycle:
                    pendingKind = PendingKind.RecycleFromReserve;
                    ShowRecycleConfirmation(skill, true);
                    break;
                case SkillMode.Inspect:
                    statusText.text = $"Inspecting {skill.name}. No data changed.";
                    break;
            }
        }

        private void StageInstall()
        {
            MercenaryState fighter = roster[selectedMercenary];
            if (targetReserve < 0) { statusText.text = "Choose a reserve skill."; return; }
            SkillState skill = fighter.reserve[targetReserve];
            pendingKind = PendingKind.Install;
            ShowConfirmation($"Install {skill.name} in Special {targetSlot + 1}?", "One reserve copy will be used.");
        }

        private void ShowRecycleConfirmation(SkillState skill, bool fromReserve)
        {
            int reward = 10 + 15 * skill.upgradeLevel;
            pendingKind = fromReserve ? PendingKind.RecycleFromReserve : PendingKind.RecycleEquipped;
            ShowConfirmation($"Recycle {skill.name}?", string.Empty);
            rewardText.text = $"FOR {reward} SCRAP";
            rewardText.color = new Color32(249, 163, 27, 255);
        }

        private void ShowConfirmation(string message, string detail)
        {
            confirmationMessage.text = message + (string.IsNullOrEmpty(detail) ? string.Empty : "\n" + detail);
            if (string.IsNullOrEmpty(rewardText.text) || !message.StartsWith("Recycle", StringComparison.Ordinal))
                rewardText.text = string.Empty;
            statusText.gameObject.SetActive(false);
            scrapText.gameObject.SetActive(false);
            confirmationPanel.SetActive(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(ButtonAt("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/YesButton").gameObject);
        }

        private void ConfirmPendingAction()
        {
            MercenaryState fighter = roster[selectedMercenary];
            switch (pendingKind)
            {
                case PendingKind.Install:
                    if (ValidReserveTarget() && targetSlot >= 0 && fighter.equipped[targetSlot] == null)
                    {
                        SkillState incoming = fighter.reserve[targetReserve].CopyForEquipped();
                        ConsumeReserve(targetReserve);
                        fighter.equipped[targetSlot] = incoming;
                        selectedSkillSlot = targetSlot;
                    }
                    break;
                case PendingKind.ReplaceFromReserve:
                    if (ValidReserveTarget() && targetSlot >= 0 && fighter.equipped[targetSlot] != null)
                    {
                        SkillState old = fighter.equipped[targetSlot];
                        SkillState incoming = fighter.reserve[targetReserve].CopyForEquipped();
                        ConsumeReserve(targetReserve);
                        AddToReserve(old);
                        fighter.equipped[targetSlot] = incoming;
                        selectedSkillSlot = targetSlot;
                    }
                    break;
                case PendingKind.Swap:
                    if (targetSlot >= 0 && targetOtherSlot >= 0)
                    {
                        (fighter.equipped[targetSlot], fighter.equipped[targetOtherSlot]) =
                            (fighter.equipped[targetOtherSlot], fighter.equipped[targetSlot]);
                    }
                    break;
                case PendingKind.Remove:
                    if (targetSlot >= 0 && fighter.equipped[targetSlot] != null)
                    {
                        AddToReserve(fighter.equipped[targetSlot]);
                        fighter.equipped[targetSlot] = null;
                    }
                    break;
                case PendingKind.RecycleFromReserve:
                    if (ValidReserveTarget()) Recycle(fighter.reserve[targetReserve], true);
                    break;
                case PendingKind.RecycleEquipped:
                    if (targetSlot >= 0 && fighter.equipped[targetSlot] != null)
                        Recycle(fighter.equipped[targetSlot], false);
                    break;
            }
            ResetPendingAction();
            selectedReserveSkill = null;
            statusText.text = "Done. Choose another mode or skill.";
            RefreshAll();
            RestoreSkillFocus();
        }

        private void Recycle(SkillState skill, bool fromReserve)
        {
            scrap += 10 + 15 * skill.upgradeLevel;
            if (!fromReserve) upgradePoints += skill.upgradeLevel;
            if (fromReserve) ConsumeReserve(targetReserve);
            else roster[selectedMercenary].equipped[targetSlot] = null;
        }

        private bool ValidReserveTarget()
        {
            return targetReserve >= 0 && targetReserve < roster[selectedMercenary].reserve.Count &&
                   roster[selectedMercenary].reserve[targetReserve].count > 0;
        }

        private void ConsumeReserve(int index)
        {
            SkillState item = roster[selectedMercenary].reserve[index];
            item.count--;
            if (item.count <= 0) roster[selectedMercenary].reserve.RemoveAt(index);
        }

        private void AddToReserve(SkillState skill)
        {
            SkillState match = roster[selectedMercenary].reserve.Find(item => item.id == skill.id && item.upgradeLevel == skill.upgradeLevel);
            if (match != null) match.count++;
            else
            {
                SkillState copy = skill.CopyForEquipped();
                copy.count = 1;
                roster[selectedMercenary].reserve.Add(copy);
            }
        }

        private void CancelPendingAction()
        {
            ResetPendingAction();
            statusText.text = "Cancelled. No data changed.";
            RestoreSkillFocus();
        }

        private void ResetPendingAction()
        {
            pendingKind = PendingKind.None;
            targetSlot = -1;
            targetOtherSlot = -1;
            targetReserve = -1;
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
            if (rewardText != null) rewardText.text = string.Empty;
            if (statusText != null) statusText.gameObject.SetActive(true);
            if (scrapText != null) scrapText.gameObject.SetActive(true);
        }

        private void RestoreSkillFocus()
        {
            if (EventSystem.current == null || section != MenuSection.Skills) return;
            Transform target = selectedSkillSlot >= 0
                ? menuRoot.Find($"SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot{selectedSkillSlot}")
                : selectedReserveVisible >= 0
                    ? menuRoot.Find($"SectionMainWindow/SkillsContent/ReservePanel/ReserveSlots/ReserveSlot{selectedReserveVisible}")
                    : null;
            if (target != null && target.GetComponent<Button>().IsInteractable())
                EventSystem.current.SetSelectedGameObject(target.gameObject);
            else if (skillMode != SkillMode.None)
                ButtonAt($"SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/{skillMode}").Select();
        }

        private void UpdateDetails(Transform panel, SkillState skill)
        {
            bool hasSkill = skill != null;
            panel.Find("TargetText").GetComponent<TMP_Text>().text = hasSkill ? skill.target : string.Empty;
            TMP_Text power = panel.Find("PowerText").GetComponent<TMP_Text>();
            power.text = hasSkill
                ? $"Power: <color=#{ColorUtility.ToHtmlStringRGB(PowerColor(skill.power))}>{skill.power}</color>"
                : string.Empty;
            power.color = new Color32(227, 230, 255, 255);
            panel.Find("AccuracyText").GetComponent<TMP_Text>().text = hasSkill ? $"Accuracy: {skill.accuracy}%" : string.Empty;
            panel.Find("CostText").GetComponent<TMP_Text>().text = hasSkill ? $"Cost: {skill.ammoCost} AM" : string.Empty;
            panel.Find("DescriptionText").GetComponent<TMP_Text>().text = hasSkill ? skill.description : string.Empty;
            panel.Find("EffectText").GetComponent<TMP_Text>().text = hasSkill ? skill.currentEffect : string.Empty;
            panel.Find("NextUpgradeText").GetComponent<TMP_Text>().text = hasSkill && !string.IsNullOrEmpty(skill.nextEffect)
                ? $"Next upgrade:\n<color=#14A02E>{skill.nextEffect}</color>" : string.Empty;
            panel.gameObject.SetActive(section != MenuSection.Squad || hasSkill);
        }

        private void RefreshDetails()
        {
            if (section == MenuSection.Squad)
                UpdateDetails(menuRoot.Find("SectionMainWindow/SquadContent/DetailsColumn/SkillDescriptionPanel"),
                    selectedSquadSkill >= 0 ? roster[selectedMercenary].equipped[selectedSquadSkill] : null);
            else if (section == MenuSection.Skills)
                UpdateDetails(menuRoot.Find("SectionMainWindow/SkillsContent/SkillDescriptionPanel"),
                    selectedSkillSlot >= 0 ? roster[selectedMercenary].equipped[selectedSkillSlot] : selectedReserveSkill);
        }

        private static Color PowerColor(string tier)
        {
            switch (tier)
            {
                case "Low": return new Color32(20, 160, 46, 255);
                case "Medium": return new Color32(249, 163, 27, 255);
                case "High": return new Color32(180, 32, 42, 255);
                default: return new Color32(115, 23, 45, 255);
            }
        }

        private TMP_Text Text(string path) => menuRoot.Find(path).GetComponent<TMP_Text>();
        private static TMP_Text Text(Transform parent, string child) => parent.Find(child).GetComponent<TMP_Text>();
        private Button ButtonAt(string path) => menuRoot.Find(path).GetComponent<Button>();
        private Image Image(string path) => menuRoot.Find(path).GetComponent<Image>();
    }
}
