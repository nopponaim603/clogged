using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One unit card in the hand. The layout follows the sprint sketch:
///
///   +---------------------+
///   | [state]        [E]  |   state icon (sword = ready) / aptitude letter + talent icon
///   |                     |
///   |      portrait       |
///   |                     |
///   +----------+----------+
///   | name     |  buffs   |   AP pips under the name, buff/debuff lamps on the right
///   +----------+----------+
///
/// The whole chrome is built in code so the prefab only supplies the portrait
/// sprite. Icons are generated placeholders (pixel masks) so the card reads
/// correctly before the real art exists - drop sprites into the matching
/// [SerializeField] slots later and they take over.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UnitIconView : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs (from prefab)")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Reused as the state letter shown when the unit is not ready (R/H/A/M).")]
    [SerializeField] private TMP_Text statusText;

    [Header("Card Layout")]
    [SerializeField] private Vector2 cardSize = new Vector2(200f, 300f);
    [SerializeField] private Color cardBackground = new Color(0.07f, 0.09f, 0.14f, 1f);
    [SerializeField] private float padding = 8f;
    [SerializeField] private float portraitHeight = 130f;

    [Header("State Colours")]
    [SerializeField] private Color hurtBackground = new Color(0.42f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color hungryBackground = new Color(0.42f, 0.26f, 0.08f, 1f);

    [Header("Frame")]
    [SerializeField] private float frameThickness = 4f;
    [SerializeField] private float selectedFrameThickness = 7f;
    [SerializeField] private float unselectedFrameAlpha = 0.6f;
    [SerializeField] private float unavailableFrameAlpha = 0.2f;
    [SerializeField] private float selectedScale = 1.06f;

    [Header("Heal Target")]
    [Tooltip("Colour of a card that can be picked as a heal patient. Mint so it reads against every unit accent.")]
    [SerializeField] private Color targetableColor = new Color(0.55f, 1f, 0.75f, 1f);
    [SerializeField] private float targetableFrameThickness = 7f;
    [Tooltip("Slow pulse so the eligible patients catch the eye even before the player looks at the hand.")]
    [SerializeField] private float targetablePulseSpeed = 3.2f;
    [SerializeField] private float targetablePulseMinAlpha = 0.55f;

    [Header("AP Pips")]
    [Tooltip("One pip per action point. Available = still unspent, pending = the action about to be confirmed will eat it, spent = already gone.")]
    [SerializeField] private Sprite apPipAvailable;
    [SerializeField] private Sprite apPipPending;
    [SerializeField] private Sprite apPipSpent;
    [SerializeField] private float apPipWidth = 18f;
    [SerializeField] private float apPipHeight = 22f;
    [SerializeField] private float apPipSpacing = 4f;
    [SerializeField] private float apPipRowHeight = 22f;

    [Header("Draw Animation")]
    [Tooltip("How far below its slot an unselected card rests. Selecting it slides the card up, like lifting it out of a hand.")]
    [SerializeField] private float restingOffset = 70f;
    [SerializeField] private float liftSpeed = 900f;

    [Header("Icons (optional - generated placeholders are used when empty)")]
    [SerializeField] private Sprite swordSprite;
    [SerializeField] private Sprite hurtIcon;
    [SerializeField] private Sprite hungryIcon;
    [SerializeField] private Sprite positiveTraitIcon;
    [SerializeField] private Sprite negativeTraitIcon;
    [SerializeField] private Sprite blessedIcon;
    [SerializeField] private Sprite buffPotionIcon;

    [Header("Buff Lamp Colours")]
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color hungryColor = new Color(1f, 0.62f, 0.22f, 1f);
    [SerializeField] private Color goodColor = new Color(0.45f, 0.80f, 1f, 1f);
    [SerializeField] private Color buffPotionColor = new Color(0.72f, 0.55f, 1f, 1f);
    [SerializeField] private Color lampOffColor = new Color(0.35f, 0.36f, 0.40f, 0.55f);

    private static readonly Color EasyColor = new Color(0.45f, 0.75f, 0.45f);
    private static readonly Color NormalColor = new Color(0.85f, 0.75f, 0.30f);
    private static readonly Color HardColor = new Color(0.80f, 0.30f, 0.30f);

    /// <summary>A sword that has been spent this day: still readable, clearly unusable.</summary>
    private static readonly Color SpentSwordColor = new Color(1f, 1f, 1f, 0.28f);

    private CanvasGroup group;
    private UnitCharacter unit;
    private Color accent = Color.white;
    private bool selectable;
    private bool selectionEnabled = true;
    private bool chromeBuilt;
    private bool targetable;
    private float appliedThickness = -1f;
    private int pendingApCost;
    private bool apCostPayer;

    private RectTransform lift;
    private Image background;
    private Image aptitudeBadge;
    private TMP_Text aptitudeLetter;
    private Image talentIcon;
    private Image stateIcon;
    private readonly List<Image> frame = new List<Image>();
    private readonly List<Image> apPips = new List<Image>();
    private RectTransform apPipRow;
    private readonly List<Image> lampIcons = new List<Image>();

    public event Action<UnitIconView> Clicked;

    public UnitCharacter Unit => unit;
    public bool IsSelected { get; private set; }
    public bool CanBeSelected => selectable && selectionEnabled && unit != null;

    /// <summary>
    /// Base AP cost of the action being set up right now, or 0 when this card
    /// is not the one that will pay. Only the payer previews what it will spend.
    /// </summary>
    public int ApCostPreview => (IsSelected || apCostPayer) ? pendingApCost : 0;

    /// <summary>
    /// How many action points the action the player is currently setting up will
    /// spend, before this unit's starvation surcharge. 0 clears the preview.
    /// </summary>
    public void SetApCostPreview(int baseApCost)
    {
        pendingApCost = Mathf.Max(0, baseApCost);
    }

    /// <summary>
    /// Marks the card that will actually pay the pending cost. Used while
    /// healing: the selected card is the patient, but the medic pays the AP.
    /// </summary>
    public void SetApCostPayer(bool value)
    {
        apCostPayer = value;
    }

    /// <summary>True while this card is a legal target for the action being aimed (e.g. a heal patient).</summary>
    public bool IsTargetable => targetable;

    private void Update()
    {
        AnimateLift();

        // Only the eligible targets animate, so idle cards cost nothing.
        if (!targetable || !chromeBuilt) return;

        float pulse = Mathf.Lerp(targetablePulseMinAlpha, 1f,
            (Mathf.Sin(Time.unscaledTime * targetablePulseSpeed) + 1f) * 0.5f);

        Color color = targetableColor;
        color.a = IsSelected ? 1f : pulse;
        for (int i = 0; i < frame.Count; i++) frame[i].color = color;
    }

    private void AnimateLift()
    {
        if (lift == null) return;

        float target = IsSelected ? 0f : -restingOffset;
        Vector2 pos = lift.anchoredPosition;
        if (Mathf.Approximately(pos.y, target)) return;

        pos.y = Mathf.MoveTowards(pos.y, target, liftSpeed * Time.unscaledDeltaTime);
        lift.anchoredPosition = pos;
    }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    /// <summary>Lets the hand panel drive the card size so the chrome always fits it.</summary>
    public void SetCardSize(Vector2 size)
    {
        if (size.x > 0f && size.y > 0f) cardSize = size;
    }

    /// <summary>Binds this card to a unit and paints its frame in the unit's accent color.</summary>
    public void Bind(UnitCharacter boundUnit, Color accentColor, bool makeSelectable)
    {
        unit = boundUnit;
        accent = accentColor;
        selectable = makeSelectable;

        BuildCardChrome();
        ApplySelectionScale();
        ApplyFrame();
        Refresh();
    }

    // ------------------------------------------------------------- tooltips

    /// <summary>
    /// Each badge answers for itself instead of one popup covering the whole
    /// card: the letter explains the aptitude, the icon under it explains the
    /// talent, the corner slot explains the state, and every lamp explains the
    /// condition it watches. All providers read the live unit, so a tooltip is
    /// never a frame behind the card.
    /// </summary>
    private HudTooltipContent StateTooltip()
    {
        if (unit == null) return null;

        if (unit.state == UnitState.Idle && unit.HasSword)
        {
            return new HudTooltipContent("READY",
                $"HOLDS {unit.currentSwords} SWORD: THAT IS THE DISPATCH TICKET INTO A CAVE. " +
                "BASE WORK DOES NOT SPEND IT. ONE TRIP PER DAY, BASE WORK UNLIMITED.");
        }

        if (unit.state == UnitState.Idle && !unit.HasSword)
        {
            return new HudTooltipContent("SWORD SPENT",
                "THE DISPATCH SWORD IS USED UP FOR TODAY, SO NO SECOND TRIP. " +
                "BASE WORK IS STILL ALLOWED. IT REFRESHES WITH AP AT THE NEXT DAY.");
        }

        switch (unit.state)
        {
            case UnitState.Resting:
                return new HudTooltipContent("RESTING",
                    "OUT OF ACTION POINTS. COMES BACK AT THE START OF THE NEXT DAY.");
            case UnitState.Hurt:
                return new HudTooltipContent("HURT",
                    "INJURED, CANNOT ACT. HEAL IT: 1 MEDICINE + 1 AP, OR 3 AP WITHOUT MEDICINE.");
            case UnitState.Away:
                return new HudTooltipContent("AWAY",
                    "OUT ON A TRIP. RETURNS WHEN THE ROLL RESOLVES.");
            case UnitState.Death:
                return new HudTooltipContent("DEATH",
                    "DEAD. GONE FOR THE REST OF THE RUN. THREE STARVATION STACKS KILL A UNIT.");
            default:
                return new HudTooltipContent("STATE", StateName(unit.state));
        }
    }

    private HudTooltipContent AptitudeTooltip()
    {
        if (unit == null) return null;

        string tier = AptitudeName(unit.primaryAptitude);
        string letter = AptitudeLetter(unit.primaryAptitude);

        return new HudTooltipContent($"APTITUDE {letter} - {tier}",
            $"THIS UNIT IS A NATURAL AT {tier}: +{GatherResolver.AptitudeBonus} SUCCESS VALUE WHEN IT JOINS A TRIP THERE.\n" +
            $"ONE TIER AWAY COSTS {GatherResolver.AptitudeMismatchPerTier}, TWO TIERS COST {GatherResolver.AptitudeMismatchPerTier * 2}.");
    }

    private HudTooltipContent TalentTooltip()
    {
        if (unit == null) return null;

        switch (unit.talent)
        {
            case UnitTalent.Heal:
                return new HudTooltipContent("TALENT - HEAL",
                    "ANYONE THIS UNIT HEALS GAINS BLESSED: " +
                    $"+{GatherResolver.BlessedBonus} SUCCESS VALUE ON THEIR NEXT TRIP.");
            case UnitTalent.Crafting:
                return new HudTooltipContent("TALENT - CRAFTING",
                    "MEDICINE THIS UNIT CRAFTS AT THE WORKSHOP IS DOUBLED.");
            case UnitTalent.Cooking:
                return new HudTooltipContent("TALENT - COOKING",
                    "FOOD THIS UNIT COOKS IS DOUBLED.");
            case UnitTalent.Exploring:
                return new HudTooltipContent("TALENT - EXPLORING",
                    "LOOT FROM A TRIP IS DOUBLED, CORE ITEMS EXCLUDED, AND THE CORE ITEM CHANCE IS +25%.");
            default:
                return new HudTooltipContent("TALENT", "THIS UNIT HAS NO SECONDARY SPECIALTY.");
        }
    }

    /// <summary>One lamp's worth of explanation, with whether it is lit right now.</summary>
    private HudTooltipContent LampTooltip(int index)
    {
        if (unit == null) return null;

        switch (index)
        {
            case 0:
                return new HudTooltipContent("HURT",
                    unit.state == UnitState.Hurt
                        ? "HURT. CANNOT ACT UNTIL HEALED."
                        : "NOT HURT. A FAILED TRIP CAN INJURE ONE UNIT.");
            case 1:
                return new HudTooltipContent("HUNGRY",
                    unit.starvationStacks > 0
                        ? $"STARVING X{unit.starvationStacks}: EVERY ACTION COSTS +{unit.starvationStacks} AP. THREE STACKS AND THE UNIT DIES."
                        : "FED. MISSING THE END-DAY MEAL ADDS A STARVATION STACK.");
            case 2:
                return new HudTooltipContent("GOOD TRAIT",
                    HasTrait(TraitPolarity.Positive)
                        ? $"+{GatherResolver.TraitBonus} SUCCESS VALUE ON A TRIP (COUNTED ONCE PER TEAM)."
                        : "NO POSITIVE TRAIT YET. A HARD SUCCESS CAN AWARD ONE.");
            case 3:
                return new HudTooltipContent("BAD TRAIT",
                    HasTrait(TraitPolarity.Negative)
                        ? $"-{GatherResolver.TraitBonus} SUCCESS VALUE ON A TRIP (COUNTED ONCE PER TEAM)."
                        : "NO NEGATIVE TRAIT. A FUMBLE CAN ADD ONE.");
            case 4:
                return new HudTooltipContent("BLESSED",
                    unit.blessed
                        ? $"+{GatherResolver.BlessedBonus} SUCCESS VALUE ON THE NEXT TRIP, THEN THE BLESSING IS USED UP."
                        : "NOT BLESSED. A MEDIC WITH THE HEAL TALENT GRANTS IT.");
            default:
                return new HudTooltipContent("BUFF POTION",
                    RunResources.Instance != null && RunResources.Instance.PartySuccessBonus > 0
                        ? $"THE CREW IS AT +{RunResources.Instance.PartySuccessBonus} SUCCESS VALUE FROM POTIONS. STACKS ALL RUN."
                        : "NO PARTY BONUS YET. CRAFT A BUFF POTION AT THE WORKSHOP.");
        }
    }

    private bool HasTrait(TraitPolarity polarity)
    {
        if (unit == null) return false;

        foreach (var trait in unit.traits)
        {
            if (trait != null && trait.polarity == polarity) return true;
        }
        return false;
    }

    /// <summary>One pip: what that single action point is doing right now.</summary>
    private HudTooltipContent ApPipTooltip(int index)
    {
        if (unit == null) return null;

        int total = Mathf.Max(1, unit.maxActionPoints);
        int left = Mathf.Clamp(unit.currentActionPoints, 0, unit.maxActionPoints);
        int spending = ApCostPreview > 0 ? Mathf.Min(unit.GetActualApCost(ApCostPreview), left) : 0;

        string state = index >= left ? "ALREADY SPENT."
            : index >= left - spending ? "WILL BE SPENT BY THE ACTION YOU ARE SETTING UP."
            : "STILL AVAILABLE.";

        return new HudTooltipContent($"ACTION POINT {index + 1}/{total}",
            $"{state}\nEVERY ACTION PAYS ITS OWN AP. AT ZERO THE UNIT RESTS UNTIL TOMORROW.");
    }

    private static string AptitudeName(ZoneAptitude aptitude)
    {
        switch (aptitude)
        {
            case ZoneAptitude.Normal: return "NORMAL CAVES";
            case ZoneAptitude.Hard: return "HARD CAVES";
            default: return "EASY CAVES";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanBeSelected) return;

        // Selecting a unit is the start of every action in the run.
        UiClickSound.Play(UiClickKind.Select);
        Clicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected == selected) return;

        IsSelected = selected;
        ApplySelectionScale();
        ApplyFrame();
        // The pending pips belong to the paying card only, so repaint at once.
        RefreshApPips();
    }

    public void SetSelectable(bool value)
    {
        if (selectable == value) return;

        selectable = value;
        ApplyFrame();
    }

    /// <summary>
    /// Marks this card as a legal target for the action being aimed (a heal
    /// patient). Eligible cards get a pulsing mint frame and full opacity -
    /// without it, a Hurt card would sit at the normal "not idle" dimming and
    /// read as unavailable, which is the opposite of what the player needs.
    /// </summary>
    public void SetTargetable(bool value)
    {
        if (targetable == value) return;

        targetable = value;
        if (group != null)
        {
            float alpha = value ? 1f : AlphaForState();
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }
        ApplyFrame();
    }

    /// <summary>Used to freeze selection (e.g. while a trip is travelling) without changing availability.</summary>
    public void SetSelectionEnabled(bool value)
    {
        if (selectionEnabled == value) return;

        selectionEnabled = value;
        if (!value) targetable = false;
        if (group != null && !value) group.alpha = AlphaForState();
        ApplyFrame();
    }

    public void Refresh()
    {
        if (unit == null) return;

        RefreshName();
        RefreshApPips();
        RefreshStateBadge();
        RefreshAptitude();
        RefreshBackground();

        if (group != null)
        {
            float alpha = targetable ? 1f : AlphaForState();
            if (!Mathf.Approximately(group.alpha, alpha)) group.alpha = alpha;
        }

        // Selectability is owned by whoever drives the panel (dispatch vs skill
        // mode pick different rules), so it is never recomputed here.
        ApplyFrame();
    }

    // ------------------------------------------------------------ content

    private void RefreshName()
    {
        if (nameText == null) return;

        // Uppercase keeps the card inside the pixel font's glyph coverage, and
        // the lit lamps carry hunger / out-today so the name line stays short.
        string text = unit.unitName != null ? unit.unitName.ToUpperInvariant() : "";
        if (nameText.text != text) nameText.text = text;
    }

    /// <summary>
    /// Top-left slot: the sword is the dispatch ticket. It stays visible but
    /// dims once spent, so "I cannot go again today" reads at a glance; units
    /// that cannot act at all show their state letter instead.
    /// </summary>
    private void RefreshStateBadge()
    {
        if (stateIcon == null) return;

        bool idle = unit.state == UnitState.Idle;
        Sprite sprite = idle ? ReadySprite : null;

        stateIcon.sprite = sprite;
        stateIcon.gameObject.SetActive(sprite != null);
        stateIcon.color = unit.HasSword ? Color.white : SpentSwordColor;

        if (statusText != null)
        {
            bool showLetter = !idle;
            statusText.gameObject.SetActive(showLetter);
            if (showLetter)
            {
                statusText.text = StateInitial(unit.state);
                statusText.color = StateColor(unit.state);
            }
        }
    }

    /// <summary>Art the HUD uses for "this unit holds a sword", shared with the help page.</summary>
    public Sprite ReadySprite => swordSprite != null ? swordSprite : PlaceholderIcons.Sword;

    /// <summary>Lamp art, so the help page can explain each lamp with the very same picture.</summary>
    public Sprite HurtLampSprite => hurtIcon != null ? hurtIcon : PlaceholderIcons.Plus;
    public Sprite HungryLampSprite => hungryIcon != null ? hungryIcon : PlaceholderIcons.Bowl;
    public Sprite GoodTraitLampSprite => positiveTraitIcon != null ? positiveTraitIcon : PlaceholderIcons.ArrowUp;
    public Sprite BadTraitLampSprite => negativeTraitIcon != null ? negativeTraitIcon : PlaceholderIcons.ArrowDown;
    public Sprite BlessedLampSprite => blessedIcon != null ? blessedIcon : PlaceholderIcons.Star;
    public Sprite PotionLampSprite => buffPotionIcon != null ? buffPotionIcon : PlaceholderIcons.Flask;

    /// <summary>One action point, drawn with the same pip the card uses.</summary>
    public Sprite ApPipSprite => apPipAvailable;

    /// <summary>Main aptitude as a lettered badge, talent as an icon right under it.</summary>
    private void RefreshAptitude()
    {
        if (aptitudeBadge != null) aptitudeBadge.color = AptitudeColor(unit.primaryAptitude);
        if (aptitudeLetter != null) aptitudeLetter.text = AptitudeLetter(unit.primaryAptitude);

        if (talentIcon != null)
        {
            talentIcon.sprite = TalentSprite(unit.talent);
            talentIcon.gameObject.SetActive(talentIcon.sprite != null);
        }
    }

    /// <summary>
    /// The card body tint follows the unit's condition: hurt paints red, hungry
    /// paints orange, and a healed-but-still-hungry unit falls back to orange.
    /// </summary>
    private void RefreshBackground()
    {
        if (background == null) return;

        Color color = BackgroundFor(unit);
        if (background.color != color) background.color = color;
    }

    private Color BackgroundFor(UnitCharacter who)
    {
        if (who.state == UnitState.Hurt) return hurtBackground;
        if (who.starvationStacks > 0) return hungryBackground;
        return cardBackground;
    }

    private static string StateInitial(UnitState state)
    {
        switch (state)
        {
            case UnitState.Idle: return "1";
            case UnitState.Resting: return "R";
            case UnitState.Hurt: return "H";
            case UnitState.Away: return "A";
            case UnitState.Death: return "D";
            default: return "";
        }
    }

    /// <summary>Long name for a state, used by the tooltip fallback.</summary>
    private static string StateName(UnitState state)
    {
        switch (state)
        {
            case UnitState.Resting: return "OUT OF ACTION POINTS UNTIL THE NEXT DAY.";
            case UnitState.Hurt: return "INJURED. HEAL IT BEFORE IT CAN ACT.";
            case UnitState.Away: return "OUT ON A TRIP.";
            case UnitState.Death: return "DEAD FOR THE REST OF THE RUN.";
            default: return "READY WHEN IT HOLDS A SWORD.";
        }
    }

    private static Color StateColor(UnitState state)
    {
        switch (state)
        {
            case UnitState.Hurt: return new Color(1f, 0.5f, 0.5f);
            case UnitState.Death: return new Color(0.8f, 0.8f, 0.8f);
            case UnitState.Away: return new Color(0.7f, 0.8f, 1f);
            default: return new Color(0.85f, 0.85f, 0.9f);
        }
    }

    private static string AptitudeLetter(ZoneAptitude aptitude)
    {
        switch (aptitude)
        {
            case ZoneAptitude.Easy: return "E";
            case ZoneAptitude.Normal: return "N";
            case ZoneAptitude.Hard: return "H";
            default: return "?";
        }
    }

    private static Color AptitudeColor(ZoneAptitude aptitude)
    {
        switch (aptitude)
        {
            case ZoneAptitude.Easy: return EasyColor;
            case ZoneAptitude.Normal: return NormalColor;
            case ZoneAptitude.Hard: return HardColor;
            default: return Color.white;
        }
    }

    private Sprite TalentSprite(UnitTalent talent)
    {
        switch (talent)
        {
            case UnitTalent.Heal: return blessedIcon != null ? blessedIcon : PlaceholderIcons.Star;
            case UnitTalent.Crafting: return buffPotionIcon != null ? buffPotionIcon : PlaceholderIcons.Flask;
            case UnitTalent.Cooking: return hungryIcon != null ? hungryIcon : PlaceholderIcons.Bowl;
            case UnitTalent.Exploring: return positiveTraitIcon != null ? positiveTraitIcon : PlaceholderIcons.ArrowUp;
            default: return null;
        }
    }

    // --------------------------------------------------------- the lamps

    /// <summary>
    /// Buff/debuff row, dashboard-warning style: every lamp is always drawn,
    /// grey until the unit actually has that condition.
    /// </summary>
    private void RefreshLamps()
    {
        if (unit == null || lampIcons.Count == 0) return;

        bool hasGoodTrait = false;
        bool hasBadTrait = false;
        foreach (var trait in unit.traits)
        {
            if (trait.polarity == TraitPolarity.Positive) hasGoodTrait = true;
            if (trait.polarity == TraitPolarity.Negative) hasBadTrait = true;
        }

        int potionStacks = RunResources.Instance != null ? RunResources.Instance.PartySuccessBonus : 0;

        SetLamp(0, unit.state == UnitState.Hurt, hurtColor);
        SetLamp(1, unit.starvationStacks > 0, hungryColor);
        SetLamp(2, hasGoodTrait, goodColor);
        SetLamp(3, hasBadTrait, hurtColor);
        SetLamp(4, unit.blessed, goodColor);
        SetLamp(5, potionStacks > 0, buffPotionColor);
    }

    private void SetLamp(int index, bool lit, Color litColor)
    {
        if (index >= lampIcons.Count) return;
        lampIcons[index].color = lit ? litColor : lampOffColor;
    }

    // ------------------------------------------------------- card chrome

    /// <summary>
    /// Builds the card body in code. Everything the player sees - background,
    /// frame, portrait, badges, text, lamps - lives under a "Lift" rect, so
    /// selecting the card slides the whole card up as one piece. The prefab's
    /// root image is kept only as an invisible click surface.
    /// </summary>
    private void BuildCardChrome()
    {
        if (chromeBuilt) return;
        chromeBuilt = true;

        var root = (RectTransform)transform;
        root.sizeDelta = cardSize;

        Sprite portraitSprite = iconImage != null ? iconImage.sprite : null;

        // Root image stays clickable but paints nothing: the visible background
        // is a child of Lift so it moves with the card.
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = false;
            iconImage.color = new Color(0f, 0f, 0f, 0f);
            iconImage.raycastTarget = true;
        }

        lift = NewRect("Lift", root);
        Stretch(lift);

        background = CreateImage("Background", lift);
        background.raycastTarget = false;
        background.color = cardBackground;
        Stretch((RectTransform)background.transform);

        BuildPortrait(portraitSprite);
        BuildTopBadges();
        BuildDivider();
        BuildBottomBand();
        BuildFrame();
    }

    private void BuildPortrait(Sprite portraitSprite)
    {
        if (portraitSprite == null) return;

        Image portrait = CreateImage("Portrait", lift);
        portrait.sprite = portraitSprite;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.color = Color.white;

        float top = padding + 30f + 4f;
        float height = Mathf.Max(40f, BandTop - 6f - top);

        var rect = (RectTransform)portrait.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-padding * 2f, height);
    }

    /// <summary>Distance from the top of the card to the name/lamp band.</summary>
    private float BandTop => cardSize.y - padding - BandHeight;

    private const float BandHeight = 96f;

    private void BuildTopBadges()
    {
        const float badgeSize = 30f;

        // Top-left: state (sword when ready).
        stateIcon = CreateImage("StateIcon", lift);
        stateIcon.sprite = PlaceholderIcons.Sword;
        stateIcon.preserveAspect = true;
        stateIcon.raycastTarget = false;
        var stateRect = (RectTransform)stateIcon.transform;
        stateRect.anchorMin = stateRect.anchorMax = new Vector2(0f, 1f);
        stateRect.pivot = new Vector2(0f, 1f);
        stateRect.sizeDelta = new Vector2(badgeSize, badgeSize);
        stateRect.anchoredPosition = new Vector2(padding, -padding);

        // The prefab's statusText doubles as the state letter for busy units.
        if (statusText != null)
        {
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 12f;
            statusText.fontSizeMax = 24f;
            statusText.raycastTarget = false;

            var textRect = (RectTransform)statusText.transform;
            textRect.SetParent(lift, false);
            textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.sizeDelta = new Vector2(badgeSize, badgeSize);
            textRect.anchoredPosition = new Vector2(padding, -padding);
        }

        HudTooltipTrigger.Attach(stateIcon, StateTooltip);
        if (statusText != null) HudTooltipTrigger.Attach(statusText, StateTooltip);

        // Top-right: main aptitude letter.
        aptitudeBadge = CreateImage("AptitudeBadge", lift);
        aptitudeBadge.sprite = PlaceholderIcons.RoundedSquare;
        aptitudeBadge.type = Image.Type.Sliced;
        aptitudeBadge.raycastTarget = false;
        var badgeRect = (RectTransform)aptitudeBadge.transform;
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = new Vector2(badgeSize, badgeSize);
        badgeRect.anchoredPosition = new Vector2(-padding, -padding);

        aptitudeLetter = NewText("AptitudeLetter", badgeRect, 20f);
        aptitudeLetter.fontStyle = FontStyles.Bold;
        aptitudeLetter.color = new Color(0.06f, 0.07f, 0.09f);

        HudTooltipTrigger.Attach(aptitudeBadge, AptitudeTooltip);
        HudTooltipTrigger.Attach(aptitudeLetter, AptitudeTooltip);

        // Talent icon sits right under the aptitude badge.
        talentIcon = CreateImage("TalentIcon", lift);
        talentIcon.preserveAspect = true;
        talentIcon.raycastTarget = false;
        var talentRect = (RectTransform)talentIcon.transform;
        talentRect.anchorMin = talentRect.anchorMax = new Vector2(1f, 1f);
        talentRect.pivot = new Vector2(1f, 1f);
        talentRect.sizeDelta = new Vector2(26f, 26f);
        talentRect.anchoredPosition = new Vector2(-padding - 2f, -(padding + badgeSize + 4f));

        HudTooltipTrigger.Attach(talentIcon, TalentTooltip);
    }

    private void BuildDivider()
    {
        float dividerY = BandTop - 4f;

        var divider = CreateImage("Divider", lift);
        divider.color = new Color(1f, 1f, 1f, 0.14f);
        divider.raycastTarget = false;

        var rect = (RectTransform)divider.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -dividerY);
        rect.sizeDelta = new Vector2(-padding * 2f, 2f);
    }

    private void BuildBottomBand()
    {
        float bandTop = BandTop;
        float leftWidth = cardSize.x * 0.56f;

        // Vertical split between the name/AP column and the lamp column.
        var split = CreateImage("Split", lift);
        split.color = new Color(1f, 1f, 1f, 0.10f);
        split.raycastTarget = false;
        var splitRect = (RectTransform)split.transform;
        splitRect.anchorMin = new Vector2(0f, 0f);
        splitRect.anchorMax = new Vector2(0f, 1f);
        splitRect.pivot = new Vector2(0f, 1f);
        splitRect.anchoredPosition = new Vector2(padding + leftWidth, -bandTop);
        splitRect.sizeDelta = new Vector2(2f, -bandTop - padding);

        // Name (top-left of the band).
        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 11f;
            nameText.fontSizeMax = 22f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            var rect = (RectTransform)nameText.transform;
            rect.SetParent(lift, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(leftWidth, 28f);
            rect.anchoredPosition = new Vector2(padding, -bandTop);
        }

        BuildApPips(bandTop + 30f, leftWidth);
        BuildLamps(bandTop, cardSize.x - leftWidth - padding * 2f);
    }

    private void BuildLamps(float bandTop, float columnWidth)
    {
        const int columns = 3;
        const float lampSize = 24f;
        const float lampGap = 6f;

        var row = NewRect("Lamps", lift);
        row.anchorMin = new Vector2(1f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(1f, 1f);
        row.sizeDelta = new Vector2(columnWidth, 0f);
        row.anchoredPosition = new Vector2(-padding, -bandTop);

        Sprite[] sprites =
        {
            hurtIcon != null ? hurtIcon : PlaceholderIcons.Plus,
            hungryIcon != null ? hungryIcon : PlaceholderIcons.Bowl,
            positiveTraitIcon != null ? positiveTraitIcon : PlaceholderIcons.ArrowUp,
            negativeTraitIcon != null ? negativeTraitIcon : PlaceholderIcons.ArrowDown,
            blessedIcon != null ? blessedIcon : PlaceholderIcons.Star,
            buffPotionIcon != null ? buffPotionIcon : PlaceholderIcons.Flask
        };

        for (int i = 0; i < sprites.Length; i++)
        {
            Image icon = CreateImage("Lamp" + i, row);
            icon.sprite = sprites[i];
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = lampOffColor;

            var rect = (RectTransform)icon.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(lampSize, lampSize);

            int col = i % columns;
            int line = i / columns;
            rect.anchoredPosition = new Vector2(col * (lampSize + lampGap), -line * (lampSize + lampGap));

            lampIcons.Add(icon);

            int lampIndex = i;
            HudTooltipTrigger.Attach(icon, () => LampTooltip(lampIndex));
        }
    }

    /// <summary>
    /// Row that holds the AP pips. Nothing is built when the three sprites are
    /// missing, so a card prefab without them keeps the old "AP x/y" text.
    /// </summary>
    private void BuildApPips(float topOffset, float columnWidth)
    {
        if (apPipAvailable == null || apPipPending == null || apPipSpent == null) return;

        var go = new GameObject("ApPips", typeof(RectTransform));
        apPipRow = (RectTransform)go.transform;
        apPipRow.SetParent(lift, false);
        apPipRow.anchorMin = new Vector2(0f, 1f);
        apPipRow.anchorMax = new Vector2(0f, 1f);
        apPipRow.pivot = new Vector2(0f, 1f);
        apPipRow.anchoredPosition = new Vector2(padding, -topOffset);
        apPipRow.sizeDelta = new Vector2(columnWidth, apPipRowHeight);
    }

    /// <summary>
    /// One pip per action point, painted from the unit's live AP. The pending
    /// cost only shows on the card that pays it.
    /// </summary>
    private void RefreshApPips()
    {
        RefreshLamps();

        if (apPipRow == null || unit == null) return;

        int total = Mathf.Max(0, unit.maxActionPoints);
        int left = Mathf.Clamp(unit.currentActionPoints, 0, total);
        int pending = ApCostPreview;
        int spending = pending > 0 ? Mathf.Min(unit.GetActualApCost(pending), left) : 0;

        while (apPips.Count < total)
        {
            var go = new GameObject("ApPip" + apPips.Count, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(apPipRow, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            apPips.Add(image);

            int pipIndex = apPips.Count - 1;
            HudTooltipTrigger.Attach(image, () => ApPipTooltip(pipIndex));
        }

        for (int i = 0; i < apPips.Count; i++)
        {
            bool visible = i < total;
            var pip = apPips[i];
            if (pip.gameObject.activeSelf != visible) pip.gameObject.SetActive(visible);
            if (!visible) continue;

            var rect = (RectTransform)pip.transform;
            rect.sizeDelta = new Vector2(apPipWidth, apPipHeight);
            rect.anchoredPosition = new Vector2(i * (apPipWidth + apPipSpacing), 0f);

            // AP drains from the left, so the pips just before the boundary are
            // the ones the pending action will eat.
            Sprite sprite = i >= left ? apPipSpent
                : i >= left - spending ? apPipPending
                : apPipAvailable;
            if (pip.sprite != sprite) pip.sprite = sprite;
        }
    }

    private void BuildFrame()
    {
        // Frame strips ride along with Lift, so the outline slides with the card.
        for (int i = 0; i < 4; i++)
        {
            Image strip = CreateImage("Frame" + i, lift);
            strip.raycastTarget = false;
            frame.Add(strip);
        }
        ConfigureFrameRects();
    }

    // ------------------------------------------------------------ helpers

    private Image CreateImage(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        return image;
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TMP_Text NewText(string objectName, Transform parent, float fontSize)
    {
        var rect = NewRect(objectName, parent);
        Stretch(rect);
        // Built inactive so TMP's Awake sees the font already applied instead of
        // logging "no Font Asset assigned" on the first frame.
        GameObject go = rect.gameObject;
        go.SetActive(false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        TmpFont.Apply(text);
        go.SetActive(true);
        return text;
    }

    private void ConfigureFrameRects()
    {
        var top = (RectTransform)frame[0].transform;
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = Vector2.zero;

        var bottom = (RectTransform)frame[1].transform;
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = Vector2.zero;

        var left = (RectTransform)frame[2].transform;
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.anchoredPosition = Vector2.zero;

        var right = (RectTransform)frame[3].transform;
        right.anchorMin = new Vector2(1f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.pivot = new Vector2(1f, 0.5f);
        right.anchoredPosition = Vector2.zero;
    }

    private void ApplySelectionScale()
    {
        transform.localScale = Vector3.one * (IsSelected ? selectedScale : 1f);
    }

    private void ApplyFrame()
    {
        if (!chromeBuilt) return;

        float thickness = IsSelected || targetable ? selectedFrameThickness : frameThickness;
        if (targetable) thickness = targetableFrameThickness;

        if (!Mathf.Approximately(appliedThickness, thickness))
        {
            appliedThickness = thickness;
            ((RectTransform)frame[0].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[1].transform).sizeDelta = new Vector2(0f, thickness);
            ((RectTransform)frame[2].transform).sizeDelta = new Vector2(thickness, 0f);
            ((RectTransform)frame[3].transform).sizeDelta = new Vector2(thickness, 0f);
        }

        // The pulse in Update() owns the colour while targetable.
        if (targetable) return;

        // The outline reads as "which dungeon tier is this unit good at".
        Color color = unit != null ? AptitudeColor(unit.primaryAptitude) : accent;
        if (!CanBeSelected) color.a = unavailableFrameAlpha;
        else color.a = IsSelected ? 1f : unselectedFrameAlpha;

        for (int i = 0; i < frame.Count; i++) frame[i].color = color;
    }

    /// <summary>Card opacity from the unit's state alone; the targetable flag overrides it.</summary>
    private float AlphaForState()
    {
        if (unit == null) return 1f;
        if (unit.state == UnitState.Death) return 0.25f;
        if (!selectable) return 0.5f;
        if (unit.state != UnitState.Idle) return 0.55f;
        return 1f;
    }
}