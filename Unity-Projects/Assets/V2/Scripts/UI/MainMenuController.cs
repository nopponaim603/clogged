using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start screen: Start / Continue / Options / Exit.
///
/// Built in code so the scene stays a single object. The options page itself is
/// <see cref="OptionsPanelView"/>, shared with the in-game OPTIONS button, so the
/// menu only has to show and hide it. Continue is enabled only when a save file
/// exists; its caption names the day being resumed.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string title = "CLOGGED";
    [SerializeField] private string subtitle = "FIND THE KEY. OPEN THE BOX. ESCAPE.";

    private Button continueButton;
    private TMP_Text continueCaption;
    private OptionsPanelView optionsPanel;

    private void Awake()
    {
        // The title track is the menu's own; it is claimed before the UI builds
        // so the screen never appears silent for a frame.
        AudioDirector.EnsureExists().PlayTitleMusic();

        BuildUi();

        // Done here rather than in Start so the menu is already correct on the
        // frame it appears - an unfocused editor can stall before Start runs.
        RefreshContinue();
    }

    private void Start()
    {
        // Pays attention to a save written while the menu was already up.
        RefreshContinue();
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
        if (optionsPanel != null) optionsPanel.CloseResolutionList();
        SetOptionsVisible(true);
    }

    private void HandleExit() => SceneFlow.Quit();

    private void SetOptionsVisible(bool visible)
    {
        if (optionsPanel == null) return;

        optionsPanel.gameObject.SetActive(visible);
    }

    private void CloseOptions() => SetOptionsVisible(false);

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

        optionsPanel = OptionsPanelView.Create(root, "BACK", CloseOptions);
        optionsPanel.gameObject.SetActive(false);
    }
}