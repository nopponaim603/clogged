using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start screen: Start / Continue / Options / Exit.
///
/// Built in code so the scene stays a single object, and because the options
/// panel needs a resolution list that only exists at runtime anyway. Continue is
/// enabled only when a save file exists; its caption names the day being resumed.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string ResolutionKey = "clogged.resolution";
    private const string FullscreenKey = "clogged.fullscreen";
    private const string VolumeKey = "clogged.volume";

    [SerializeField] private string title = "CLOGGED";
    [SerializeField] private string subtitle = "FIND THE KEY. OPEN THE BOX. ESCAPE.";

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    private Button continueButton;
    private TMP_Text continueCaption;
    private GameObject optionsPanel;

    private Button resolutionField;
    private TMP_Text resolutionFieldLabel;
    private GameObject resolutionListRoot;
    private GameObject resolutionCatcher;
    private RectTransform resolutionListContent;
    private ScrollRect resolutionScroll;
    private readonly List<Button> resolutionRows = new List<Button>();

    private Button windowedButton;
    private Button fullscreenButton;

    private Slider volumeSlider;
    private TMP_Text volumeValue;
    private bool suppressVolumeCallback;

    private int resolutionIndex;
    private bool fullscreen;
    private float volume;

    private void Awake()
    {
        BuildResolutions();
        LoadPreferences();
        BuildUi();

        // Done here rather than in Start so the menu is already correct on the
        // frame it appears - an unfocused editor can stall before Start runs.
        ApplyPreferenceState();
        RefreshContinue();
    }

    private void Start()
    {
        // Pays attention to a save written while the menu was already up.
        ApplyPreferenceState();
        RefreshContinue();
    }

    // ---------------------------------------------------------------- options

    private void BuildResolutions()
    {
        resolutions.Clear();

        foreach (var mode in Screen.resolutions)
        {
            var size = new Vector2Int(mode.width, mode.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        // Largest first: the list opens on the biggest mode and the current
        // size is usually near the top instead of twenty rows down.
        resolutions.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
    }

    private void LoadPreferences()
    {
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreenMode != FullScreenMode.Windowed ? 1 : 0) == 1;
        volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));

        var current = new Vector2Int(Screen.width, Screen.height);
        int saved = PlayerPrefs.GetInt(ResolutionKey, -1);
        resolutionIndex = saved >= 0 && saved < resolutions.Count ? saved : resolutions.IndexOf(current);
        if (resolutionIndex < 0) resolutionIndex = Mathf.Max(0, resolutions.Count - 1);
    }

    private void SavePreferences()
    {
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    private void ApplyPreferenceState()
    {
        AudioListener.volume = volume;

        if (resolutionFieldLabel != null) resolutionFieldLabel.text = ResolutionLabel();

        PaintDisplayButtons();

        if (volumeSlider != null)
        {
            // Writing .value fires onValueChanged; the guard keeps that from
            // re-saving the same preference every time the panel is shown.
            suppressVolumeCallback = true;
            volumeSlider.value = volume;
            suppressVolumeCallback = false;
        }

        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    private void PaintDisplayButtons()
    {
        PaintChoice(windowedButton, !fullscreen);
        PaintChoice(fullscreenButton, fullscreen);
    }

    private static void PaintChoice(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;

        button.targetGraphic.color = selected ? UiFactory.ChoiceSelected : UiFactory.ButtonIdle;
    }

    private string ResolutionLabel()
    {
        if (resolutions.Count == 0) return $"{Screen.width} X {Screen.height}";
        var size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        return $"{size.x} X {size.y}";
    }

    private void SelectResolution(int index)
    {
        if (resolutions.Count == 0) return;

        resolutionIndex = Mathf.Clamp(index, 0, resolutions.Count - 1);
        CloseResolutionList();
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        var size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        var mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(size.x, size.y, mode);

        SavePreferences();
        ApplyPreferenceState();
        PaintResolutionRows();
    }

    private void SetFullscreen(bool value)
    {
        if (fullscreen == value) return;

        fullscreen = value;
        ApplyResolution();
    }

    private void SetVolume(float value)
    {
        if (suppressVolumeCallback) return;

        volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;

        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";

        SavePreferences();
    }

    // ------------------------------------------------------------------- menu

    private void RefreshContinue()
    {
        bool hasSave = SaveSystem.HasSave;

        if (continueButton != null) continueButton.interactable = hasSave;

        if (continueCaption == null) return;

        if (!hasSave)
        {
            continueCaption.text = "NO SAVED RUN";
            return;
        }

        var save = SaveSystem.Peek();
        continueCaption.text = save != null
            ? $"DAY {save.currentDay} / {save.deadlineDay}"
            : "SAVED RUN FOUND";
    }

    private void HandleStart() => SceneFlow.NewGame();

    private void HandleContinue() => SceneFlow.Continue();

    private void HandleOptions()
    {
        CloseResolutionList();
        SetOptionsVisible(true);
    }

    private void HandleExit() => SceneFlow.Quit();

    private void SetOptionsVisible(bool visible)
    {
        if (optionsPanel != null) optionsPanel.SetActive(visible);
    }

    // -------------------------------------------------------------------- ui

    private void BuildUi()
    {
        var root = (RectTransform)transform;

        var backdrop = UiFactory.CreateImage("Backdrop", root, new Color(0.035f, 0.05f, 0.075f, 1f));
        UiFactory.Stretch((RectTransform)backdrop.transform);

        var titleLabel = UiFactory.CreateText("Title", root, title, 86f, UiFactory.Accent);
        UiFactory.Center(titleLabel.rectTransform, new Vector2(0f, 300f), new Vector2(1000f, 110f));

        var subtitleLabel = UiFactory.CreateText("Subtitle", root, subtitle, 22f, UiFactory.DimText);
        UiFactory.Center(subtitleLabel.rectTransform, new Vector2(0f, 230f), new Vector2(1000f, 40f));

        var start = UiFactory.CreateButton(root, "StartButton", "START", new Vector2(360f, 66f), HandleStart);
        UiFactory.Center((RectTransform)start.transform, new Vector2(0f, 96f), new Vector2(360f, 66f));

        continueButton = UiFactory.CreateButton(root, "ContinueButton", "CONTINUE", new Vector2(360f, 66f), HandleContinue);
        UiFactory.Center((RectTransform)continueButton.transform, new Vector2(0f, 18f), new Vector2(360f, 66f));

        continueCaption = UiFactory.CreateText("ContinueCaption", root, "", 18f, UiFactory.DimText);
        UiFactory.Center(continueCaption.rectTransform, new Vector2(0f, -32f), new Vector2(600f, 26f));

        var options = UiFactory.CreateButton(root, "OptionsButton", "OPTIONS", new Vector2(360f, 66f), HandleOptions);
        UiFactory.Center((RectTransform)options.transform, new Vector2(0f, -78f), new Vector2(360f, 66f));

        var exit = UiFactory.CreateButton(root, "ExitButton", "EXIT", new Vector2(360f, 66f), HandleExit);
        UiFactory.Center((RectTransform)exit.transform, new Vector2(0f, -156f), new Vector2(360f, 66f));

        var hint = UiFactory.CreateText("Hint", root, "PROGRESS IS SAVED EACH TIME YOU END A DAY", 16f, UiFactory.DimText);
        UiFactory.Center(hint.rectTransform, new Vector2(0f, -240f), new Vector2(900f, 26f));

        BuildOptionsPanel(root);

        optionsPanel.SetActive(false);
    }

    private void BuildOptionsPanel(RectTransform root)
    {
        var panel = UiFactory.CreateImage("OptionsPanel", root, new Color(0.02f, 0.03f, 0.05f, 0.9f));
        UiFactory.Stretch((RectTransform)panel.transform);
        optionsPanel = panel.gameObject;

        var card = UiFactory.CreateImage("Card", panel.transform, UiFactory.Panel);
        UiFactory.Center((RectTransform)card.transform, Vector2.zero, new Vector2(820f, 520f));

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = UiFactory.Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        var heading = UiFactory.CreateText("Heading", card.transform, "OPTIONS", 44f, UiFactory.Accent);
        UiFactory.Center(heading.rectTransform, new Vector2(0f, 190f), new Vector2(700f, 60f));

        BuildRowLabel(card.transform, "RESOLUTION", 90f);
        BuildResolutionField(card.transform, 90f);

        BuildRowLabel(card.transform, "DISPLAY", 10f);
        windowedButton = BuildChoiceButton(card.transform, "WindowedButton", "WINDOW", -10f, 10f, 160f,
            () => SetFullscreen(false));
        fullscreenButton = BuildChoiceButton(card.transform, "FullscreenButton", "FULLSCREEN", 185f, 10f, 200f,
            () => SetFullscreen(true));

        BuildRowLabel(card.transform, "VOLUME", -70f);
        volumeSlider = BuildVolumeSlider(card.transform, 55f, -70f, 270f);
        volumeValue = UiFactory.CreateText("VolumeValue", card.transform, "", 24f, UiFactory.Accent);
        UiFactory.Center(volumeValue.rectTransform, new Vector2(310f, -70f), new Vector2(90f, 40f));

        var note = UiFactory.CreateText("AudioNote", card.transform, "AUDIO IS WIRED BUT HAS NO CLIPS YET", 15f, UiFactory.DimText);
        UiFactory.Center(note.rectTransform, new Vector2(0f, -130f), new Vector2(700f, 24f));

        var back = UiFactory.CreateButton(card.transform, "BackButton", "BACK", new Vector2(260f, 60f), CloseOptions);
        UiFactory.Center((RectTransform)back.transform, new Vector2(0f, -200f), new Vector2(260f, 60f));

        BuildResolutionList(card.transform);
    }

    private static void BuildRowLabel(Transform parent, string label, float y)
    {
        var name = UiFactory.CreateText(label + "Label", parent, label, 24f, UiFactory.TextColor, TextAlignmentOptions.Left);
        UiFactory.Center(name.rectTransform, new Vector2(-250f, y), new Vector2(300f, 40f));
    }

    /// <summary>
    /// The closed half of the dropdown: a button showing the current size with a
    /// rotated "&gt;" as its caret. Clicking it opens the list underneath.
    /// </summary>
    private void BuildResolutionField(Transform card, float y)
    {
        resolutionField = UiFactory.CreateButton(card, "ResolutionField", "", new Vector2(340f, 48f),
            ToggleResolutionList, 24f);
        UiFactory.Center((RectTransform)resolutionField.transform, new Vector2(110f, y), new Vector2(340f, 48f));

        resolutionFieldLabel = resolutionField.GetComponentInChildren<TMP_Text>();
        if (resolutionFieldLabel != null)
        {
            resolutionFieldLabel.alignment = TextAlignmentOptions.Left;
            resolutionFieldLabel.fontSizeMax = 22f;
        }

        // The pixel font has no arrow glyph, so the caret is a ">" turned on its
        // side instead of a character that would fall back to another typeface.
        var caret = UiFactory.CreateText("Caret", resolutionField.transform, ">", 22f, UiFactory.Accent);
        UiFactory.Center(caret.rectTransform, new Vector2(146f, 0f), new Vector2(24f, 24f));
        caret.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
    }

    private static Button BuildChoiceButton(Transform parent, string objectName, string label, float x, float y,
        float width, System.Action onClick)
    {
        var button = UiFactory.CreateButton(parent, objectName, label, new Vector2(width, 48f), onClick, 22f);
        UiFactory.Center((RectTransform)button.transform, new Vector2(x, y), new Vector2(width, 48f));
        return button;
    }

    private Slider BuildVolumeSlider(Transform parent, float x, float y, float width)
    {
        var slider = UiFactory.CreateSlider(parent, "VolumeSlider", width, 28f);
        UiFactory.Center((RectTransform)slider.transform, new Vector2(x, y), new Vector2(width, 28f));

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(SetVolume);
        return slider;
    }

    /// <summary>
    /// The open half of the dropdown: a scrollable list laid over the rows below
    /// the field, plus a click catcher so a click anywhere else closes it.
    /// </summary>
    private void BuildResolutionList(Transform card)
    {
        const float listWidth = 340f;
        const float listHeight = 204f;

        var catcher = UiFactory.CreateImage("ResolutionCatcher", card, new Color(0f, 0f, 0f, 0f));
        UiFactory.Stretch((RectTransform)catcher.transform);
        var catcherButton = catcher.gameObject.AddComponent<Button>();
        catcherButton.transition = Selectable.Transition.None;
        catcherButton.onClick.AddListener(CloseResolutionList);
        resolutionCatcher = catcher.gameObject;

        var list = UiFactory.CreateImage("ResolutionList", card, UiFactory.Panel);
        UiFactory.Center((RectTransform)list.transform, new Vector2(110f, 66f - listHeight * 0.5f),
            new Vector2(listWidth, listHeight));
        var listOutline = list.gameObject.AddComponent<Outline>();
        listOutline.effectColor = UiFactory.Border;
        listOutline.effectDistance = new Vector2(2f, -2f);
        listOutline.useGraphicAlpha = false;

        var viewport = UiFactory.CreateImage("Viewport", list.transform, new Color(1f, 1f, 1f, 0f));
        UiFactory.Stretch((RectTransform)viewport.transform, 6f);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = UiFactory.CreateRect("Content", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.padding = new RectOffset(2, 2, 2, 2);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        resolutionListContent = content;
        resolutionScroll = scroll;
        resolutionListRoot = list.gameObject;

        resolutionRows.Clear();
        for (int i = 0; i < resolutions.Count; i++)
        {
            int index = i;
            var row = UiFactory.CreateButton(content, "Row" + i, ResolutionText(i), new Vector2(0f, 34f),
                () => SelectResolution(index), 20f);

            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 34f;

            var rowLabel = row.GetComponentInChildren<TMP_Text>();
            if (rowLabel != null) rowLabel.alignment = TextAlignmentOptions.Left;

            resolutionRows.Add(row);
        }

        PaintResolutionRows();
        SetResolutionListVisible(false);
    }

    private string ResolutionText(int index)
    {
        if (index < 0 || index >= resolutions.Count) return "";
        var size = resolutions[index];
        return $"{size.x} X {size.y}";
    }

    private void PaintResolutionRows()
    {
        for (int i = 0; i < resolutionRows.Count; i++)
        {
            PaintChoice(resolutionRows[i], i == resolutionIndex);
        }
    }

    private void ToggleResolutionList()
    {
        bool visible = resolutionListRoot != null && !resolutionListRoot.activeSelf;
        SetResolutionListVisible(visible);
    }

    private void CloseResolutionList() => SetResolutionListVisible(false);

    private void SetResolutionListVisible(bool visible)
    {
        // The catcher only exists while the list is open; otherwise it would eat
        // every click meant for the rows underneath.
        if (resolutionCatcher != null) resolutionCatcher.SetActive(visible);
        if (resolutionListRoot == null) return;

        resolutionListRoot.SetActive(visible);
        if (!visible) return;

        resolutionListRoot.transform.SetAsLastSibling();
        PaintResolutionRows();
        ScrollToSelected();
    }

    /// <summary>
    /// Puts the picked size in view. The rows are a fixed stride, so the offset
    /// is arithmetic rather than a walk over the layout.
    /// </summary>
    private void ScrollToSelected()
    {
        if (resolutionListContent == null || resolutionScroll == null) return;

        const float rowStride = 36f;   // 34 row + 2 spacing
        const float padding = 2f;

        Canvas.ForceUpdateCanvases();

        float viewportHeight = resolutionScroll.viewport.rect.height;
        float contentHeight = resolutionListContent.rect.height;
        float maxOffset = Mathf.Max(0f, contentHeight - viewportHeight);

        float rowTop = padding + resolutionIndex * rowStride;
        float offset = Mathf.Clamp(rowTop - (viewportHeight - 34f) * 0.5f, 0f, maxOffset);

        resolutionListContent.anchoredPosition = new Vector2(resolutionListContent.anchoredPosition.x, offset);
    }

    private void CloseOptions()
    {
        CloseResolutionList();
        SetOptionsVisible(false);
    }
}
