using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The options page: resolution, window mode, audio faders.
///
/// Built once here and shared by the main menu and the in-game OPTIONS button so
/// both screens read as the same page - only the footer differs (BACK on the
/// menu, BACK TO GAME / RETURN TO MENU inside a run). Preferences live in
/// PlayerPrefs under fixed keys, so the two entry points always agree.
///
/// Audio is three faders, not one: Master, Music and SFX each map to an exposed
/// parameter on the game mixer through <see cref="AudioDirector"/>, which is what
/// lets a player keep the BGM quiet without losing the sting cues.
/// </summary>
public class OptionsPanelView : MonoBehaviour
{
    private const string ResolutionKey = "clogged.resolution";
    private const string FullscreenKey = "clogged.fullscreen";

    /// <summary>Row the resolution field sits on; the dropdown hangs off its bottom edge.</summary>
    private const float ResolutionRowY = 130f;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private readonly List<Button> resolutionRows = new List<Button>();
    private readonly List<AudioFader> faders = new List<AudioFader>();

    /// <summary>One row of the audio section: its PlayerPrefs key plus the widgets that show it.</summary>
    private sealed class AudioFader
    {
        public string key;
        public Slider slider;
        public TMP_Text value;
    }

    private Button resolutionField;
    private TMP_Text resolutionFieldLabel;
    private GameObject resolutionListRoot;
    private GameObject resolutionCatcher;
    private RectTransform resolutionListContent;
    private ScrollRect resolutionScroll;

    private Button windowedButton;
    private Button fullscreenButton;

    private bool suppressVolumeCallback;

    private int resolutionIndex;
    private bool fullscreen;

    /// <summary>
    /// Builds the whole page (dim backdrop + card) under <paramref name="parent"/>.
    /// When <paramref name="secondaryLabel"/> is empty the footer is a single
    /// centred BACK button; otherwise the two buttons sit side by side.
    /// </summary>
    public static OptionsPanelView Create(Transform parent, string primaryLabel, Action primaryAction,
        string secondaryLabel = null, Action secondaryAction = null)
    {
        var backdrop = UiFactory.CreateImage("OptionsPanel", parent, new Color(0.02f, 0.03f, 0.05f, 0.9f));
        UiFactory.Stretch((RectTransform)backdrop.transform);

        var view = backdrop.gameObject.AddComponent<OptionsPanelView>();
        view.Build(primaryLabel, primaryAction, secondaryLabel, secondaryAction);
        return view;
    }

    private void Awake()
    {
        BuildResolutions();
        LoadPreferences();
    }

    // ------------------------------------------------------------ construction

    private void Build(string primaryLabel, Action primaryAction, string secondaryLabel, Action secondaryAction)
    {
        var card = UiFactory.CreateImage("Card", transform, UiFactory.Panel);
        UiFactory.Center((RectTransform)card.transform, Vector2.zero, new Vector2(820f, 520f));

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = UiFactory.Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        var heading = UiFactory.CreateText("Heading", card.transform, "OPTIONS", 44f, UiFactory.Accent);
        UiFactory.Center(heading.rectTransform, new Vector2(0f, 200f), new Vector2(700f, 60f));

        BuildRowLabel(card.transform, "RESOLUTION", ResolutionRowY);
        BuildResolutionField(card.transform, ResolutionRowY);

        BuildRowLabel(card.transform, "DISPLAY", 66f);
        windowedButton = BuildChoiceButton(card.transform, "WindowedButton", "WINDOW", -10f, 66f, 160f,
            () => SetFullscreen(false));
        fullscreenButton = BuildChoiceButton(card.transform, "FullscreenButton", "FULLSCREEN", 185f, 66f, 200f,
            () => SetFullscreen(true));

        // Three faders, one per mixer group. Kept on their own rows so the value
        // readout of one never sits under the slider of another. That is the whole
        // page now: the red fog has no control, because Risk drives it.
        BuildFader(card.transform, "Master", AudioDirector.MasterVolumeKey, 4f);
        BuildFader(card.transform, "Music", AudioDirector.MusicVolumeKey, -50f);
        BuildFader(card.transform, "Sfx", AudioDirector.SfxVolumeKey, -104f);

        BuildFooter(card.transform, primaryLabel, primaryAction, secondaryLabel, secondaryAction);

        BuildResolutionList(card.transform);

        ApplyPreferenceState();
    }

    private static void BuildFooter(Transform card, string primaryLabel, Action primaryAction,
        string secondaryLabel, Action secondaryAction)
    {
        const float y = -204f;
        var size = new Vector2(300f, 56f);

        if (string.IsNullOrEmpty(secondaryLabel))
        {
            var back = UiFactory.CreateButton(card, "BackButton", primaryLabel, size, primaryAction);
            UiFactory.Center((RectTransform)back.transform, new Vector2(0f, y), size);
            return;
        }

        var leave = UiFactory.CreateButton(card, "ReturnToGameButton", primaryLabel, size, primaryAction);
        UiFactory.Center((RectTransform)leave.transform, new Vector2(-158f, y), size);

        var menu = UiFactory.CreateButton(card, "ReturnToMenuButton", secondaryLabel, size, secondaryAction);
        UiFactory.Center((RectTransform)menu.transform, new Vector2(158f, y), size);
    }

    private static void BuildRowLabel(Transform parent, string label, float y)
    {
        var name = UiFactory.CreateText(label + "Label", parent, label, 24f, UiFactory.TextColor,
            TextAlignmentOptions.Left);
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
        float width, Action onClick)
    {
        var button = UiFactory.CreateButton(parent, objectName, label, new Vector2(width, 48f), onClick, 22f);
        UiFactory.Center((RectTransform)button.transform, new Vector2(x, y), new Vector2(width, 48f));
        return button;
    }

    /// <summary>
    /// One audio row: label on the left, slider through the middle, percentage on
    /// the right - the same shape the single VOLUME row used to have.
    /// </summary>
    private void BuildFader(Transform card, string label, string key, float y)
    {
        BuildRowLabel(card, label.ToUpperInvariant(), y);

        var slider = UiFactory.CreateSlider(card, label + "Slider", 270f, 28f);
        UiFactory.Center((RectTransform)slider.transform, new Vector2(55f, y), new Vector2(270f, 28f));

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        var readout = UiFactory.CreateText(label + "Value", card, "", 24f, UiFactory.Accent);
        UiFactory.Center(readout.rectTransform, new Vector2(310f, y), new Vector2(90f, 40f));

        var fader = new AudioFader { key = key, slider = slider, value = readout };
        slider.onValueChanged.AddListener(value => OnFaderChanged(fader, value));
        faders.Add(fader);
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
        UiFactory.Center((RectTransform)list.transform,
            new Vector2(110f, ResolutionRowY - 24f - listHeight * 0.5f), new Vector2(listWidth, listHeight));
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

    // ------------------------------------------------------------- preferences

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
        fullscreen = PlayerPrefs.GetInt(FullscreenKey,
            Screen.fullScreenMode != FullScreenMode.Windowed ? 1 : 0) == 1;

        var current = new Vector2Int(Screen.width, Screen.height);
        int saved = PlayerPrefs.GetInt(ResolutionKey, -1);
        resolutionIndex = saved >= 0 && saved < resolutions.Count ? saved : resolutions.IndexOf(current);
        if (resolutionIndex < 0) resolutionIndex = Mathf.Max(0, resolutions.Count - 1);
    }

    private void SavePreferences()
    {
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyPreferenceState()
    {
        if (resolutionFieldLabel != null) resolutionFieldLabel.text = ResolutionLabel();

        PaintChoice(windowedButton, !fullscreen);
        PaintChoice(fullscreenButton, fullscreen);

        // Writing .value fires onValueChanged; the guard keeps that from
        // re-saving the same preference every time the panel is shown.
        suppressVolumeCallback = true;
        foreach (var fader in faders)
        {
            float value = PlayerPrefs.GetFloat(fader.key, 0.8f);
            fader.slider.value = Mathf.Clamp01(value);
            PaintFader(fader, value);
        }
        suppressVolumeCallback = false;

        // The mixer may not have been built yet on the menu's first frame, so the
        // levels are pushed again here rather than only on a slider drag.
        AudioDirector.Instance?.ApplyVolumes();
    }

    private void OnFaderChanged(AudioFader fader, float value)
    {
        // Programmatic writes (ApplyPreferenceState) must not click: the guard is
        // already there to stop them re-saving, and a slider that chirps when the
        // page opens reads as a bug.
        if (suppressVolumeCallback) return;

        float clamped = Mathf.Clamp01(value);
        PaintFader(fader, clamped);

        // UiClickSound.Play throttles, so dragging gives a tick instead of a buzz.
        UiClickSound.Play(UiClickKind.Plain);

        AudioDirector.SetVolume(fader.key, clamped);
    }

    private static void PaintFader(AudioFader fader, float value)
    {
        if (fader.value != null) fader.value.text = Mathf.RoundToInt(value * 100f) + "%";
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

    private string ResolutionText(int index)
    {
        if (index < 0 || index >= resolutions.Count) return "";
        var size = resolutions[index];
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
    // ------------------------------------------------------------ the dropdown

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

    /// <summary>Closes the dropdown if it happens to be open. Safe to call anytime.</summary>
    public void CloseResolutionList() => SetResolutionListVisible(false);

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
}
