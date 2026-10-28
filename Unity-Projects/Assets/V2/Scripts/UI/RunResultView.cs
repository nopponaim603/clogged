using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Win / lose screen. Appears when <see cref="DayManager"/> declares an outcome,
/// shows the crystal (or the still-sealed box) plus the run's tallies, and offers
/// the two ways out of a finished run.
///
/// Built in code and kept on a dormant CanvasGroup so nothing about the run's
/// own HUD has to change while the panel is up.
/// </summary>
[DefaultExecutionOrder(1000)]
public class RunResultView : MonoBehaviour
{
    [Header("Art")]
    [Tooltip("Crystal shown on a win. Picked by RunResources.Variant.")]
    [SerializeField] private Sprite orangeCrystal;
    [SerializeField] private Sprite yellowCrystal;
    [Tooltip("Still-sealed box shown on a loss.")]
    [SerializeField] private Sprite orangeBox;
    [SerializeField] private Sprite yellowBox;

    [Header("Copy")]
    [SerializeField] private string winTitle = "YOU GOT A CRYSTAL!!";
    [SerializeField] private string winReason = "BOTH CORE ITEMS SECURED. THE CREW ESCAPES.";
    [SerializeField] private string loseTitle = "YOU CANNOT FIND A CRYSTAL";
    [SerializeField] private string loseDeadlineReason = "THE DEADLINE PASSED AND THE BOX IS STILL SEALED.";
    [SerializeField] private string loseCrewReason = "THE WHOLE CREW IS GONE. NOBODY IS LEFT TO SEARCH.";

    private CanvasGroup group;
    private Image emblem;
    private TMP_Text titleLabel;
    private TMP_Text reasonLabel;
    private RectTransform statsContainer;
    private Button primaryButton;
    private TMP_Text primaryLabel;
    private Button menuButton;
    private TMP_Text menuLabel;

    private bool built;
    private bool subscribed;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // Subscribed here rather than in Start: an unfocused editor can stall
        // before Start runs, and a result triggered on the first frame would
        // otherwise go unnoticed.
        Subscribe();
    }

    private void Start()
    {
        // Second chance when this object woke before DayManager did.
        if (!subscribed) Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;

        var days = DayManager.Instance;
        if (days == null) return;

        days.OnRunEnded += HandleRunEnded;
        subscribed = true;

        // A scene reloaded while the run was already over still has to show the panel.
        if (days.Outcome != RunOutcome.InProgress) HandleRunEnded(days.Outcome);
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        var days = DayManager.Instance;
        if (days != null) days.OnRunEnded -= HandleRunEnded;
        subscribed = false;
    }

    private void HandleRunEnded(RunOutcome outcome)
    {
        if (outcome == RunOutcome.InProgress) return;

        Build();
        Populate(outcome == RunOutcome.Win, outcome);

        // The crystal reveal is the mid-run flourish; the result screen replaces it.
        var reveal = FindAnyObjectByType<CrystalRevealView>();
        if (reveal != null) reveal.Hide(instant: true);

        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
        transform.SetAsLastSibling();
    }

    private void Populate(bool won, RunOutcome outcome)
    {
        titleLabel.text = won ? winTitle : loseTitle;
        titleLabel.color = won ? UiFactory.Accent : UiFactory.Fail;
        reasonLabel.text = won
            ? winReason
            : outcome == RunOutcome.LoseAllUnitsLost ? loseCrewReason : loseDeadlineReason;

        bool yellow = RunResources.Instance != null && RunResources.Instance.Variant == CoreItemVariant.Yellow;
        var art = won
            ? (yellow ? yellowCrystal : orangeCrystal)
            : (yellow ? yellowBox : orangeBox);

        emblem.sprite = art;
        emblem.enabled = art != null;
        emblem.color = won ? Color.white : new Color(1f, 1f, 1f, 0.55f);

        primaryLabel.text = won ? "NEW GAME" : "TRY AGAIN";
        menuLabel.text = "MAIN MENU";

        for (int i = statsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(statsContainer.GetChild(i).gameObject);
        }

        // Two columns keep the block short enough to clear the buttons below it.
        AddStatRow("DAYS USED", DaysUsed().ToString(), 0, 0);
        AddStatRow("FOOD COOKED", RunStats.FoodCooked.ToString(), 0, 1);
        AddStatRow("FOOD EATEN", RunStats.FoodConsumed.ToString(), 0, 2);
        AddStatRow("HEALS", RunStats.HealsPerformed.ToString(), 0, 3);
        AddStatRow("CRAFTED", RunStats.MedicineCrafted.ToString(), 1, 0);
        AddStatRow("GATHER TRIPS", RunStats.GatherTrips.ToString(), 1, 1);
        AddStatRow("CREW LOST", RunStats.UnitsLost.ToString(), 1, 2);
        AddStatRow("SURVIVORS", Survivors().ToString(), 1, 3);
    }

    private static int Survivors()
    {
        if (UnitManager.Instance == null) return 0;

        int alive = 0;
        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.IsAlive) alive++;
        }
        return alive;
    }

    /// <summary>
    /// The day the run ended on, capped at the deadline. A deadline loss advances
    /// the counter past the last day, which would otherwise read as "8 used" on a
    /// seven-day run.
    /// </summary>
    private static int DaysUsed()
    {
        var days = DayManager.Instance;
        if (days == null) return RunStats.DaysPlayed;

        return Mathf.Min(days.CurrentDay, days.DeadlineDay);
    }

    private void AddStatRow(string label, string value, int column, int row)
    {
        float y = -row * 40f;

        // Card content spans -360..+360; each column is 340 wide with the label
        // on the left and its number right-aligned against the column edge.
        float labelX = column == 0 ? -235f : 135f;
        float valueX = column == 0 ? -60f : 310f;

        var name = UiFactory.CreateText(label, statsContainer, label, 22f, UiFactory.DimText, TextAlignmentOptions.Left);
        UiFactory.Center(name.rectTransform, new Vector2(labelX, y), new Vector2(250f, 30f));

        var amount = UiFactory.CreateText(value, statsContainer, value, 22f, UiFactory.TextColor, TextAlignmentOptions.Right);
        UiFactory.Center(amount.rectTransform, new Vector2(valueX, y), new Vector2(80f, 30f));
    }

    private void Build()
    {
        if (built) return;
        built = true;

        var root = (RectTransform)transform;
        UiFactory.Stretch(root);

        var backdrop = UiFactory.CreateImage("Backdrop", root, new Color(0.02f, 0.03f, 0.05f, 0.9f));
        UiFactory.Stretch((RectTransform)backdrop.transform);
        backdrop.raycastTarget = true;

        var card = UiFactory.CreateImage("Card", root, UiFactory.Panel);
        UiFactory.Center((RectTransform)card.transform, new Vector2(0f, 20f), new Vector2(880f, 690f));

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = UiFactory.Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        emblem = UiFactory.CreateImage("Emblem", card.transform, Color.white);
        emblem.preserveAspect = true;
        emblem.raycastTarget = false;
        UiFactory.Center((RectTransform)emblem.transform, new Vector2(0f, 195f), new Vector2(140f, 140f));

        titleLabel = UiFactory.CreateText("Title", card.transform, "", 42f, UiFactory.Accent);
        UiFactory.Center(titleLabel.rectTransform, new Vector2(0f, 85f), new Vector2(800f, 56f));

        reasonLabel = UiFactory.CreateText("Reason", card.transform, "", 18f, UiFactory.DimText);
        UiFactory.Center(reasonLabel.rectTransform, new Vector2(0f, 42f), new Vector2(800f, 30f));

        var rule = UiFactory.CreateImage("Rule", card.transform, new Color(1f, 1f, 1f, 0.12f));
        UiFactory.Center((RectTransform)rule.transform, new Vector2(0f, 16f), new Vector2(720f, 2f));

        statsContainer = UiFactory.CreateRect("Stats", card.transform);
        UiFactory.Center(statsContainer, new Vector2(0f, -70f), new Vector2(760f, 180f));

        menuButton = UiFactory.CreateButton(card.transform, "MainMenuButton", "MAIN MENU", new Vector2(250f, 62f),
            SceneFlow.ToMainMenu);
        UiFactory.Center((RectTransform)menuButton.transform, new Vector2(-145f, -265f), new Vector2(250f, 62f));
        menuLabel = menuButton.GetComponentInChildren<TMP_Text>();

        primaryButton = UiFactory.CreateButton(card.transform, "PrimaryButton", "NEW GAME", new Vector2(250f, 62f),
            HandlePrimary);
        UiFactory.Center((RectTransform)primaryButton.transform, new Vector2(145f, -265f), new Vector2(250f, 62f));
        primaryLabel = primaryButton.GetComponentInChildren<TMP_Text>();
    }

    /// <summary>New Game after a win, Try Again after a loss - both are a fresh run.</summary>
    private void HandlePrimary()
    {
        var days = DayManager.Instance;
        if (days != null && days.Outcome == RunOutcome.Win) SceneFlow.NewGame();
        else SceneFlow.TryAgain();
    }
}
