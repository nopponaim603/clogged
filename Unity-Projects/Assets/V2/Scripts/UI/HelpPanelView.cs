using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The how-to-play page behind the HELP button.
///
/// Layout: a left rail of sections and a scrolling right pane, because the copy
/// outgrew a single block of text. One section is shown at a time, so a new
/// player reads five short lines instead of a wall - and every number here is
/// read from the live controllers, so the page can never quote a stale cost
/// (that is exactly how the old Heal tooltip drifted to "2 AP").
///
/// Copy is English capitals: the HUD runs on the baked pixel font, which only
/// carries A-Z, 0-9 and a small punctuation set.
/// </summary>
public class HelpPanelView : MonoBehaviour
{
    private const float RailWidth = 300f;
    private const float CardWidth = 1200f;
    private const float CardHeight = 720f;

    // Pane insets inside the card: left clears the rail, the rest are breathing
    // room. Kept as constants because the scrollbar has to line up with them.
    private const float PaneLeft = RailWidth + 52f;
    private const float PaneRight = 52f;
    private const float PaneBottom = 24f;
    private const float PaneTop = 140f;

    private static float PaneHeight => CardHeight - PaneTop - PaneBottom;

    /// <summary>Vertical offset of the pane's centre from the card's centre.</summary>
    private static float PaneCenterY => (PaneBottom + PaneHeight * 0.5f) - CardHeight * 0.5f;

    private static readonly Color RailColor = new Color(0.055f, 0.075f, 0.11f, 1f);
    private static readonly Color RowColor = new Color(0.105f, 0.13f, 0.18f, 1f);

    /// <summary>One help entry: a short heading plus the explanation under it.</summary>
    private class Entry
    {
        public string title;
        public string body;
        public Color accent;

        /// <summary>Art shown left of the heading, matching the HUD (sword, bowl, lamp...).</summary>
        public Sprite icon;

        /// <summary>Fallback when the concept has no art: the letter the card prints (R/H/A/D/O) or a short tag.</summary>
        public string glyph;

        public Entry(string heading, string text, Color? colour = null)
        {
            title = heading;
            body = text;
            accent = colour ?? UiFactory.Accent;
        }

        public Entry WithIcon(Sprite sprite)
        {
            icon = sprite;
            return this;
        }

        public Entry WithGlyph(string text)
        {
            glyph = text;
            return this;
        }
    }

    /// <summary>One rail button plus the entries it shows.</summary>
    private class Section
    {
        public string label;
        public readonly List<Entry> entries = new List<Entry>();
    }

    private readonly List<Section> sections = new List<Section>();
    private readonly List<Button> railButtons = new List<Button>();
    private readonly List<GameObject> rows = new List<GameObject>();

    private RectTransform content;
    private RectTransform railRoot;
    private ScrollRect scroll;
    private TMP_Text heading;
    private TMP_Text counter;
    private int selectedSection;

    /// <summary>Builds the page (dim backdrop + card) under <paramref name="parent"/>.</summary>
    public static HelpPanelView Create(Transform parent, Action onClose)
    {
        var backdrop = UiFactory.CreateImage("HelpPanel", parent, new Color(0f, 0f, 0f, 0.82f));
        UiFactory.Stretch((RectTransform)backdrop.transform);

        var view = backdrop.gameObject.AddComponent<HelpPanelView>();
        view.Build(onClose);
        return view;
    }

    // ------------------------------------------------------------ construction

    private void Build(Action onClose)
    {
        BuildSections();

        var card = UiFactory.CreateImage("Card", transform, UiFactory.Panel);
        UiFactory.Center((RectTransform)card.transform, Vector2.zero, new Vector2(CardWidth, CardHeight));

        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = UiFactory.Border;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        BuildHeader(card.transform, onClose);
        BuildRail(card.transform);
        BuildContentPane(card.transform);

        ShowSection(0);
    }

    private void BuildHeader(Transform card, Action onClose)
    {
        var title = UiFactory.CreateText("Heading", card, "HOW TO PLAY", 40f, UiFactory.Accent,
            TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(28f, -22f);
        title.rectTransform.sizeDelta = new Vector2(520f, 52f);

        counter = UiFactory.CreateText("SectionCounter", card, "", 20f, UiFactory.DimText,
            TextAlignmentOptions.Right);
        counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(1f, 1f);
        counter.rectTransform.pivot = new Vector2(1f, 1f);
        counter.rectTransform.anchoredPosition = new Vector2(-208f, -34f);
        counter.rectTransform.sizeDelta = new Vector2(220f, 32f);

        var close = UiFactory.CreateButton(card, "CloseButton", "CLOSE", new Vector2(160f, 48f), onClose, 22f);
        close.GetComponent<RectTransform>().anchorMin = close.GetComponent<RectTransform>().anchorMax =
            new Vector2(1f, 1f);
        close.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
        close.GetComponent<RectTransform>().anchoredPosition = new Vector2(-28f, -24f);
    }

    private void BuildRail(Transform card)
    {
        var rail = UiFactory.CreateImage("Rail", card, RailColor);
        var railRect = (RectTransform)rail.transform;
        railRect.anchorMin = new Vector2(0f, 0f);
        railRect.anchorMax = new Vector2(0f, 1f);
        railRect.pivot = new Vector2(0f, 1f);
        railRect.anchoredPosition = new Vector2(16f, -88f);
        railRect.sizeDelta = new Vector2(RailWidth, -(88f + 16f));

        var layout = rail.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(10, 10, 12, 12);
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        railRoot = railRect;
        PopulateRail();
    }

    private void PopulateRail()
    {
        for (int i = 0; i < sections.Count; i++)
        {
            int index = i;
            var button = UiFactory.CreateButton(railRoot, "Section" + i, sections[i].label,
                new Vector2(RailWidth - 20f, 52f), () => ShowSection(index), 21f);

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.alignment = TextAlignmentOptions.Left;

            railButtons.Add(button);
        }
    }

    /// <summary>
    /// Re-resolves the HUD art and rebuilds the rail. The page is built from
    /// HudTopBar.Start, which can run before the roster spawns its cards - the
    /// sword and lamp pictures only exist once a card does, so opening the page
    /// refreshes it instead of showing a half-empty section.
    /// </summary>
    public void Rebuild()
    {
        foreach (var row in rows) Destroy(row);
        rows.Clear();

        for (int i = railButtons.Count - 1; i >= 0; i--)
        {
            if (railButtons[i] != null) Destroy(railButtons[i].gameObject);
        }
        railButtons.Clear();
        sections.Clear();

        BuildSections();
        PopulateRail();
        ShowSection(Mathf.Clamp(selectedSection, 0, sections.Count - 1));
    }

    private void BuildContentPane(Transform card)
    {
        heading = UiFactory.CreateText("SectionHeading", card, "", 30f, UiFactory.Accent,
            TextAlignmentOptions.Left);
        heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0f, 1f);
        heading.rectTransform.pivot = new Vector2(0f, 1f);
        heading.rectTransform.anchoredPosition = new Vector2(RailWidth + 52f, -96f);
        heading.rectTransform.sizeDelta = new Vector2(600f, 40f);

        var pane = UiFactory.CreateImage("Pane", card, new Color(1f, 1f, 1f, 0f));
        var paneRect = (RectTransform)pane.transform;
        paneRect.anchorMin = new Vector2(0f, 0f);
        paneRect.anchorMax = new Vector2(1f, 1f);
        paneRect.offsetMin = new Vector2(PaneLeft, PaneBottom);
        paneRect.offsetMax = new Vector2(-PaneRight, -PaneTop);
        pane.gameObject.AddComponent<RectMask2D>();

        var viewport = paneRect;

        content = UiFactory.CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll = pane.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 60f;

        BuildScrollbar(card, paneRect);
    }

    /// <summary>
    /// A thin drag handle beside the pane. Without it a section that runs past
    /// the fold looks truncated rather than scrollable.
    ///
    /// The track is pinned to the card's right edge with explicit sizes: copying
    /// the pane's stretch anchors would stretch the track across the whole card
    /// and tint every row behind a 6% white film.
    /// </summary>
    private void BuildScrollbar(Transform card, RectTransform pane)
    {
        var track = UiFactory.CreateImage("ScrollTrack", card, new Color(1f, 1f, 1f, 0.06f));
        var trackRect = (RectTransform)track.transform;
        trackRect.anchorMin = trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.anchoredPosition = new Vector2(-26f, PaneCenterY);
        trackRect.sizeDelta = new Vector2(8f, PaneHeight);

        var handle = UiFactory.CreateImage("Handle", trackRect, new Color(1f, 1f, 1f, 0.35f));
        var handleRect = (RectTransform)handle.transform;
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.None;

        if (scroll != null)
        {
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }

    // ---------------------------------------------------------------- sections

    private void ShowSection(int index)
    {
        if (index < 0 || index >= sections.Count) return;

        selectedSection = index;

        foreach (var row in rows) Destroy(row);
        rows.Clear();

        var section = sections[index];
        heading.text = section.label;
        counter.text = $"{index + 1} / {sections.Count}";

        foreach (var entry in section.entries) rows.Add(BuildRow(entry));

        for (int i = 0; i < railButtons.Count; i++) PaintRailButton(railButtons[i], i == index);

        content.anchoredPosition = Vector2.zero;
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private GameObject BuildRow(Entry entry)
    {
        var row = UiFactory.CreateImage("Row", content, RowColor);
        var rowRect = (RectTransform)row.transform;

        bool hasMark = entry.icon != null || !string.IsNullOrEmpty(entry.glyph);
        int textInset = hasMark ? 62 : 24;

        var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(textInset, 16, 10, 12);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Left accent bar: with six or seven cards stacked, the eye needs an
        // edge to land on, and the colour doubles as a severity cue.
        var accent = UiFactory.CreateImage("Accent", rowRect, entry.accent);
        var accentRect = (RectTransform)accent.transform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(5f, 0f);
        accent.raycastTarget = false;

        // The row's layout group would otherwise lay the bar out as a stacked
        // child; it is decoration pinned to the left edge, so it opts out.
        var accentLayout = accent.gameObject.AddComponent<LayoutElement>();
        accentLayout.ignoreLayout = true;

        if (hasMark) BuildMark(rowRect, entry);

        var title = UiFactory.CreateText("Title", rowRect, entry.title, 22f, entry.accent,
            TextAlignmentOptions.TopLeft);
        title.textWrappingMode = TextWrappingModes.Normal;
        title.overflowMode = TextOverflowModes.Overflow;

        var body = UiFactory.CreateText("Body", rowRect, entry.body, 19f, UiFactory.TextColor,
            TextAlignmentOptions.TopLeft);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;

        return row.gameObject;
    }

    /// <summary>
    /// The little badge left of a heading: real HUD art when the concept has
    /// one, otherwise the letter the unit card would print (R / H / A / D / O).
    /// Pinned, not laid out, so it never changes the row's height.
    /// </summary>
    private static void BuildMark(RectTransform rowRect, Entry entry)
    {
        var mark = UiFactory.CreateRect("Mark", rowRect);
        mark.anchorMin = mark.anchorMax = new Vector2(0f, 1f);
        mark.pivot = new Vector2(0f, 1f);
        mark.anchoredPosition = new Vector2(20f, -10f);
        mark.sizeDelta = new Vector2(30f, 30f);

        var ignore = mark.gameObject.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;

        if (entry.icon != null)
        {
            var image = mark.gameObject.AddComponent<Image>();
            image.sprite = entry.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Real art keeps its own colours - a green sword would read as a
            // different item than the one on the card.
            image.color = Color.white;
            return;
        }

        var text = UiFactory.CreateText("Glyph", mark, entry.glyph, 22f, entry.accent);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 22f;
    }

    private static void PaintRailButton(Button button, bool selected)
    {
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image != null) image.color = selected ? UiFactory.ChoiceSelected : UiFactory.ButtonIdle;

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = selected ? Color.white : UiFactory.DimText;
    }

    // -------------------------------------------------------------------- copy

    /// <summary>
    /// The page itself. Every number that also exists in a controller is read
    /// from that controller; the literals here are the prose around them.
    /// </summary>
    private void BuildSections()
    {
        sections.Clear();

        var work = BaseWorkController.Instance;
        var zones = GatherZoneDatabase.Instance;
        var days = DayManager.Instance;
        var resources = RunResources.Instance;

        int cookAp = work != null ? work.CookBaseApCost : 1;
        int cookMat = work != null ? work.CookMaterialPerRound : 1;
        int healMed = work != null ? work.HealBaseApCost(true) : 1;
        int healAp = work != null ? work.HealBaseApCost(false) : 3;
        int craftAp = work != null ? work.WorkshopBaseApCost(WorkshopMode.Medicine) : 1;
        int craftMat = work != null ? work.WorkshopBaseMaterialCost(WorkshopMode.Medicine) : 2;
        int potionAp = work != null ? work.WorkshopBaseApCost(WorkshopMode.BuffPotion) : 2;
        int potionMat = work != null ? work.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) : 4;
        int potionBonus = work != null ? work.BuffPotionBonusPerRound : 10;

        // HUD art, so a help row shows the exact picture the game shows. Cards
        // can still be spawning on the first build, hence the Rebuild() call when
        // the page opens; every lookup here tolerates a null.
        var status = FindAnyObjectByType<StatusBarView>();
        var roster = FindAnyObjectByType<UnitRosterView>();
        var confirm = FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        var skillPanel = FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        var node = FindAnyObjectByType<MapNodeView>();

        UnitIconView card = roster != null && roster.Cards.Count > 0 ? roster.Cards[0] : null;

        Sprite foodIcon = status != null ? status.FoodSprite : null;
        Sprite medkitIcon = status != null ? status.MedicineSprite : null;
        Sprite cookingIcon = status != null ? status.CookingMaterialSprite : null;
        Sprite materialsIcon = status != null ? status.MaterialsSprite : null;

        Sprite swordIcon = confirm != null && confirm.DispatchIcon != null
            ? confirm.DispatchIcon
            : card != null ? card.ReadySprite : PlaceholderIcons.Sword;
        Sprite apIcon = card != null ? card.ApPipSprite : null;
        Sprite caveIcon = node != null ? node.CaveSprite : PlaceholderIcons.Cave;

        Sprite hurtIcon = card != null ? card.HurtLampSprite : PlaceholderIcons.Plus;
        Sprite hungryIcon = card != null ? card.HungryLampSprite : PlaceholderIcons.Bowl;
        Sprite goodTraitIcon = card != null ? card.GoodTraitLampSprite : PlaceholderIcons.ArrowUp;
        Sprite badTraitIcon = card != null ? card.BadTraitLampSprite : PlaceholderIcons.ArrowDown;
        Sprite blessedIcon = card != null ? card.BlessedLampSprite : PlaceholderIcons.Star;
        Sprite potionIcon = card != null ? card.PotionLampSprite : PlaceholderIcons.Flask;

        Sprite cookIcon = skillPanel != null ? skillPanel.CookIcon : null;
        Sprite healIcon = skillPanel != null ? skillPanel.HealIcon : null;
        Sprite craftIcon = skillPanel != null ? skillPanel.CraftIcon : null;

        // 1 ------------------------------------------------------------- basics
        var basics = new Section { label = "1  BASICS" };
        basics.entries.Add(new Entry("PICK A UNIT",
            "CLICK A CARD IN THE HAND AT THE BOTTOM TO SELECT IT. A SELECTED CARD LIFTS UP AND ITS AP PIPS LIGHT.")
            .WithIcon(swordIcon));
        basics.entries.Add(new Entry("PICK A DUNGEON",
            "CLICK ONE OF THE THREE CAVE CARDS. ITS SUCCESS VALUE POPS BIGGER AND THE SELECTION MARKER APPEARS ABOVE IT.")
            .WithIcon(caveIcon));
        basics.entries.Add(new Entry("SEND THE CREW",
            "TWO UNITS MAXIMUM PER TRIP. EACH UNIT PAYS THE ZONE'S AP COST AND SPENDS 1 READINESS. A UNIT WITH NO READINESS CANNOT GO.")
            .WithIcon(swordIcon));
        basics.entries.Add(new Entry("USE A SKILL",
            "ONE UNIT AT A TIME. COOK AND CRAFT QUEUE ROUNDS (CLICK AGAIN, RIGHT CLICK TO CLEAR); HEAL WAITS FOR A HURT PATIENT.")
            .WithIcon(cookIcon));
        basics.entries.Add(new Entry("CONFIRM",
            "ONE BUTTON IN THE BOTTOM RIGHT DOES EVERYTHING. IT SHOWS THE LABEL AND WHAT IT WILL SPEND: AP FOR A SKILL, AP PLUS ONE READINESS PER UNIT FOR A TRIP.")
            .WithGlyph(">"));
        basics.entries.Add(new Entry("END THE DAY",
            $"CLICK THE DAY FRAME, TOP RIGHT. FOOD IS PAID FOR EVERYONE THEN AP AND READINESS REFRESH. YOU HAVE {Deadline()} DAYS.")
            .WithGlyph("D7"));
        basics.entries.Add(new Entry("WIN THE RUN",
            "FIND THE BOX AND THE KEY, THEN THE RUN ENDS IN A WIN IMMEDIATELY - NO NEED TO SURVIVE TO THE LAST DAY.",
            UiFactory.Success).WithGlyph("W"));
        basics.entries.Add(new Entry("LOSE THE RUN",
            $"THREE WAYS OUT: THE DEADLINE PASSES WITH NO CORE ITEMS, EVERY UNIT DIES, OR EVERY UNIT IS HURT AT ONCE - HEALING NEEDS A HEALTHY HEALER, SO THAT LAST ONE CANNOT BE UNDONE. YOU HAVE {Deadline()} DAYS.",
            UiFactory.Fail).WithGlyph("X"));

        // 2 ---------------------------------------------------------- resources
        var res = new Section { label = "2  RESOURCES" };
        res.entries.Add(new Entry($"FOOD  (STOCK {Stock(resources, r => r.Food)})",
            "1 PER UNIT PER DAY, PAID WHEN YOU END THE DAY IN ROSTER ORDER. UNITS OUT ON A TRIP DO NOT EAT. RUNNING OUT ADDS A STARVATION STACK.")
            .WithIcon(foodIcon));
        res.entries.Add(new Entry($"MEDKIT  (STOCK {Stock(resources, r => r.Medicine)})",
            "SPENT BY HEAL: 1 MEDKIT TURNS A HURT UNIT BACK TO READINESS.")
            .WithIcon(medkitIcon));
        res.entries.Add(new Entry($"COOKING MATERIAL  (STOCK {Stock(resources, r => r.CookingMaterial)})",
            "SPENT BY COOK. THE EASY CAVE IS THE CHEAPEST PLACE TO FARM IT.")
            .WithIcon(cookingIcon));
        res.entries.Add(new Entry($"MATERIALS  (STOCK {Stock(resources, r => r.Materials)})",
            "SPENT BY CRAFT FOR MEDKITS AND BUFF POTIONS. THE HARD CAVE IS THE CHEAPEST PLACE TO FARM IT.")
            .WithIcon(materialsIcon));
        res.entries.Add(new Entry("ACTION POINTS  (AP)",
            "EVERY UNIT HAS 3 PER DAY. DUNGEONS AND SKILLS BOTH COST AP, AND STARVATION ADDS TO THAT COST.")
            .WithIcon(apIcon));
        res.entries.Add(new Entry("READINESS  (1 PER UNIT, PER DAY)",
            "THE DISPATCH TICKET. ENTERING A CAVE SPENDS THE UNIT'S READINESS ON TOP OF THE AP, AND A UNIT WITH NO READINESS CANNOT BE SENT. " +
            "IT WORKS LIKE AP: ONE PER UNIT, REFRESHED EVERY DAY, AND BASE WORK DOES NOT USE IT.")
            .WithIcon(swordIcon));

        // 3 -------------------------------------------------------------- units
        var units = new Section { label = "3  UNITS" };
        units.entries.Add(new Entry("ANN", "EASY CAVE NATURAL. TALENT: HEAL - ANYONE SHE HEALS GAINS BLESSED.")
            .WithGlyph("E").WithIcon(healIcon));
        units.entries.Add(new Entry("BOON", "HARD CAVE NATURAL. TALENT: CRAFTING - MEDICINE HE CRAFTS IS DOUBLED.")
            .WithGlyph("H").WithIcon(craftIcon));
        units.entries.Add(new Entry("CIA", "NORMAL CAVE NATURAL. TALENT: COOKING - FOOD SHE COOKS IS DOUBLED.")
            .WithGlyph("N").WithIcon(cookIcon));
        units.entries.Add(new Entry("DAN", "HARD CAVE NATURAL. TALENT: EXPLORING - TRIP LOOT IS DOUBLED, CORE ITEMS EXCLUDED.")
            .WithGlyph("H").WithIcon(cookingIcon));
        units.entries.Add(new Entry($"APTITUDE  (+{GatherResolver.AptitudeBonus})",
            $"THE BIG LETTER ON THE CARD IS THE CAVE TIER THIS UNIT IS A NATURAL AT. SENDING IT THERE ADDS +{GatherResolver.AptitudeBonus} SUCCESS VALUE ONCE PER TRIP.")
            .WithGlyph("E"));
        units.entries.Add(new Entry($"EVERY TIER AWAY  (-{GatherResolver.AptitudeMismatchPerTier})",
            $"A UNIT SENT ONE TIER AWAY COSTS {GatherResolver.AptitudeMismatchPerTier} SUCCESS VALUE, TWO TIERS COST {GatherResolver.AptitudeMismatchPerTier * 2}. IT IS NOT A BAN - JUST A TAX.")
            .WithGlyph("N"));
        units.entries.Add(new Entry("READING A CARD",
            "TOP LEFT: READINESS OR STATE LETTER. TOP RIGHT: APTITUDE LETTER, TALENT ICON UNDER IT. BOTTOM LEFT: NAME AND AP PIPS. BOTTOM RIGHT: SIX BUFF LAMPS. HOVER ANY OF THEM FOR ITS OWN EXPLANATION.")
            .WithIcon(swordIcon));

        // 4 ------------------------------------------------------------- states
        var states = new Section { label = "4  STATES" };
        states.entries.Add(new Entry("READINESS", "HAS ITS READINESS, SO IT CAN GO ON A TRIP. BASE WORK IS ALLOWED TOO.", UiFactory.Success)
            .WithIcon(swordIcon));
        states.entries.Add(new Entry("READINESS SPENT", "THE READINESS BADGE GOES DIM FOR THE REST OF THE DAY, SO NO SECOND TRIP. BASE WORK IS STILL FINE.", UiFactory.DimText)
            .WithGlyph("0"));
        states.entries.Add(new Entry("RESTING", "OUT OF AP. COMES BACK AT THE NEXT DAY.", UiFactory.DimText)
            .WithGlyph("R"));
        states.entries.Add(new Entry("HURT", "CANNOT ACT AT ALL. HEAL IT - IT DOES NOT CLEAR ON ITS OWN. IF EVERY UNIT IS HURT AT ONCE THE RUN IS LOST, BECAUSE NOBODY IS LEFT TO HEAL ANYONE.", UiFactory.Fail)
            .WithIcon(hurtIcon));
        states.entries.Add(new Entry("AWAY", "MID TRIP. DOES NOT EAT THAT NIGHT.", UiFactory.DimText)
            .WithGlyph("A"));
        states.entries.Add(new Entry("DEATH", "GONE FOR THE REST OF THE RUN. ONLY STARVATION GETS HERE - A ROLL NEVER KILLS.", UiFactory.Fail)
            .WithGlyph("D"));
        states.entries.Add(new Entry("HUNGRY", "ONE LAMP PER STARVATION STACK. EVERY ACTION COSTS +1 AP MORE, AND A FULL METER KILLS THE UNIT.", UiFactory.Fail)
            .WithIcon(hungryIcon));
        states.entries.Add(new Entry($"TRAIT GOOD  (+{GatherResolver.TraitBonus})", "PERMANENT, UNTIL THE RUN ENDS. A HARD SUCCESS CAN AWARD ONE (50%). COUNTED ONCE PER TRIP EVEN IF TWO UNITS HAVE ONE.", UiFactory.Success)
            .WithIcon(goodTraitIcon));
        states.entries.Add(new Entry($"TRAIT BAD  (-{GatherResolver.TraitBonus})", "PERMANENT. A FAILURE CAN AWARD ONE (50%), A FUMBLE ALWAYS DOES. ALSO COUNTED ONCE PER TRIP.", UiFactory.Fail)
            .WithIcon(badTraitIcon));
        states.entries.Add(new Entry($"BLESSED  (+{GatherResolver.BlessedBonus})", "GRANTED BY A HEAL-TALENT MEDIC. COVERS THE NEXT TRIP ONLY, THEN BURNS OFF.", UiFactory.Success)
            .WithIcon(blessedIcon));
        states.entries.Add(new Entry($"BUFF POTION  (+{potionBonus} PER STACK)", "CRAFTED AT THE WORKSHOP. COUNTS FOR THE WHOLE CREW FOR THE REST OF THE RUN AND STACKS.", UiFactory.Success)
            .WithIcon(potionIcon));
        states.entries.Add(new Entry("STARVING",
            $"EVERY ACTION COSTS +1 AP PER STACK, SO 2 STACKS MAKE A 3 AP CAVE COST 5 AND THE UNIT CANNOT AFFORD IT. AT {UnitCharacter.MaxStarvationStacks} STACKS THE UNIT DIES.", UiFactory.Fail)
            .WithIcon(hungryIcon));

        // 5 ---------------------------------------------------------- dungeons
        var dungeons = new Section { label = "5  DUNGEONS" };
        dungeons.entries.Add(new Entry(ZoneLine(zones, GatherZone.Easy),
            "THE FOOD RUN. CHEAPEST TRAVEL AND THE HIGHEST SUCCESS VALUE, SO THIS IS WHERE YOU FARM COOKING MATERIAL EARLY.", UiFactory.Success)
            .WithIcon(caveIcon));
        dungeons.entries.Add(new Entry(ZoneLine(zones, GatherZone.Normal),
            "THE MEDKIT RUN, AND THE ONLY CAVE THAT GIVES UP THE KEY.", UiFactory.Accent)
            .WithIcon(caveIcon));
        dungeons.entries.Add(new Entry(ZoneLine(zones, GatherZone.Hard),
            "THE MATERIAL RUN, AND THE ONLY CAVE THAT GIVES UP THE BOX. ALSO THE CHEAPEST PLACE TO FUMBLE, SO SEND THE NATURALS.", UiFactory.Fail)
            .WithIcon(caveIcon));
        dungeons.entries.Add(new Entry("CORE ITEMS  (KEY + BOX)",
            "EACH PIECE IS BOUND TO ONE CAVE AND DROPS ON THAT CAVE'S HARD SUCCESS, GUARANTEED - NO DICE, NO CHANCE. NORMAL HANDS OVER THE KEY, HARD HANDS OVER THE BOX, EASY HANDS OVER NOTHING. REGULAR SUCCESS, FAILURE AND FUMBLE NEVER DROP ONE. A PIECE YOU ALREADY HOLD IS NOT ROLLED FOR.")
            .WithGlyph("B"));
        dungeons.entries.Add(new Entry("JACKPOT  (SV 80+)",
            $"WHILE THE SUCCESS VALUE IS {GatherDispatchController.JackpotSuccessValue} OR MORE THE FOG IS THIN ENOUGH TO SEE BOTH CACHES: A NATURAL {GatherDispatchController.JackpotRoll} TAKES WHATEVER PIECE IS STILL MISSING, ON TOP OF THE CAVE'S OWN DROP.")
            .WithGlyph("1"));
        dungeons.entries.Add(new Entry("CLEARING THE FOG",
            "A CLEARED TRIP BURNS RISK - 5 AT EASY, 10 AT NORMAL, 15 AT HARD - AND THINNER FOG RAISES EVERY SUCCESS VALUE. EASY AND NORMAL SHAKE THE FOG OFF COMPLETELY BEFORE HARD DOES, SO WORK YOUR WAY BACK OUT. TRADE TRIPS FOR AIR.")
            .WithGlyph("-"));

        // 6 ------------------------------------------------------------ skills
        var skills = new Section { label = "6  SKILLS" };
        skills.entries.Add(new Entry($"COOK  ({cookAp} AP + {cookMat} MATERIAL)",
            "MAKES 1 FOOD PER ROUND. QUEUE AS MANY ROUNDS AS AP AND MATERIAL ALLOW. CIA MAKES 2.")
            .WithIcon(cookIcon));
        skills.entries.Add(new Entry($"HEAL  MED  ({healMed} AP + 1 MEDKIT)",
            "TURNS ONE HURT UNIT BACK TO READINESS. THE CHEAPEST MODE WHILE MEDKITS LAST.")
            .WithIcon(healIcon));
        skills.entries.Add(new Entry($"HEAL  AP ONLY  ({healAp} AP)",
            "SAME RESULT WITH NO MEDKIT. THE MODE BUTTON FLIPS BETWEEN THE TWO AND IS DEAD WHEN THE SHELF IS EMPTY.")
            .WithIcon(healIcon));
        skills.entries.Add(new Entry($"CRAFT  MEDKIT  ({craftAp} AP + {craftMat} MATERIALS)",
            "MAKES 1 MEDKIT. BOON MAKES 2.")
            .WithIcon(craftIcon));
        skills.entries.Add(new Entry($"CRAFT  BUFF POTION  ({potionAp} AP + {potionMat} MATERIALS)",
            $"ADDS +{potionBonus} SUCCESS VALUE TO EVERY FUTURE TRIP FOR THE REST OF THE RUN. STACKS, SO IT IS THE LONG GAME.")
            .WithIcon(craftIcon));
        skills.entries.Add(new Entry("NO DAILY LIMIT",
            "BASE WORK IS NOT ONCE-A-DAY. THE SAME UNIT CAN COOK AND STILL BE SENT OUT, AS LONG AS IT HAS AP AND ITS READINESS LEFT.")
            .WithGlyph("+"));

        // 7 ----------------------------------------------------- success value
        var sv = new Section { label = "7  SUCCESS VALUE" };
        sv.entries.Add(new Entry($"BASE VALUE PER CAVE  ({BaseValue(zones)})",
            "EACH CAVE HAS ITS OWN CEILING BEFORE ANYONE JOINS. ROLL 1 TO 100 AND ROLL UNDER IT; EASY IS FRIENDLY, HARD IS NOT.")
            .WithIcon(caveIcon));
        sv.entries.Add(new Entry($"APTITUDE  (+{GatherResolver.AptitudeBonus} ONCE)", "ONE NATURAL AT THE ZONE'S TIER IS ENOUGH - SENDING TWO DOES NOT DOUBLE IT.")
            .WithGlyph("E"));
        sv.entries.Add(new Entry("MISMATCH PER UNIT", $"-{GatherResolver.AptitudeMismatchPerTier} FOR EVERY TIER BETWEEN A UNIT'S LETTER AND THE CAVE. EACH UNIT COUNTS SEPARATELY.")
            .WithGlyph("-"));
        sv.entries.Add(new Entry($"DUO  (+{GatherResolver.DuoBonus})", "SENDING TWO UNITS INSTEAD OF ONE. ALSO TWO CHANCES TO SURVIVE A FAILURE'S INJURY, SINCE IT PICKS ONE VICTIM.")
            .WithGlyph("2"));
        sv.entries.Add(new Entry("RISK  (THE RED FOG)",
            $"STARTS AT {RunResources.MaxRisk} AND IS SUBTRACTED FROM EVERY TRIP - BUT EACH CAVE SHRUGS OFF A HANDICAP FIRST: " +
            $"EASY {GatherResolver.ZoneHandicap(GatherZone.Easy)}, NORMAL {GatherResolver.ZoneHandicap(GatherZone.Normal)}, " +
            $"HARD {GatherResolver.ZoneHandicap(GatherZone.Hard)}. THE SUM IS -RISK + HANDICAP AND NEVER GOES BELOW 0, " +
            "SO EASY STOPS FEELING THE FOG AT RISK 30 AND NORMAL AT 20 WHILE HARD PAYS UNTIL RISK IS 0. " +
            "THE FOG AND THE GAUGE TO THE LEFT OF THE CAVES ARE THE SAME NUMBER.")
            .WithGlyph("R"));
        sv.entries.Add(new Entry("TRAITS, BLESSED, POTIONS", $"+{GatherResolver.TraitBonus} GOOD TRAIT, -{GatherResolver.TraitBonus} BAD TRAIT (ONCE EACH PER TRIP), +{GatherResolver.BlessedBonus} PER BLESSED UNIT, PLUS THE PARTY POTION TOTAL.")
            .WithIcon(goodTraitIcon));

        // 8 ------------------------------------------------------------- roll
        var roll = new Section { label = "8  THE ROLL" };
        roll.entries.Add(new Entry("HOW IT WORKS",
            "THE NUMBER ON THE CAVE CARD IS THE ONE TO BEAT. YOU ROLL 1 TO 100 AND HAVE TO ROLL UNDER IT - LOWER IS BETTER. THE BAR IN THE DICE POPUP SHOWS THE FOUR BANDS BEFORE THE DICE LAND.",
            UiFactory.TextColor).WithGlyph("D"));
        roll.entries.Add(new Entry("HARD SUCCESS  (TOP THIRD)",
            "ROLL AT OR UNDER SV / 3. FULL LOOT PLUS ONE BONUS PICK, A 50% CHANCE OF A GOOD TRAIT, AND THE ONLY TIER THAT CAN DROP A CORE ITEM.",
            UiFactory.Success).WithGlyph("H"));
        roll.entries.Add(new Entry("REGULAR SUCCESS", "ROLL AT OR UNDER THE SV. MAIN LOOT ONLY.", UiFactory.Success)
            .WithGlyph("R"));
        roll.entries.Add(new Entry("FAILURE", "ROLL OVER THE SV. HALF THE MAIN LOOT (ROUNDED DOWN, OFTEN ZERO), ONE RANDOM UNIT HURT, AND A 50% CHANCE OF A BAD TRAIT.", UiFactory.Fail)
            .WithGlyph("F"));
        roll.entries.Add(new Entry("FUMBLE", "ROLL 100 - OR 96 TO 100 WHEN THE SV IS UNDER 50. NO LOOT, EVERY UNIT ON THE TRIP IS HURT, AND EVERY ONE OF THEM TAKES A BAD TRAIT.", UiFactory.Fail)
            .WithGlyph("X"));
        roll.entries.Add(new Entry("THE 50 LINE",
            "FUMBLE IS 1% AT SV 50 OR MORE BUT JUMPS TO 5% BELOW IT. DRAGGING THE VALUE UNDER 50 IS THE MOST EXPENSIVE MISTAKE IN THE GAME.",
            UiFactory.Fail).WithGlyph("50"));

        sections.Add(basics);
        sections.Add(res);
        sections.Add(units);
        sections.Add(states);
        sections.Add(dungeons);
        sections.Add(skills);
        sections.Add(sv);
        sections.Add(roll);
    }

    private static string Deadline()
    {
        var days = DayManager.Instance;
        return days != null ? days.DeadlineDay.ToString() : "7";
    }

    private static string Stock(RunResources resources, Func<RunResources, int> read)
    {
        return resources != null ? read(resources).ToString() : "0";
    }

    private static string BaseValue(GatherZoneDatabase zones)
    {
        if (zones == null) return "EASY 80 / NORMAL 55 / HARD 30";

        return $"EASY {Value(zones, GatherZone.Easy)} / NORMAL {Value(zones, GatherZone.Normal)} / HARD {Value(zones, GatherZone.Hard)}";
    }

    private static int Value(GatherZoneDatabase zones, GatherZone zone)
    {
        var config = zones.GetConfig(zone);
        return config != null ? config.baseSuccessValue : 0;
    }

    /// <summary>"EASY CAVE - 1 AP - SV 80 - HANDICAP 30 - LOOT COOKING MATERIAL X2" for one rail row.</summary>
    private static string ZoneLine(GatherZoneDatabase zones, GatherZone zone)
    {
        var config = zones != null ? zones.GetConfig(zone) : null;
        if (config == null) return zone.ToString().ToUpperInvariant() + " CAVE";

        return $"{zone.ToString().ToUpperInvariant()} CAVE  -  {config.apCost} AP  -  SV {config.baseSuccessValue}  -  " +
               $"HANDICAP {GatherResolver.ZoneHandicap(zone)}  -  {ItemName(config.mainLoot.type)} X{config.mainLoot.amount}";
    }

    private static string ItemName(InventoryItemType type)
    {
        switch (type)
        {
            case InventoryItemType.CookingMaterial: return "COOKING MATERIAL";
            case InventoryItemType.Medicine: return "MEDKIT";
            case InventoryItemType.Materials: return "MATERIALS";
            default: return "LOOT";
        }
    }
}
