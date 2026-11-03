using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The card that states the goal: the one line the run is played for, a way into
/// the game, and a way into the tutorial.
///
/// It is raised over the game scene once the map has loaded (see
/// <see cref="RunBootstrap"/>), so it reads as the first thing the run says
/// rather than as another menu step. Closing it is free - the dim backdrop is
/// the card's own, so pressing outside it is the same as starting.
/// </summary>
public class NewGamePromptView : MonoBehaviour
{
    private static readonly Color Backdrop = new Color(0.02f, 0.03f, 0.05f, 0.9f);
    private static readonly Color Border = new Color(1f, 0.88f, 0.55f, 0.55f);

    [SerializeField] private string headline = "FIND THE CRYSTAL AND WIN THE GAME!!";
    [SerializeField] private string body =
        "SEND YOUR CREW INTO THE CAVES, COLLECT THE KEY AND THE BOX, AND SURVIVE THE FOG.\n" +
        "FIRST TIME HERE? THE TUTORIAL WALKS YOU THROUGH ONE TRIP.";
    [SerializeField] private string startLabel = "START THE RUN";
    [SerializeField] private string tutorialLabel = "VIEW THE TUTORIAL?";

    private Action onStart;
    private Action onTutorial;
    private Action onClose;

    /// <summary>
    /// Raises the card over whatever scene is running. It gets its own canvas so
    /// it cannot lose the draw order to the map's canvas or the dice popup - both
    /// of which live on canvases of their own.
    /// </summary>
    public static NewGamePromptView Show(Action onStart, Action onTutorial, Action onClose = null)
    {
        Canvas host = null;
        foreach (var candidate in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!candidate.isRootCanvas) continue;
            host = candidate;
            break;
        }
        if (host == null) host = FindAnyObjectByType<Canvas>();
        if (host == null) return null;

        var go = new GameObject("NewGamePrompt", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster));
        var rect = (RectTransform)go.transform;
        rect.SetParent(host.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        // Above the tutorial overlay (600) and the dice popup (500).
        var own = go.GetComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = 700;

        var view = go.AddComponent<NewGamePromptView>();
        view.onStart = onStart;
        view.onTutorial = onTutorial;
        view.onClose = onClose;
        view.Build();
        return view;
    }

    /// <summary>Takes the card down. Safe to call twice.</summary>
    public void Close()
    {
        var closing = onClose;
        onStart = null;
        onTutorial = null;
        onClose = null;

        closing?.Invoke();
        TakeDown();
    }

    private void HandleStart()
    {
        var callback = onStart;
        onStart = null;
        onTutorial = null;
        onClose = null;

        callback?.Invoke();
        TakeDown();
    }

    private void HandleTutorial()
    {
        var callback = onTutorial;
        onStart = null;
        onTutorial = null;
        onClose = null;

        callback?.Invoke();
        TakeDown();
    }

    /// <summary>
    /// Hides first, then destroys: inactive this frame, gone next. Destroy alone
    /// would leave the card on screen for the rest of the frame, so a click that
    /// dismisses it could still land on it.
    /// </summary>
    private void TakeDown()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Build()
    {
        var root = (RectTransform)transform;

        var backdrop = UiFactory.CreateImage("Backdrop", root, Backdrop);
        UiFactory.Stretch((RectTransform)backdrop.transform);

        // The backdrop is the "start the run anyway" target, so a player who
        // clicks past the card is playing within one click.
        var catcher = backdrop.gameObject.AddComponent<Button>();
        catcher.transition = Selectable.Transition.None;
        catcher.onClick.AddListener(Close);

        var card = UiFactory.CreateImage("Card", root, UiFactory.Panel);
        UiFactory.Center((RectTransform)card.transform, Vector2.zero, new Vector2(940f, 420f));

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        // A card click must not fall through to the backdrop's close button.
        var cardBlocker = card.gameObject.AddComponent<Button>();
        cardBlocker.transition = Selectable.Transition.None;

        var title = UiFactory.CreateText("Headline", card.transform, headline, 40f, UiFactory.Accent);
        UiFactory.Center(title.rectTransform, new Vector2(0f, 128f), new Vector2(860f, 70f));
        title.textWrappingMode = TextWrappingModes.Normal;
        TmpFont.Apply(title);

        var copy = UiFactory.CreateText("Body", card.transform, body, 22f, UiFactory.TextColor);
        UiFactory.Center(copy.rectTransform, new Vector2(0f, 40f), new Vector2(820f, 90f));
        copy.textWrappingMode = TextWrappingModes.Normal;
        copy.alignment = TextAlignmentOptions.Center;
        TmpFont.Apply(copy);

        var start = UiFactory.CreateButton(card.transform, "StartRunButton", startLabel,
            new Vector2(340f, 62f), HandleStart);
        UiFactory.Center((RectTransform)start.transform, new Vector2(-186f, -112f), new Vector2(340f, 62f));

        var tutorial = UiFactory.CreateButton(card.transform, "ViewTutorialButton", tutorialLabel,
            new Vector2(340f, 62f), HandleTutorial);
        UiFactory.Center((RectTransform)tutorial.transform, new Vector2(186f, -112f), new Vector2(340f, 62f));
    }
}
