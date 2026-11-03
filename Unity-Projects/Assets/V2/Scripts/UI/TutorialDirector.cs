using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The scripted tour that runs in the Tutorial scene: one coach mark per idea,
/// then it hands the player back to the real game.
///
/// The steps are written out as a list rather than driven by a state machine,
/// because the whole script is content: each entry says what to highlight, what
/// to read, and how it ends (a tap anywhere, or the player actually doing the
/// thing the card asks for). Nothing here changes a rule - the tutorial plays the
/// real game, one cave, one trip, one cook.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    /// <summary>
    /// The scripted roll. With Risk at its default 80 and Ann sent to the Easy
    /// cave, that cave reads SV 55, so 10 lands inside the Hard Success band.
    /// </summary>
    public const int ScriptedRoll = 10;

    private sealed class Step
    {
        public string text;
        public string prompt = TapPrompt;
        public Func<RectTransform> target;
        public Func<bool> done;
        public Action enter;
        public bool catchTap = true;
        public Func<RectTransform[]> focus;

        /// <summary>
        /// When set, the coach mark stays off screen until this returns true.
        /// Used for the dice step: the player watches the dice tumble first, and
        /// the card only arrives once they have stopped.
        /// </summary>
        public Func<bool> wait;
    }

    private const string TapPrompt = "CLICK TO CONTINUE";
    private const string ActPrompt = "DO IT ON THE HIGHLIGHTED CONTROL";

    [Tooltip("Ann is the guide: she is the Easy natural, so every step that names a card names hers.")]
    [SerializeField] private string guideName = "Ann";

    private readonly List<Step> steps = new List<Step>();
    private int index = -1;

    /// <summary>False while the step is waiting for something on screen and has not drawn yet.</summary>
    private bool shown;

    private TutorialOverlay overlay;
    private RectTransform canvasRoot;

    /// <summary>Target the ring is currently on, so a step can be re-aimed when it changes.</summary>
    private RectTransform lastTarget;

    /// <summary>Union target for the dungeon row, and for anything else made of several rects.</summary>
    private RectTransform focus;

    private UnitRosterView roster;
    private GatherMapController map;
    private SkillFlowController flow;
    private DispatchActionPanel action;
    private DayManager days;

    /// <summary>Step currently on screen, for tests (-1 before the first one).</summary>
    public int StepIndex => index;

    /// <summary>Total number of coach marks in the script, for tests.</summary>
    public int StepCount => steps.Count;

    /// <summary>True once the closing card is up.</summary>
    public bool IsFinished { get; private set; }

    /// <summary>The copy currently on screen, so a test can assert the script order.</summary>
    public string CurrentText => index >= 0 && index < steps.Count ? steps[index].text : "";

    /// <summary>True when the current step advances on a tap instead of waiting for an action.</summary>
    public bool CurrentIsTapStep => index >= 0 && index < steps.Count && steps[index].done == null;

    /// <summary>Feeds a tap in from code - the editor cannot synthesise mouse input for tests.</summary>
    public void Tap()
    {
        if (overlay != null) overlay.Tap();
    }

    /// <summary>Runs the gate check once, so a test can advance without waiting for a frame.</summary>
    public void Pump()
    {
        if (index < 0 || index >= steps.Count) return;

        var step = steps[index];

        // A step that is waiting for something on screen (the dice landing)
        // has not drawn yet; show it the moment the wait clears.
        if (!shown)
        {
            if (step.wait != null && !step.wait()) return;

            ShowCurrent(step);
            return;
        }

        // A target that only appears later (the dice popup's CONTINUE, the
        // queued-round number) has to be picked up the frame it shows up,
        // otherwise the ring sits in the middle of the screen pointing nowhere.
        if (step.target != null)
        {
            var target = step.target();
            if (target != lastTarget)
            {
                lastTarget = target;
                overlay.Retarget(target);
            }
        }

        if (step.done != null && step.done()) Advance();
    }

    private void Start()
    {
        // A tour must never touch the player's real save, and its End Day would
        // otherwise write one.
        SaveSystem.Suspended = true;

        roster = FindAnyObjectByType<UnitRosterView>();
        map = GatherMapController.Instance;
        flow = SkillFlowController.Instance;
        action = FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        days = DayManager.Instance;

        overlay = TutorialOverlay.Create();
        if (overlay == null)
        {
            Debug.LogWarning("[TutorialDirector] No canvas to draw the tutorial on.");
            return;
        }

        canvasRoot = (RectTransform)overlay.transform.parent;
        focus = UiFactory.CreateRect("TutorialFocus", canvasRoot);
        focus.anchorMin = focus.anchorMax = new Vector2(0.5f, 0.5f);
        focus.pivot = Vector2.zero;
        focus.gameObject.SetActive(true);

        BuildScript();
        index = -1;
        Advance();
    }

    private void Update()
    {
        Pump();
    }

    /// <summary>Jumps to the next step, running its enter hook and painting its coach mark.</summary>
    private void Advance()
    {
        index++;
        shown = false;

        if (index >= steps.Count)
        {
            Finish();
            return;
        }

        var step = steps[index];
        step.enter?.Invoke();

        // A step that is waiting on the screen (a tumbling dice popup) keeps the
        // overlay clear so the player watches the thing, not the card.
        if (step.wait != null && !step.wait())
        {
            overlay.HideStep();
            lastTarget = null;
            return;
        }

        ShowCurrent(step);
    }

    /// <summary>Paints the coach mark for the step the tour is on.</summary>
    private void ShowCurrent(Step step)
    {
        shown = true;

        var target = step.target != null ? step.target() : null;
        lastTarget = target;
        overlay.ShowStep(step.text, step.prompt, target, step.catchTap, Advance,
            step.focus != null ? step.focus() : null);
    }

    private void Finish()
    {
        IsFinished = true;
        overlay.ShowFinale(
            "THAT IS EVERYTHING YOU NEED TO SURVIVE OUT HERE. GOOD LUCK!",
            "RETURN TO GAME",
            Depart);
    }

    /// <summary>The tutorial is a sandbox: leaving it starts the real run from day one.</summary>
    private void Depart()
    {
        SaveSystem.Suspended = false;

        // The tour left the board in a half-played state; a real run starts
        // clean. Straight into the game - the player has just been taught the
        // goal, so the goal card would be talking to itself.
        SceneFlow.NewGame();
    }

    private void OnDestroy()
    {
        // Leaving the scene any other way (menu, quit) must not leave saves off.
        SaveSystem.Suspended = false;
    }

    // ------------------------------------------------------------- script

    private void BuildScript()
    {
        steps.Clear();

        Add("The goal of this game is to find the crystal and beat the game.", null, null);

        Add("The crystal is in the Chest in these dungeon.",
            DungeonRow, null);

        Add("You need to find both Key and Chest from the dungeon.",
            GoalFrame, null);

        Add("Choose your agent (up to 2 per dispatch) and plan your strategy to explore the dungeon.",
            HandRow, null);

        Add("Each agent has their own aptitude. Using the wrong person for the wrong job carries a penalty.",
            GuideCard, null, null, TapPrompt, GuideAptitude);

        Add($"Choose {guideName}.", GuideCard, GuidePicked, null, ActPrompt);

        Add("Choose Easy Dungeon.", EasyCave, CavePicked, null, ActPrompt);

        Add("Exploring or using a skill costs AP. It refills when the next day starts.",
            GuideAp, null);

        Add("Exploring a dungeon also costs readiness. Each agent can explore once per day; it refills the next day.",
            GuideSword, null);

        Add("Every dungeon has its own Success Value (SV). Buffs and penalties move it.",
            EasyChance, null);

        Add("These are the items you can gain while exploring this dungeon.",
            EasyDrops, null);

        Add("Click here to dispatch.", DispatchButton, DiceOpened,
            () => GatherResolver.ForcedRoll = ScriptedRoll, ActPrompt);

        // The dice are the one moment the game asks the player to just watch:
        // the card is held back until they stop, then it explains the result.
        Add("Roll a die and aim for a value lower than the SV to succeed.",
            DicePanel, null, null, TapPrompt, null, DiceSettled);

        Add("A Hard Success rewards you with a bonus, while a Fumble gives you a harsh penalty.",
            DiceBar, null);

        Add("Click continue.", DiceContinue, DiceClosed, null, ActPrompt);

        Add("Pioneering unknown territory is not easy. There is a risk that hinders your journey - " +
            "it directly lowers your Success Value. Cleared dungeons bring that risk down.",
            RiskGauge, null);

        Add("Also pay attention to your resources. Run out of food and you face a penalty.",
            ResourceBar, null);

        Add("You can use skills here to turn resources into what you need.",
            SkillColumn, null);

        Add($"Choose {guideName}.", GuideCard, GuidePicked, null, ActPrompt);

        Add("Choose Cook.", CookButton, CookArmed, null, ActPrompt);

        Add("Clicking a skill adds a queued round.", CookQueue, null);

        Add("Choose Cook again.", CookButton, () => flow != null && flow.PendingRounds >= 2,
            null, ActPrompt);

        Add("You can queue as many rounds as your AP and materials allow.", CookQueue, null);

        Add("Click Cook to confirm.", DispatchButton, SkillResolved, null, ActPrompt);

        Add("Click here to end the day.", DayFrame, DayEnded, null, ActPrompt);

        Add("You can read more about the game here.", HelpButton, null);
    }

    private void Add(string text, Func<RectTransform> target, Func<bool> done,
        Action enter = null, string prompt = TapPrompt, Func<RectTransform[]> focus = null,
        Func<bool> wait = null)
    {
        steps.Add(new Step
        {
            text = text,
            target = target,
            done = done,
            enter = enter,
            prompt = prompt,
            catchTap = done == null,
            focus = focus,
            wait = wait
        });
    }

    // ------------------------------------------------------------ targets

    private RectTransform Find(string path)
    {
        if (canvasRoot == null) return null;
        return canvasRoot.Find(path) as RectTransform;
    }

    private static RectTransform Child(RectTransform parent, string name)
    {
        if (parent == null) return null;
        return parent.Find(name) as RectTransform;
    }

    /// <summary>First of the candidates that is on screen, so a step can name a fallback.</summary>
    private static RectTransform FirstLive(params RectTransform[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate != null && candidate.gameObject.activeInHierarchy) return candidate;
        }
        return candidates.Length > 0 ? candidates[0] : null;
    }

    private UnitIconView GuideView()
    {
        if (roster == null) return null;

        foreach (var card in roster.Cards)
        {
            if (card != null && card.Unit != null && card.Unit.unitName == guideName) return card;
        }
        return roster.Cards.Count > 0 ? roster.Cards[0] : null;
    }

    private RectTransform GuideCard()
    {
        // The inner "Lift" rect, not the card root: the root carries 70px of
        // padding above the art for the hover lift, and ringing that empty band
        // reads as a mistake.
        var view = GuideView();
        if (view == null) return null;

        var root = (RectTransform)view.transform;
        var lift = Child(root, "Lift");
        return lift != null ? lift : root;
    }

    /// <summary>
    /// The letter badge that carries the agent's main aptitude - the one thing
    /// the "wrong person for the wrong job" card is pointing at. A single snug
    /// ring reads better than ringing the whole card.
    /// </summary>
    private RectTransform[] GuideAptitude()
    {
        // One ring around the pair: the green aptitude letter and the talent
        // glyph below it read as a single block of "who this agent is", so they
        // are boxed together instead of as two separate hotspots.
        var lift = GuideCard();
        if (lift == null) return null;

        var box = Union(Child(lift, "AptitudeBadge"), Child(lift, "TalentIcon"));
        return box != null ? new[] { box } : null;
    }

    private RectTransform GuideAp()
    {
        return Child(GuideCard(), "ApPips");
    }

    private RectTransform GuideSword()
    {
        return Child(GuideCard(), "StateIcon");
    }

    private RectTransform HandRow()
    {
        var container = Find("CharacterHandPanel/Cards");
        if (container == null) return null;

        var rects = new List<RectTransform>();
        foreach (var card in roster != null ? roster.Cards : null)
        {
            if (card == null) continue;

            var root = (RectTransform)card.transform;
            var lift = Child(root, "Lift");
            rects.Add(lift != null ? lift : root);
        }

        return rects.Count > 0 ? Union(rects.ToArray()) : container;
    }

    private MapNodeView NodeOf(GatherZone zone)
    {
        if (map == null) return null;

        foreach (var node in map.Nodes)
        {
            if (node != null && node.Data != null && node.Data.zone == zone) return node;
        }
        return map.Nodes.Count > 0 ? map.Nodes[0] : null;
    }

    private RectTransform DungeonRow()
    {
        if (map == null) return null;

        var rects = new List<RectTransform>();
        foreach (var node in map.Nodes)
        {
            if (node != null) rects.Add((RectTransform)node.transform);
        }
        return Union(rects.ToArray());
    }

    private RectTransform EasyCave() => NodeOf(GatherZone.Easy)?.transform as RectTransform;

    private RectTransform EasyChance() => Child(EasyCave(), "Chance");

    private RectTransform EasyDrops() => Child(EasyCave(), "Drops");

    private RectTransform GoalFrame()
    {
        var group = Find("StatusBar/CoreItemGroup");
        return FirstLive(Child(group, "GoalFrame"), group);
    }

    private RectTransform DispatchButton() => action != null ? (RectTransform)action.transform : null;

    private Transform DiceRoot()
    {
        if (DiceRollView.Instance == null) return null;
        return DiceRollView.Instance.transform.Find("Overlay/Panel");
    }

    private RectTransform DicePanel() => DiceRoot() as RectTransform;

    private RectTransform DiceBar() => DiceRoot()?.Find("Bar") as RectTransform;

    private RectTransform DiceContinue() => DiceRoot()?.Find("ContinueButton") as RectTransform;

    private RectTransform RiskGauge()
    {
        var fog = RedFogOverlay.Instance;
        if (fog == null) return null;
        return fog.OverlayRoot.Find("FogGauge") as RectTransform;
    }

    private RectTransform ResourceBar() => Find("StatusBar/TopBar/ResourceRow");

    private RectTransform SkillColumn() => Find("SkillTriggerPanel");

    private RectTransform CookButton() => Find("SkillTriggerPanel/CookRow/CookButton");

    private RectTransform CookQueue()
    {
        var row = Find("SkillTriggerPanel/CookRow");
        return FirstLive(Child(row, "Count"), Child(row, "CountSlot"), row);
    }

    private RectTransform DayFrame() => Find("StatusBar/DayFrame");

    private RectTransform HelpButton() => Find("HudTopBar/HelpButton");

    /// <summary>
    /// A transient rect covering several controls - the dungeon row, or the two
    /// small badges on a card. The overlay needs one rect, so the union is
    /// written into the hidden focus object and handed over.
    /// </summary>
    private RectTransform Union(params RectTransform[] targets)
    {
        if (focus == null) return null;

        bool any = false;
        float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f;

        foreach (var target in targets)
        {
            if (target == null) continue;

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            for (int i = 0; i < 4; i++)
            {
                Vector2 local = canvasRoot.InverseTransformPoint(corners[i]);
                if (!any)
                {
                    minX = maxX = local.x;
                    minY = maxY = local.y;
                    any = true;
                }
                else
                {
                    minX = Mathf.Min(minX, local.x);
                    maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }
            }
        }

        if (!any) return null;

        focus.anchoredPosition = new Vector2(minX, minY);
        focus.sizeDelta = new Vector2(maxX - minX, maxY - minY);
        return focus;
    }

    // ------------------------------------------------------------- gates

    private bool GuidePicked()
    {
        var view = GuideView();
        return view != null && view.IsSelected;
    }

    private bool CavePicked()
    {
        return map != null && map.SelectedNode != null &&
               map.SelectedNode.Data != null && map.SelectedNode.Data.zone == GatherZone.Easy;
    }

    private bool DiceOpened() => DiceRollView.Instance != null && DiceRollView.Instance.IsShowing;

    private bool DiceClosed() => DiceRollView.Instance == null || !DiceRollView.Instance.IsShowing;

    /// <summary>The dice have stopped tumbling and the result is on screen.</summary>
    private bool DiceSettled() => DiceRollView.Instance != null && DiceRollView.Instance.Settled;

    private bool CookArmed() => flow != null && flow.Armed == SkillKind.Cook;

    /// <summary>The confirm click resolved the queued cook and handed the button back.</summary>
    private bool SkillResolved() => flow != null && !flow.IsSkillActive;

    private bool DayEnded()
    {
        if (days == null) return true;
        return days.CurrentDay > 1;
    }
}
