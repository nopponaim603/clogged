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
    private TMP_Text resolutionValue;
    private TMP_Text displayValue;
    private TMP_Text volumeValue;

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

        if (resolutionValue != null) resolutionValue.text = ResolutionLabel();
        if (displayValue != null) displayValue.text = fullscreen ? "FULLSCREEN" : "WINDOWED";
        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    private string ResolutionLabel()
    {
        if (resolutions.Count == 0) return $"{Screen.width} X {Screen.height}";
        var size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        return $"{size.x} X {size.y}";
    }

    private void CycleResolution(int direction)
    {
        if (resolutions.Count == 0) return;

        resolutionIndex = (resolutionIndex + direction + resolutions.Count) % resolutions.Count;
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        var size = resolutions[Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1)];
        var mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(size.x, size.y, mode);

        SavePreferences();
        ApplyPreferenceState();
    }

    private void ToggleDisplay()
    {
        fullscreen = !fullscreen;
        ApplyResolution();
    }

    private void ChangeVolume(float delta)
    {
        volume = Mathf.Clamp01(Mathf.Round((volume + delta) * 10f) / 10f);
        AudioListener.volume = volume;

        SavePreferences();
        ApplyPreferenceState();
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

    private void HandleOptions() => SetOptionsVisible(true);

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

        resolutionValue = BuildRow(card.transform, "RESOLUTION", 90f,
            () => CycleResolution(-1), () => CycleResolution(1));

        displayValue = BuildRow(card.transform, "DISPLAY", 10f,
            ToggleDisplay, ToggleDisplay);

        volumeValue = BuildRow(card.transform, "VOLUME", -70f,
            () => ChangeVolume(-0.1f), () => ChangeVolume(0.1f));

        var note = UiFactory.CreateText("AudioNote", card.transform, "AUDIO IS WIRED BUT HAS NO CLIPS YET", 15f, UiFactory.DimText);
        UiFactory.Center(note.rectTransform, new Vector2(0f, -130f), new Vector2(700f, 24f));

        var back = UiFactory.CreateButton(card.transform, "BackButton", "BACK", new Vector2(260f, 60f),
            () => SetOptionsVisible(false));
        UiFactory.Center((RectTransform)back.transform, new Vector2(0f, -200f), new Vector2(260f, 60f));
    }

    /// <summary>One options row: name on the left, current value in the middle, two stepper buttons on the right.</summary>
    private TMP_Text BuildRow(Transform parent, string label, float y, System.Action decrease, System.Action increase)
    {
        var name = UiFactory.CreateText(label + "Label", parent, label, 24f, UiFactory.TextColor, TextAlignmentOptions.Left);
        UiFactory.Center(name.rectTransform, new Vector2(-250f, y), new Vector2(300f, 40f));

        var value = UiFactory.CreateText(label + "Value", parent, "", 24f, UiFactory.Accent);
        UiFactory.Center(value.rectTransform, new Vector2(40f, y), new Vector2(260f, 40f));

        var previous = UiFactory.CreateButton(parent, label + "Previous", "<", new Vector2(56f, 48f), decrease, 24f);
        UiFactory.Center((RectTransform)previous.transform, new Vector2(210f, y), new Vector2(56f, 48f));

        var next = UiFactory.CreateButton(parent, label + "Next", ">", new Vector2(56f, 48f), increase, 24f);
        UiFactory.Center((RectTransform)next.transform, new Vector2(280f, y), new Vector2(56f, 48f));

        return value;
    }
}
