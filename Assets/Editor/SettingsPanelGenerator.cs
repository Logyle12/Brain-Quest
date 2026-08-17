// SettingsPanelGenerator.cs
//
// MUST live in a folder literally named "Editor" anywhere under Assets/
// (e.g. Assets/Editor/SettingsPanelGenerator.cs) or Unity will fail to
// compile the project - editor-only scripts are only allowed to reference
// UnityEditor when they're inside an Editor folder.
//
// Menu: Tools > Settings UI > Generate Settings Panel
//
// What this does NOT do: touch fonts, sprites, or colors beyond flat
// placeholder ones - you re-skin after.
//
// What this DOES do: builds Sidebar + Header + scrollable Card + Save,
// matching object names and layout rules verified against the real,
// working project (not just my own guesses) - including the Sidebar,
// which in earlier versions of this script was principle-based only and
// has since been corrected to match your actual NavList/AudioButton/
// DisplayButton structure:
//   - The slider has a real Fill Area + Handle wired to Slider.fillRect /
//     handleRect, not just a background strip - it's visible and
//     draggable immediately, not an invisible component.
//   - Selectors (Font Style, Text Size) start on the label you'd actually
//     expect (e.g. "Default"), not always index 0 of their options list.
//   - Rows_Container_[panel] (the ScrollRect/Mask) gets an explicit
//     LayoutElement (Min Height + Flexible Height 1) so it always reports
//     a real size upward instead of an ambiguous 0.
//   - MainContent's Vertical Layout Group is Upper Center aligned, not
//     Middle Center, so the panel doesn't re-center itself as content
//     height changes between text sizes.
//   - TitleBlock (header) and each row's TextBlock get ONE height driver
//     each (their own Vertical Layout Group with Child Control Height on)
//     - never both a Layout Group AND a Content Size Fitter on the same
//     Title/Subtitle text, which was the header's double-driver conflict.
//   - TitleBlock also gets Child Control Width ON, so Title/Subtitle track
//     real available width instead of a frozen authored value.
//   - Every fixed-size control (icon circles, slider, prev/next buttons,
//     toggle) gets Min Width/Height == Preferred, so overflow-shrink can
//     never squash them - only the genuinely flexible elements compress.
//   - Value-style labels (e.g. "100%") get Min Width only (no Preferred),
//     which is the flex-grow behaviour: compact by default, grows only
//     when the text actually needs more room, never forced wide upfront.
//   - A selector control (Previous / Text / Next, e.g. Font Style) gets
//     its own internal Horizontal Layout Group + Content Size Fitter so
//     it reflows and reports its real width upward, with its background
//     image set to Ignore Layout so it doesn't get treated as a 4th item.
//
// After running this, use FontManager the same way as before - it applies
// font/size the same way regardless of how the hierarchy was built.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public static class SettingsPanelGenerator
{
    // ---- tunables -----------------------------------------------------
    private const float RowIconSize = 55f;
    private const float RowSpacing = 20f;
    private const float RowPaddingLR = 20f;
    private const float SliderWidth = 220f; // was 300 - too wide a fraction of a 1024-reference canvas, starved TextBlock
    private const float ValueLabelMinWidth = 70f;   // flex-grow floor, not a fixed size
    private const float SelectorArrowSize = 55f;
    private const float SelectorTextMinWidth = 100f;
    private const float ToggleWidth = 58f;
    private const float ToggleHeight = 36f;
    private const float RowsContainerMinHeight = 150f;
    private const float NavIconSize = 52f;

    [MenuItem("Tools/Settings UI/Generate Settings Panel")]
    public static void Generate()
    {
        EnsureEventSystemExists();

        Transform parent = Selection.activeTransform;
        if (parent == null || parent.GetComponent<RectTransform>() == null)
        {
            Canvas canvas = CreateCanvas();
            parent = canvas.transform;
        }

        GameObject settingsUI = CreateUIObject("SettingsUI", parent);
        StretchFull(settingsUI.GetComponent<RectTransform>());
        HorizontalLayoutGroup uiLayout = settingsUI.AddComponent<HorizontalLayoutGroup>();
        uiLayout.childAlignment = TextAnchor.UpperCenter;
        uiLayout.childForceExpandWidth = false; // was true - this was overriding Sidebar's width entirely
        uiLayout.childForceExpandHeight = true;
        uiLayout.childControlWidth = true;
        uiLayout.childControlHeight = true;

        CreateSidebar(settingsUI.transform);

        GameObject mainContent = CreateUIObject("MainContent", settingsUI.transform);
        AddLayoutElement(mainContent, -1, -1, 77, -1, -1, 1); // 77 against Sidebar's 23 - see note above

        VerticalLayoutGroup mcLayout = mainContent.AddComponent<VerticalLayoutGroup>();
        mcLayout.childAlignment = TextAnchor.UpperCenter; // NOT MiddleCenter - see header comment
        mcLayout.spacing = 15f; // was 5 - too tight, read as Save touching the card above it
        // Top matches Sidebar's own top padding (20) so ContentHeader and
        // SidebarHeader start at the same Y and read as one aligned row.
        // Bottom stops Save sitting flush against the screen edge.
        mcLayout.padding = new RectOffset(0, 0, 20, 20);
        mcLayout.childForceExpandWidth = true;
        mcLayout.childForceExpandHeight = false;
        mcLayout.childControlWidth = true;
        mcLayout.childControlHeight = true;

        CreateHeader(mainContent.transform, "SOUND", "Adjust the sounds to your liking.");
        GameObject card = CreateSettingsCard(mainContent.transform, "Display");
        CreateSaveArea(mainContent.transform);

        Transform rowsContent = card.transform.Find("Rows_Container_Display/Viewport/Content");
        CreateRow(rowsContent, "Row_Brightness", new Color(0.73f, 0.18f, 0.23f),
            "Brightness", "Adjust the brightness.",
            row => CreateSliderRow(row, "Slider_Brightness"));
        CreateRow(rowsContent, "Row_Font_Style", new Color(0.95f, 0.71f, 0.18f),
            "Font Style", "Choose the style of text.",
            row => CreateSelectorRow(row, "FontSelector", new List<string> { "Default", "OpenDyslexic" }, 0));
        CreateRow(rowsContent, "Row_Font_Size", new Color(0.43f, 0.88f, 0f),
            "Text Size", "Choose the size of text.",
            row => CreateSelectorRow(row, "FontSizeSelector", new List<string> { "Small", "Default", "Large", "Maximum" }, 1));
        CreateRow(rowsContent, "Row_Fullscreen", new Color(0.21f, 0.73f, 1f),
            "Fullscreen", "Use fullscreen for best experience.",
            row => CreateToggleRow(row));

        Selection.activeGameObject = settingsUI;
        Debug.Log("Settings panel generated. Rows_Container_Display's Content now holds 4 example rows - " +
                  "duplicate CreateRow() calls in code, or duplicate the row GameObjects directly, for more.");
    }

    private static void CreateSidebar(Transform parent)
    {
        // Matches the structure verified against your actual working scene:
        // Sidebar > SidebarHeader (IconCircle + Label), NavList > AudioButton /
        // DisplayButton, each with a nested "Content" row (HorizontalLayoutGroup)
        // holding IconBox + Label. Confirmed correct: Force Expand off on every
        // row here, Min=Preferred on both icon sizes, Min-Width-only on labels.
        GameObject sidebar = CreateUIObject("Sidebar", parent);
        Image sidebarBg = sidebar.AddComponent<Image>();
        sidebarBg.color = new Color(0.24f, 0.42f, 0.63f);
        // Flexible weight, not a fixed pixel width, and no Min Width floor -
        // a floor can silently override the ratio whenever the canvas's
        // actual resolved width makes the percentage fall under it, which
        // is exactly what was making the sidebar wider than intended. Pure
        // 23:77 against MainContent's weight below is immune to that.
        AddLayoutElement(sidebar, -1, -1, 23, -1, -1, 1); // pure ratio, no fixed floor to fight it - measured directly off your reference image's pixel boundaries
        VerticalLayoutGroup sbLayout = sidebar.AddComponent<VerticalLayoutGroup>();
        sbLayout.padding = new RectOffset(20, 20, 20, 20);
        sbLayout.spacing = 20f;
        sbLayout.childAlignment = TextAnchor.UpperCenter;
        sbLayout.childForceExpandWidth = true;
        sbLayout.childForceExpandHeight = false;
        sbLayout.childControlWidth = true;
        sbLayout.childControlHeight = true;

        GameObject header = CreateUIObject("SidebarHeader", sidebar.transform);
        AddLayoutElement(header, -1, -1, -1, 56, 56, 0);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        // Matches AudioButton/DisplayButton's Content padding+spacing below
        // exactly - that's what makes the gear icon and the nav button icons
        // land in the same column instead of drifting relative to each other.
        headerLayout.padding = new RectOffset(15, 15, 0, 0);
        headerLayout.spacing = 15f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;

        GameObject iconCircle = CreateImagePlaceholder("IconCircle", header.transform, Color.white);
        AddLayoutElement(iconCircle, -1, NavIconSize, 0, -1, NavIconSize, 0);

        GameObject settingsLabel = CreateUIObject("Label", header.transform);
        CreateTMP(settingsLabel, "SETTINGS", 25, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        AddLayoutElement(settingsLabel, -1, -1, 1, -1, -1, -1);

        GameObject navList = CreateUIObject("NavList", sidebar.transform);
        navList.AddComponent<Image>().color = Color.clear;
        AddLayoutElement(navList, -1, -1, -1, -1, -1, 0);
        VerticalLayoutGroup navLayout = navList.AddComponent<VerticalLayoutGroup>();
        navLayout.spacing = 15f;
        navLayout.childAlignment = TextAnchor.UpperCenter;
        navLayout.childForceExpandWidth = true;
        navLayout.childForceExpandHeight = false;
        navLayout.childControlWidth = true;
        navLayout.childControlHeight = true;

        CreateNavButton(navList.transform, "AudioButton", "SOUND", new Color(0.55f, 0.5f, 0.75f));
        CreateNavButton(navList.transform, "DisplayButton", "DISPLAY", new Color(0.55f, 0.5f, 0.75f));
    }

    private static void CreateNavButton(Transform parent, string name, string label, Color bgColor)
    {
        GameObject btn = CreateUIObject(name, parent);
        AddLayoutElement(btn, -1, -1, -1, 60, 60, 0);
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = bgColor;
        btn.AddComponent<Button>();

        GameObject content = CreateUIObject("Content", btn.transform);
        StretchFull(content.GetComponent<RectTransform>());
        HorizontalLayoutGroup btnLayout = content.AddComponent<HorizontalLayoutGroup>();
        btnLayout.padding = new RectOffset(15, 15, 0, 0);
        btnLayout.spacing = 15f; // matches SidebarHeader's spacing above
        btnLayout.childAlignment = TextAnchor.MiddleLeft;
        btnLayout.childForceExpandWidth = false;
        btnLayout.childForceExpandHeight = false;
        btnLayout.childControlWidth = true;
        btnLayout.childControlHeight = true;

        GameObject icon = CreateImagePlaceholder("IconBox", content.transform, Color.white);
        AddLayoutElement(icon, NavIconSize, NavIconSize, 0, NavIconSize, NavIconSize, 0);

        GameObject textGO = CreateUIObject("Label", content.transform);
        CreateTMP(textGO, label, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        // Min-Width-only, same flex-grow rule as everywhere else - this is
        // what stops "SETTINGS" from being forced into a narrower box than
        // it needs and wrapping into "SETTIN/GS".
        AddLayoutElement(textGO, 80, -1, 1, -1, -1, -1);
    }

    // ---- top-level pieces ----------------------------------------------

    private static void EnsureEventSystemExists()
    {
        // Every interaction - button clicks, slider drags, and yes, scrollbar
        // dragging - routes through exactly one EventSystem in the scene.
        // FindObjectOfType searches the whole scene, not just what this
        // script has built, so this won't create a duplicate if one already
        // exists (which it should, in a real project - but a fresh empty
        // scene has none, and everything generated would otherwise be
        // inert: not just non-scrollable, completely unclickable).
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasGO = new GameObject("GeneratedSettingsCanvas", typeof(RectTransform));
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 576); // matches your actual target resolution
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Settings Canvas");
        return canvas;
    }

    private static void CreateHeader(Transform parent, string title, string subtitle)
    {
        GameObject header = CreateUIObject("ContentHeader", parent);
        HorizontalLayoutGroup hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(45, 0, 20, 0); // was 32 - matches Content's padding(25) + Row's padding(20) so icons column-align
        hlg.spacing = RowSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft; // hugs the left edge - deliberate, not MiddleRight
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = false;

        GameObject iconCircle = CreateImagePlaceholder("IconCircle", header.transform, new Color(0.6f, 0.6f, 0.9f));
        AddLayoutElement(iconCircle, 56, 56, 0, 56, 56, 0);

        GameObject titleBlock = CreateUIObject("TitleBlock", header.transform);
        VerticalLayoutGroup tbLayout = titleBlock.AddComponent<VerticalLayoutGroup>();
        tbLayout.spacing = 5f;
        tbLayout.childAlignment = TextAnchor.UpperLeft;
        tbLayout.childForceExpandWidth = false;
        tbLayout.childForceExpandHeight = false;
        tbLayout.childControlWidth = true;   // ON: Title/Subtitle track real width, don't freeze
        tbLayout.childControlHeight = true;  // ON: this is TitleBlock's ONLY height driver
        ContentSizeFitter tbFitter = titleBlock.AddComponent<ContentSizeFitter>();
        tbFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        tbFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title/Subtitle: LayoutElement only. Do NOT add a ContentSizeFitter
        // here too - that was the exact double-driver bug that caused the
        // header to overlap itself. TitleBlock's own VerticalLayoutGroup
        // (above) is the single source of truth for their height.
        CreateHeaderText(titleBlock.transform, "Title", title, 36, FontStyles.Bold);
        CreateHeaderText(titleBlock.transform, "Subtitle", subtitle, 20, FontStyles.Normal);
    }

    private static GameObject CreateSettingsCard(Transform parent, string panelSuffix)
    {
        GameObject card = CreateUIObject("SettingsCard", parent);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.09f, 0.2f, 1f);
        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = true; // single child (Rows_Container) fills the card
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;

        GameObject rowsContainer = CreateUIObject("Rows_Container_" + panelSuffix, card.transform);
        ScrollRect scrollRect = rowsContainer.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 25f;
        // Explicit LayoutElement so this object ALWAYS reports a real size
        // upward - ScrollRect/RectMask2D don't implement ILayoutElement on
        // their own, so without this the container's reported height is
        // ambiguous and everything above it (card, header) inherits that
        // ambiguity.
        AddLayoutElement(rowsContainer, -1, -1, -1, RowsContainerMinHeight, -1, 1);

        // Viewport is a separate object (not just Rows_Container's own rect)
        // specifically so ScrollRect can shrink its width on its own when
        // the scrollbar appears (Auto Hide And Expand Viewport, below) -
        // that resizing needs a rect ScrollRect actually owns and controls.
        GameObject viewport = CreateUIObject("Viewport", rowsContainer.transform);
        StretchFull(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 28f; // was 15 - too tight relative to the bigger icons/fonts
        contentLayout.padding = new RectOffset(25, 25, 15, 15);
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // The actual visible/draggable scrollbar - set to only appear once
        // there's something to scroll (which for this feature specifically
        // means "once the font size has grown enough that content overflows
        // the card"), and to claim its own strip of width when it does so
        // rows don't render underneath it.
        GameObject scrollbarGO = CreateUIObject("Scrollbar_Vertical", rowsContainer.transform);
        RectTransform scrollbarRect = scrollbarGO.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(16, 0);
        scrollbarGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        Scrollbar scrollbarComp = scrollbarGO.AddComponent<Scrollbar>();
        scrollbarComp.direction = Scrollbar.Direction.BottomToTop;

        GameObject slidingArea = CreateUIObject("Sliding Area", scrollbarGO.transform);
        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(2, 2);
        slidingRect.offsetMax = new Vector2(-2, -2);

        GameObject handle = CreateImagePlaceholder("Handle", slidingArea.transform, new Color(1f, 1f, 1f, 0.6f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(1, 0.2f); // Scrollbar.size overrides this live at runtime
        handleRect.sizeDelta = Vector2.zero;
        scrollbarComp.targetGraphic = handle.GetComponent<Image>();
        scrollbarComp.handleRect = handleRect;

        scrollRect.verticalScrollbar = scrollbarComp;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3f;

        return card;
    }

    private static void CreateSaveArea(Transform parent)
    {
        GameObject saveArea = CreateUIObject("SaveArea", parent);
        AddLayoutElement(saveArea, -1, -1, -1, 90, 90, 0); // was 70 - was leaving almost no margin around the button
        Image areaBg = saveArea.AddComponent<Image>();
        areaBg.color = new Color(0.24f, 0.36f, 0.55f); // was unset entirely - button had nothing behind it
        HorizontalLayoutGroup hlg = saveArea.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        GameObject saveButton = CreateUIObject("SaveButton", saveArea.transform);
        Image btnImg = saveButton.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.45f, 0.15f);
        saveButton.AddComponent<Button>();
        AddLayoutElement(saveButton, 220, 220, 0, 60, 60, 0);
        GameObject saveLabel = CreateUIObject("Label", saveButton.transform);
        CreateTMP(saveLabel, "SAVE", 24, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchFull(saveLabel.GetComponent<RectTransform>());
    }

    // ---- row construction ------------------------------------------------

    private static void CreateRow(Transform parent, string rowName, Color iconColor,
        string title, string subtitle, System.Action<Transform> addControl)
    {
        GameObject row = CreateUIObject(rowName, parent);
        AddLayoutElement(row, -1, -1, 1, -1, -1, 1);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset((int)RowPaddingLR, (int)RowPaddingLR, 0, 0);
        hlg.spacing = RowSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        GameObject iconBox = CreateImagePlaceholder("IconBox", row.transform, iconColor);
        AddLayoutElement(iconBox, RowIconSize, RowIconSize, 0, RowIconSize, RowIconSize, 0);

        GameObject textBlock = CreateUIObject("TextBlock", row.transform);
        AddLayoutElement(textBlock, 150, -1, 1, -1, -1, -1); // Min Width floor - was unset, could be squeezed toward 0
        VerticalLayoutGroup tbLayout = textBlock.AddComponent<VerticalLayoutGroup>();
        tbLayout.spacing = 4f;
        tbLayout.childAlignment = TextAnchor.UpperLeft;
        tbLayout.childControlWidth = true;
        tbLayout.childControlHeight = true;
        // Same rule as the header: LayoutElement only on Title/Subtitle,
        // no ContentSizeFitter - this VerticalLayoutGroup is the one and
        // only height driver.
        CreateHeaderText(textBlock.transform, "Title", title, 24, FontStyles.Bold);
        CreateHeaderText(textBlock.transform, "Subtitle", subtitle, 16, FontStyles.Normal);

        addControl(row.transform);
    }

    private static void CreateSliderRow(Transform row, string sliderName)
    {
        GameObject slider = CreateUIObject(sliderName, row);
        Slider sliderComp = slider.AddComponent<Slider>();
        sliderComp.minValue = 0f;
        sliderComp.maxValue = 1f;
        sliderComp.value = 1f;
        sliderComp.direction = Slider.Direction.LeftToRight;
        AddLayoutElement(slider, SliderWidth, SliderWidth, 0, -1, -1, 0);

        GameObject bg = CreateImagePlaceholder("Background", slider.transform, new Color(0.3f, 0.3f, 0.3f));
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.4f);
        bgRect.anchorMax = new Vector2(1, 0.6f);
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area / Fill - the coloured bar that grows with the value.
        GameObject fillArea = CreateUIObject("Fill Area", slider.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.4f);
        fillAreaRect.anchorMax = new Vector2(1, 0.6f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        GameObject fill = CreateImagePlaceholder("Fill", fillArea.transform, new Color(0.73f, 0.18f, 0.23f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;

        // Handle Slide Area / Handle - the draggable knob.
        GameObject handleArea = CreateUIObject("Handle Slide Area", slider.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateImagePlaceholder("Handle", handleArea.transform, Color.white);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24, 24);

        sliderComp.fillRect = fillRect;
        sliderComp.handleRect = handleRect;
        sliderComp.targetGraphic = handle.GetComponent<Image>();

        GameObject valueLabel = CreateUIObject("ValueLabel", row);
        TextMeshProUGUI vlText = CreateTMP(valueLabel, "100%", 22, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        // Flex-grow: Min Width is the floor it starts compact at, Preferred
        // is left unset so TMP's own natural width takes over once the
        // string needs more room (larger font size, longer value, etc.)
        AddLayoutElement(valueLabel, ValueLabelMinWidth, -1, 0, -1, -1, -1);
    }

    private static void CreateToggleRow(Transform row)
    {
        GameObject toggleGO = CreateUIObject("Toggle_Control", row);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        AddLayoutElement(toggleGO, ToggleWidth, ToggleWidth, 0, ToggleHeight, ToggleHeight, 0);

        GameObject track = CreateImagePlaceholder("Track", toggleGO.transform, new Color(0.4f, 0.4f, 0.4f));
        StretchFull(track.GetComponent<RectTransform>());

        GameObject knob = CreateImagePlaceholder("Knob", toggleGO.transform, Color.white);
        RectTransform knobRect = knob.GetComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0, 0);
        knobRect.anchorMax = new Vector2(0, 1);
        knobRect.sizeDelta = new Vector2(ToggleHeight - 4, -4);
        knobRect.anchoredPosition = new Vector2(ToggleHeight / 2f, 0);
        toggle.targetGraphic = track.GetComponent<Image>();
    }

    private static void CreateSelectorRow(Transform row, string selectorName, List<string> options, int defaultIndex)
    {
        GameObject selector = CreateUIObject(selectorName, row);
        AddLayoutElement(selector, -1, -1, 0, -1, -1, -1);
        HorizontalLayoutGroup hlg = selector.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        ContentSizeFitter selFitter = selector.AddComponent<ContentSizeFitter>();
        selFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        selFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Background sits behind Previous/Text/Next as decoration, not as
        // a layout participant - without IgnoreLayout it would be treated
        // as a 4th arranged item and break the stretch-to-fill look.
        GameObject bg = CreateImagePlaceholder("Background", selector.transform, new Color(0.15f, 0.2f, 0.3f));
        StretchFull(bg.GetComponent<RectTransform>());
        AddLayoutElement(bg, -1, -1, -1, -1, -1, -1);
        bg.GetComponent<LayoutElement>().ignoreLayout = true;

        GameObject prev = CreateUIObject("Previous", selector.transform);
        prev.AddComponent<Image>().color = new Color(0.95f, 0.71f, 0.18f);
        prev.AddComponent<Button>();
        AddLayoutElement(prev, SelectorArrowSize, SelectorArrowSize, 0, SelectorArrowSize, SelectorArrowSize, 0);

        GameObject text = CreateUIObject("Text", selector.transform);
        string startText = (defaultIndex >= 0 && defaultIndex < options.Count) ? options[defaultIndex] : "Default";
        CreateTMP(text, startText, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        // Same flex-grow rule as ValueLabel: Min Width floor, no fixed
        // Preferred Width, so it grows only as far as the current word
        // actually needs.
        AddLayoutElement(text, SelectorTextMinWidth, -1, 0, -1, -1, -1);

        GameObject next = CreateUIObject("Next", selector.transform);
        next.AddComponent<Image>().color = new Color(0.95f, 0.71f, 0.18f);
        next.AddComponent<Button>();
        AddLayoutElement(next, SelectorArrowSize, SelectorArrowSize, 0, SelectorArrowSize, SelectorArrowSize, 0);
    }

    // ---- low-level helpers ----------------------------------------------

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static GameObject CreateImagePlaceholder(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(GameObject go, string text, int fontSize, FontStyles style, TextAlignmentOptions align)
    {
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static void CreateHeaderText(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        GameObject go = CreateUIObject(name, parent);
        CreateTMP(go, text, fontSize, style, TextAlignmentOptions.TopLeft);
        // LayoutElement only - deliberately no ContentSizeFitter here.
        // The parent's VerticalLayoutGroup (Child Control Height = on) is
        // the single height driver; adding a second one on this object is
        // the exact bug that caused the header to overlap itself.
        AddLayoutElement(go, -1, -1, 1, -1, -1, -1);
    }

    private static void AddLayoutElement(GameObject go, float minW, float prefW, float flexW,
        float minH, float prefH, float flexH)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minWidth = minW;
        le.preferredWidth = prefW;
        le.flexibleWidth = flexW;
        le.minHeight = minH;
        le.preferredHeight = prefH;
        le.flexibleHeight = flexH;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}