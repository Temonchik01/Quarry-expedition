using System;
using System.Linq;
using BtlSys.UI;
using BtlSys.UIPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public static class MenuPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scene/MenuPrototype.unity";
    private const string UiAtlasPath = "Assets/Sprites/UI.png";
    private const string PortraitAtlasPath = "Assets/Sprites/Placeholders.png";
    private const string FontPath = "Assets/Pixel_Font-7 SDF.asset";
    private const float Width = 960f;
    private const float Height = 540f;
    private const float ScreenScale = 1.5f;
    private const float LayoutWidth = Width / ScreenScale;
    private const float LayoutHeight = Height / ScreenScale;

    private static Sprite[] uiSprites;
    private static TMP_FontAsset pixelFont;
    private static Sprite buttonSprite;
    private static Sprite inverseSprite;
    private static Sprite panelSprite;
    private static Sprite plusSprite;
    private static Sprite emptySkillSprite;
    private static Sprite statsSprite;

    [MenuItem("Tools/Quarry Expedition/Create Menu Prototype Scene")]
    public static void CreateMenuPrototypeScene()
    {
        LoadAssets();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateEventSystem();
        Canvas canvas = CreateCanvas();
        Image background = CreateImage(canvas.transform, "PrototypeBackground", null, Image.Type.Simple, false);
        SetRect(background.rectTransform, 0, 0, Width, Height);
        background.color = new Color32(8, 8, 12, 255);
        background.raycastTarget = false;

        RectTransform menuRoot = MakeRect(canvas.transform, "MenuRoot", 0, 0, LayoutWidth, LayoutHeight);
        menuRoot.localScale = Vector3.one * ScreenScale;
        CreatePanel(menuRoot, "ScreenFrame", 4, 4, LayoutWidth - 8, LayoutHeight - 8);
        BuildNavigation(menuRoot);
        GameObject sectionWindow = CreatePanel(menuRoot, "SectionMainWindow", 138, 6, 496, 348);
        BuildSquadScreen(sectionWindow.GetComponent<RectTransform>());
        BuildSkillsScreen(sectionWindow.GetComponent<RectTransform>());
        BuildPlaceholderScreen(sectionWindow.GetComponent<RectTransform>());
        sectionWindow.SetActive(false);

        MenuPrototypeController controller = menuRoot.gameObject.AddComponent<MenuPrototypeController>();
        Sprite[] portraits = LoadSprites(PortraitAtlasPath);
        SetControllerPortraits(controller, portraits);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log($"[MenuPrototype] Created {ScenePath} for a {Width}x{Height} UI reference.");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    }

    public static void ValidateMenuPrototype()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject menu = GameObject.Find("MenuRoot");
        Require(menu != null, "MenuRoot exists");
        MenuPrototypeController controller = menu.GetComponent<MenuPrototypeController>();
        Require(controller != null, "Menu prototype controller is attached");
        controller.InitializePrototype();

        Transform root = menu.transform;
        Transform navigation = root.Find("GlobalNavigation/NavigationItems");
        Transform upper = navigation.Find("UpperSectionButtons");
        Transform lower = navigation.Find("BottomSectionButtons");
        Transform fighters = navigation.Find("MercenaryList");
        GameObject window = root.Find("SectionMainWindow").gameObject;
        Require(!window.activeSelf && !fighters.gameObject.activeSelf && upper.childCount == 5,
            "Opening state shows only the five section buttons");
        Require(menu.GetComponentInParent<Canvas>().GetComponent<CanvasScaler>().referenceResolution == new Vector2(960, 540),
            "Menu uses the project 16:9 reference resolution");

        upper.Find("SquadButton").GetComponent<Button>().onClick.Invoke();
        Require(fighters.gameObject.activeSelf && upper.childCount == 1 && lower.childCount == 4,
            "Squad places fighters below its button and later sections below");
        Require(!window.activeSelf, "Squad waits for a fighter choice");
        fighters.Find("MechaButton").GetComponent<Button>().onClick.Invoke();
        Require(window.activeSelf && root.Find("SectionMainWindow/SquadContent").gameObject.activeSelf,
            "Choosing Mecha opens the Squad content");
        Transform health = root.Find("SectionMainWindow/SquadContent/DetailsColumn/StatsPanel/StatsRows/HEALTH");
        health.Find("UpgradeButton").GetComponent<Button>().onClick.Invoke();
        Require(health.Find("ValueText").GetComponent<TMP_Text>().text == "120" &&
                root.Find("SectionMainWindow/SquadContent/OverviewPanel/PointsText").GetComponent<TMP_Text>().text == "POINTS:9",
            "Stat upgrade updates the value and shared points");
        Transform squadSlots = root.Find("SectionMainWindow/SquadContent/OverviewPanel/EquippedSlots");
        squadSlots.Find("Slot0").GetComponent<Button>().onClick.Invoke();
        Require(root.Find("SectionMainWindow/SquadContent/DetailsColumn/SkillDescriptionPanel").gameObject.activeSelf,
            "Installed skill opens its description");
        squadSlots.Find("Slot2").GetComponent<Button>().onClick.Invoke();
        Require(!root.Find("SectionMainWindow/SquadContent/DetailsColumn/SkillDescriptionPanel").gameObject.activeSelf,
            "Empty Squad slot clears the description");

        lower.Find("SkillsButton").GetComponent<Button>().onClick.Invoke();
        Require(upper.childCount == 2 && lower.childCount == 3 && !window.activeSelf,
            "Skills keeps Squad above it and waits for a new fighter choice");
        fighters.Find("MechaButton").GetComponent<Button>().onClick.Invoke();
        Require(root.Find("SectionMainWindow/SkillsContent").gameObject.activeSelf, "Skills view opens for Mecha");
        Transform reserveSlots = root.Find("SectionMainWindow/SkillsContent/ReservePanel/ReserveSlots");
        Require(reserveSlots.Find("ReserveSlot8/Label").GetComponent<TMP_Text>().text == "↓",
            "Reserve shows the down arrow when more skills remain");
        reserveSlots.Find("ReserveSlot8").GetComponent<Button>().onClick.Invoke();
        Require(reserveSlots.Find("ReserveSlot0/Label").GetComponent<TMP_Text>().text == "↑",
            "Reserve moves by one and shows the up arrow");
        reserveSlots.Find("ReserveSlot0").GetComponent<Button>().onClick.Invoke();

        root.Find("SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/Install").GetComponent<Button>().onClick.Invoke();
        root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2").GetComponent<Button>().onClick.Invoke();
        reserveSlots.Find("ReserveSlot0").GetComponent<Button>().onClick.Invoke();
        GameObject confirmation = root.Find("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel").gameObject;
        Require(confirmation.activeSelf, "Install asks for confirmation");
        root.Find("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/YesButton").GetComponent<Button>().onClick.Invoke();
        string installed = root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2/Label").GetComponent<TMP_Text>().text;
        Require(installed.Contains("Grenade") && reserveSlots.Find("ReserveSlot0/Label").GetComponent<TMP_Text>().text.Contains("x9"),
            "Install uses one reserve copy and fills the chosen slot");

        root.Find("SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/Remove").GetComponent<Button>().onClick.Invoke();
        root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2").GetComponent<Button>().onClick.Invoke();
        Require(confirmation.activeSelf, "Remove asks for confirmation");
        root.Find("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/NoButton").GetComponent<Button>().onClick.Invoke();
        Require(root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2/Label")
            .GetComponent<TMP_Text>().text.Contains("Grenade"), "Cancel keeps the installed skill");

        root.Find("SectionMainWindow/SkillsContent/FighterColumn/InteractionModePanel/Recycle").GetComponent<Button>().onClick.Invoke();
        root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2").GetComponent<Button>().onClick.Invoke();
        reserveSlots.Find("ReserveSlot0").GetComponent<Button>().onClick.Invoke();
        Require(confirmation.transform.Find("RewardText").GetComponent<TMP_Text>().text.Contains("10 SCRAP"),
            "Reserve recycling shows the reward before confirmation");
        root.Find("SectionMainWindow/SkillsContent/FighterColumn/ConfirmationPanel/YesButton").GetComponent<Button>().onClick.Invoke();
        Require(reserveSlots.Find("ReserveSlot0/Label").GetComponent<TMP_Text>().text.Contains("x8") &&
                root.Find("SectionMainWindow/SkillsContent/EquippedSkillsPanel/EquippedSlots/EquippedSlot2/Label")
                    .GetComponent<TMP_Text>().text.Contains("Grenade") &&
                root.Find("SectionMainWindow/SquadContent/OverviewPanel/PointsText").GetComponent<TMP_Text>().text == "POINTS:9",
            "Recycling from Reserve uses one copy without changing equipped skills or points");

        Debug.Log("[MenuPrototype] Navigation, Squad upgrades, reserve scrolling, install, cancellation, and recycle checks passed.");
    }

    private static void Require(bool condition, string check)
    {
        if (!condition) throw new InvalidOperationException($"Menu prototype check failed: {check}");
    }

    private static void LoadAssets()
    {
        uiSprites = LoadSprites(UiAtlasPath);
        buttonSprite = FindSprite(uiSprites, "Button");
        inverseSprite = FindSprite(uiSprites, "ButtonInverse");
        panelSprite = FindSprite(uiSprites, "PanelUI");
        plusSprite = FindSprite(uiSprites, "PlusUpgrade");
        emptySkillSprite = FindSprite(uiSprites, "NewAttack");
        statsSprite = FindSprite(uiSprites, "StatsSlider");
        pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (buttonSprite == null || inverseSprite == null || panelSprite == null || pixelFont == null)
            throw new InvalidOperationException("Menu prototype could not find the UI atlas or Pixel_Font-7 SDF asset.");
    }

    private static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    }

    private static Sprite FindSprite(Sprite[] sprites, string spriteName)
    {
        return sprites.FirstOrDefault(sprite => sprite.name == spriteName);
    }

    private static void SetControllerPortraits(MenuPrototypeController controller, Sprite[] sprites)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty portraits = serialized.FindProperty("portraits");
        portraits.arraySize = 3;
        portraits.GetArrayElementAtIndex(0).objectReferenceValue = FindSprite(sprites, "Mecha");
        portraits.GetArrayElementAtIndex(1).objectReferenceValue = FindSprite(sprites, "Drilldozer");
        portraits.GetArrayElementAtIndex(2).objectReferenceValue = FindSprite(sprites, "Tank");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule input = eventSystem.GetComponent<InputSystemUIInputModule>();
        input.AssignDefaultActions();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("MenuPrototypeCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Width, Height);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        return canvas;
    }

    private static void BuildNavigation(RectTransform root)
    {
        GameObject panel = CreatePanel(root, "GlobalNavigation", 6, 6, 126, 348);
        RectTransform items = MakeRect(panel.transform, "NavigationItems", 5, 6, 116, 336);
        VerticalLayoutGroup layout = items.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(3, 3, 3, 3);
        layout.spacing = 2;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform upper = CreateNavigationGroup(items, "UpperSectionButtons");
        GameObject mercenaries = new GameObject("MercenaryList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(CanvasGroup));
        mercenaries.transform.SetParent(items, false);
        LayoutElement listLayout = mercenaries.GetComponent<LayoutElement>();
        listLayout.preferredHeight = 63;
        listLayout.minHeight = 63;
        VerticalLayoutGroup mercLayout = mercenaries.GetComponent<VerticalLayoutGroup>();
        mercLayout.spacing = 2;
        mercLayout.childControlWidth = true;
        mercLayout.childControlHeight = true;
        mercLayout.childForceExpandWidth = true;
        mercLayout.childForceExpandHeight = false;
        CreateLayoutButton(mercenaries.transform, "MechaButton", "Mecha", 19);
        CreateLayoutButton(mercenaries.transform, "DrilldozerButton", "Drilldozer", 19);
        CreateLayoutButton(mercenaries.transform, "TankButton", "Tank", 19);
        mercenaries.SetActive(false);

        GameObject spacer = new GameObject("FlexibleSpacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(items, false);
        spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;
        spacer.SetActive(false);
        RectTransform bottom = CreateNavigationGroup(items, "BottomSectionButtons");
        bottom.gameObject.SetActive(false);
        string[] names = { "SquadButton", "SkillsButton", "DatabaseButton", "SettingsButton", "TitleButton" };
        string[] labels = { "SQUAD", "SKILLS", "DATABASE", "SETTINGS", "TITLE" };
        Button[] sections = new Button[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            CreateLayoutButton(upper, names[i], labels[i], 22);
            sections[i] = upper.Find(names[i]).GetComponent<Button>();
        }
        Button[] fighters = new Button[3];
        for (int i = 0; i < fighters.Length; i++)
        {
            fighters[i] = mercenaries.transform.GetChild(i).GetComponent<Button>();
            fighters[i].GetComponent<ButtonInverseTextUI>().SetHideBackgroundWhenIdle(true);
        }
        upper.GetComponent<LayoutElement>().preferredHeight = names.Length * 22 + (names.Length - 1) * 2;
        MenuPrototypeNavigationUI navigation = panel.AddComponent<MenuPrototypeNavigationUI>();
        navigation.Configure(items, upper, bottom, mercenaries.GetComponent<RectTransform>(), spacer, sections, fighters);
    }

    private static RectTransform CreateNavigationGroup(Transform parent, string name)
    {
        RectTransform group = MakeRect(parent, name, 0, 0, 106, 0);
        VerticalLayoutGroup layout = group.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        group.gameObject.AddComponent<LayoutElement>();
        return group;
    }

    private static void BuildSquadScreen(RectTransform root)
    {
        GameObject squad = new GameObject("SquadContent", typeof(RectTransform));
        squad.transform.SetParent(root, false);
        SetRect(squad.GetComponent<RectTransform>(), 4, 4, 488, 340);

        GameObject overview = CreatePanel(squad.transform, "OverviewPanel", 0, 0, 138, 340);
        TMP_Text nameText = CreateText(overview.transform, "NameText", "MECHA", 6, 317, 126, 16, 12, TextAlignmentOptions.Center);
        nameText.gameObject.SetActive(false);
        GameObject portraitFrame = CreatePanel(overview.transform, "PortraitFrame", 4, 212, 130, 120);
        Image portrait = CreateImage(portraitFrame.transform, "Portrait", FindPortrait("Mecha"), Image.Type.Simple, true);
        SetRect(portrait.rectTransform, 4, 4, 122, 112);
        CreateText(overview.transform, "SpecialsHeader", "SPECIALS:", 4, 188, 130, 18, 12, TextAlignmentOptions.Center);
        RectTransform equipped = MakeRect(overview.transform, "EquippedSlots", 4, 26, 130, 158);
        for (int i = 0; i < 8; i++)
        {
            Button slot = CreateSlotButton(equipped, $"Slot{i}", 0, 135 - i * 19, 110, 17);
            CreateUpgradeIcon(slot.transform, "PlusUpgrade", 113, 0, 16, 17);
            if (i < 2) SetButtonLabel(slot, i == 0 ? "Drill +" : "Tackle");
        }
        CreatePanel(overview.transform, "PointsFrame", 4, 4, 130, 20);
        CreateText(overview.transform, "PointsText", "POINTS:10", 4, 4, 130, 20, 11, TextAlignmentOptions.Center);

        GameObject details = new GameObject("DetailsColumn", typeof(RectTransform));
        details.transform.SetParent(squad.transform, false);
        SetRect(details.GetComponent<RectTransform>(), 144, 0, 344, 340);
        GameObject statsPanel = CreatePanel(details.transform, "StatsPanel", 0, 151, 344, 189);
        CreateText(statsPanel.transform, "Header", "CHARACTER STATS", 8, 165, 328, 16, 13, TextAlignmentOptions.Center);
        RectTransform rows = MakeRect(statsPanel.transform, "StatsRows", 8, 8, 328, 148);
        string[] labels = { "HEALTH", "ATTACK", "DEFENCE", "AMMO", "SPEED" };
        for (int i = 0; i < labels.Length; i++)
            BuildStatRow(rows, labels[i], 0, 119 - i * 29, 328, 28);

        BuildDescriptionPanel(details.transform, "SkillDescriptionPanel", 0, 0, 344, 145);
    }

    private static void BuildStatRow(Transform parent, string statName, float x, float y, float width, float height)
    {
        RectTransform row = MakeRect(parent, statName, x, y, width, height);
        Image hitbox = row.gameObject.AddComponent<Image>();
        hitbox.pixelsPerUnitMultiplier = 0.4f;
        hitbox.color = new Color(1, 1, 1, 0);
        hitbox.raycastTarget = false;
        CreateText(row, "Label", statName, 0, 15, 180, 12, 10, TextAlignmentOptions.Left);
        CreateText(row, "ValueText", "100", width - 42, 15, 42, 12, 10, TextAlignmentOptions.Right);

        float barWidth = width - 104;
        Image barBack = CreateImage(row, "Bar", null, Image.Type.Simple, false);
        SetRect(barBack.rectTransform, 0, 3, barWidth, 8);
        barBack.color = new Color32(58, 28, 37, 255);
        Image fill = CreateImage(barBack.transform, "Fill", statsSprite, Image.Type.Filled, false);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.5f;
        fill.color = new Color32(223, 62, 35, 255);
        SetRect(fill.rectTransform, 0, 0, barWidth, 8);

        GameObject upgrade = new GameObject("UpgradeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        upgrade.transform.SetParent(row, false);
        SetRect(upgrade.GetComponent<RectTransform>(), barWidth + 3, 0, 16, 16);
        Image upgradeImage = upgrade.GetComponent<Image>();
        upgradeImage.sprite = plusSprite;
        upgradeImage.pixelsPerUnitMultiplier = 0.4f;
        upgradeImage.raycastTarget = true;
        Button upgradeButton = upgrade.GetComponent<Button>();
        upgradeButton.targetGraphic = upgradeImage;
        upgradeButton.transition = Selectable.Transition.None;
        TMP_Text preview = CreateText(row, "PreviewText", "+20", barWidth + 21, 0, 34, 14, 10, TextAlignmentOptions.Left);
        preview.color = new Color32(249, 163, 27, 255);
        StatUpgradeIndicatorUI indicator = upgrade.AddComponent<StatUpgradeIndicatorUI>();
        indicator.Configure(upgradeImage, preview, plusSprite, FindSprite(uiSprites, "PlusUpgradeInverse"));
    }

    private static void BuildSkillsScreen(RectTransform root)
    {
        GameObject skills = new GameObject("SkillsContent", typeof(RectTransform));
        skills.transform.SetParent(root, false);
        SetRect(skills.GetComponent<RectTransform>(), 4, 4, 488, 340);

        GameObject fighter = new GameObject("FighterColumn", typeof(RectTransform));
        fighter.transform.SetParent(skills.transform, false);
        SetRect(fighter.GetComponent<RectTransform>(), 0, 0, 140, 340);
        GameObject portraitFrame = CreatePanel(fighter.transform, "PortraitFrame", 5, 216, 130, 120);
        Image portrait = CreateImage(portraitFrame.transform, "Portrait", FindPortrait("Mecha"), Image.Type.Simple, true);
        SetRect(portrait.rectTransform, 4, 4, 122, 112);
        CreateText(fighter.transform, "NameText", "MECHA", 5, 211, 130, 16, 13, TextAlignmentOptions.Center);

        GameObject modePanel = CreatePanel(fighter.transform, "InteractionModePanel", 5, 79, 130, 126);
        string[] modes = { "Install", "Replace", "Remove", "Recycle", "Inspect" };
        Color[] modeColors =
        {
            new Color32(249, 196, 47, 255), new Color32(24, 160, 67, 255),
            new Color32(223, 62, 35, 255), new Color32(180, 32, 42, 255), new Color32(14, 190, 180, 255)
        };
        for (int i = 0; i < modes.Length; i++)
        {
            CreateButton(modePanel.transform, modes[i], modes[i].ToUpperInvariant(), 5, 97 - i * 21, 120, 19, modeColors[i]);
        }
        CreateText(fighter.transform, "StatusText", "Choose a mode, then a skill.", 5, 61, 130, 16, 8, TextAlignmentOptions.Center);
        CreateText(fighter.transform, "ScrapText", "SCRAP:0", 5, 47, 130, 14, 10, TextAlignmentOptions.Center).color = new Color32(249, 163, 27, 255);

        GameObject confirmation = CreatePanel(fighter.transform, "ConfirmationPanel", 5, 4, 130, 70);
        confirmation.SetActive(false);
        CreateText(confirmation.transform, "MessageText", "", 4, 40, 122, 27, 9, TextAlignmentOptions.Left);
        CreateText(confirmation.transform, "RewardText", "", 4, 25, 122, 13, 9, TextAlignmentOptions.Left);
        CreateButton(confirmation.transform, "YesButton", "YES", 4, 4, 57, 17);
        CreateButton(confirmation.transform, "NoButton", "NO", 69, 4, 57, 17, new Color32(120, 27, 45, 255));

        BuildDescriptionPanel(skills.transform, "SkillDescriptionPanel", 146, 202, 338, 134);
        GameObject reserve = CreatePanel(skills.transform, "ReservePanel", 146, 4, 162, 190);
        CreateText(reserve.transform, "Header", "RESERVE:", 5, 170, 152, 16, 12, TextAlignmentOptions.Center);
        RectTransform reserveSlots = MakeRect(reserve.transform, "ReserveSlots", 5, 4, 152, 163);
        for (int i = 0; i < 9; i++)
            CreateSlotButton(reserveSlots, $"ReserveSlot{i}", 0, 145 - i * 18, 152, 17, false);

        GameObject equipped = CreatePanel(skills.transform, "EquippedSkillsPanel", 316, 4, 168, 190);
        CreateText(equipped.transform, "Header", "SPECIALS:", 5, 170, 158, 16, 12, TextAlignmentOptions.Center);
        RectTransform equippedSlots = MakeRect(equipped.transform, "EquippedSlots", 5, 4, 158, 163);
        for (int i = 0; i < 8; i++)
            CreateSlotButton(equippedSlots, $"EquippedSlot{i}", 0, 145 - i * 19, 158, 17);

        skills.SetActive(false);
    }

    private static GameObject BuildDescriptionPanel(Transform parent, string name, float x, float y, float width, float height)
    {
        GameObject panel = CreatePanel(parent, name, x, y, width, height);
        CreateText(panel.transform, "TargetText", "1 Enemy", 8, height - 27, 130, 16, 11, TextAlignmentOptions.Left);
        CreateText(panel.transform, "PowerText", "Power: Low", 8, height - 45, 155, 16, 11, TextAlignmentOptions.Left).color = new Color32(20, 160, 46, 255);
        CreateText(panel.transform, "AccuracyText", "Accuracy: 100", width - 150, height - 27, 142, 16, 11, TextAlignmentOptions.Right);
        CreateText(panel.transform, "CostText", "Cost:20 AM", width - 150, height - 45, 142, 16, 11, TextAlignmentOptions.Right);
        CreateText(panel.transform, "DescriptionText", "Attempts three hits on one enemy.", 8, 56, width - 16, Mathf.Max(30, height - 100), 11, TextAlignmentOptions.TopLeft);
        CreateText(panel.transform, "EffectText", "+ Hits every enemy once.", 8, 37, width - 16, 16, 10, TextAlignmentOptions.Left).color = new Color32(37, 214, 90, 255);
        CreateText(panel.transform, "NextUpgradeText", "Next upgrade:\n<color=#14A02E>+ Ignite enemy</color>", 8, 5, width - 16, 30, 9, TextAlignmentOptions.BottomLeft);
        return panel;
    }

    private static void BuildPlaceholderScreen(RectTransform root)
    {
        GameObject placeholder = CreatePanel(root, "PlaceholderContent", 4, 4, 488, 340);
        CreateText(placeholder.transform, "Heading", "DATABASE", 10, 270, 468, 30, 20, TextAlignmentOptions.Center);
        CreateText(placeholder.transform, "Info", "This prototype focuses on Squad and Skills.", 10, 230, 468, 24, 13, TextAlignmentOptions.Center);
        placeholder.SetActive(false);
    }

    private static void CreateLayoutButton(Transform parent, string name, string label, float height)
    {
        Button button = CreateButton(parent, name, label, 0, 0, 82, height);
        LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private static Button CreateUpgradeIcon(Transform parent, string name, float x, float y, float width, float height)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        SetRect(obj.GetComponent<RectTransform>(), x, y, width, height);
        Image image = obj.GetComponent<Image>();
        image.sprite = plusSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.pixelsPerUnitMultiplier = 0.4f;
        image.raycastTarget = true;
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.highlightedSprite = FindSprite(uiSprites, "PlusUpgradeInverse");
        state.selectedSprite = state.highlightedSprite;
        state.pressedSprite = state.highlightedSprite;
        button.spriteState = state;
        return button;
    }

    private static Button CreateSlotButton(Transform parent, string name, float x, float y, float width, float height, bool showEmptyIcon = true)
    {
        Button button = CreateButton(parent, name, string.Empty, x, y, width, height);
        if (showEmptyIcon && emptySkillSprite != null)
        {
            Image plus = CreateImage(button.transform, "NewAttack", emptySkillSprite, Image.Type.Simple, true);
            SetRect(plus.rectTransform, width * 0.5f - 6, height * 0.5f - 6, 12, 12);
        }
        return button;
    }

    private static Button CreateButton(Transform parent, string name, string label, float x, float y, float width, float height, Color? tint = null)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonInverseTextUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        SetRect(rect, x, y, width, height);
        Image image = obj.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.4f;
        image.color = tint ?? Color.white;
        image.raycastTarget = true;
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = button.spriteState;
        spriteState.highlightedSprite = inverseSprite;
        spriteState.selectedSprite = inverseSprite;
        spriteState.pressedSprite = inverseSprite;
        spriteState.disabledSprite = buttonSprite;
        button.spriteState = spriteState;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        button.navigation = navigation;
        CreateText(obj.transform, "Label", label, 3, 0, width - 6, height, 12, TextAlignmentOptions.Center);
        return button;
    }

    private static GameObject CreatePanel(Transform parent, string name, float x, float y, float width, float height)
    {
        Image image = CreateImage(parent, name, panelSprite, Image.Type.Sliced, false);
        SetRect(image.rectTransform, x, y, width, height);
        image.color = Color.white;
        image.raycastTarget = false;
        return image.gameObject;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Image.Type type, bool preserveAspect)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = preserveAspect;
        image.pixelsPerUnitMultiplier = 0.4f;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float x, float y, float width,
        float height, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        SetRect(rect, x, y, width, height);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.font = pixelFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = new Color32(227, 230, 255, 255);
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform MakeRect(Transform parent, string name, float x, float y, float width, float height)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        SetRect(rect, x, y, width, height);
        return rect;
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        rect.localScale = Vector3.one;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        button.GetComponentInChildren<TMP_Text>().text = value;
    }

    private static Sprite FindPortrait(string name)
    {
        Sprite[] sprites = LoadSprites(PortraitAtlasPath);
        return FindSprite(sprites, name);
    }
}
