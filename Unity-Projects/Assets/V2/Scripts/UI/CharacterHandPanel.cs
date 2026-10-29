using System;
using UnityEngine;

/// <summary>
/// Character hand along the bottom centre - one card per unit, laid out like a
/// hand of cards. Doubles as the selection surface for both the dispatch flow
/// and the skill flow.
///
/// The mode decides how many cards may be held at once and, for Heal, narrows
/// the hand down to the wounded:
///   Dispatch   -> up to 2 cards, any unit that can still act
///   Skill      -> 1 card, the character doing the base work
///   HealTarget -> 1 card, and only units in <see cref="UnitState.Hurt"/>
///
/// The panel itself holds no selection state - UnitRosterView owns that so a
/// single component decides what is selected at any moment.
/// </summary>
public class CharacterHandPanel : MonoBehaviour
{
    public enum SelectionMode
    {
        Dispatch,
        Skill,
        HealTarget
    }

    [Header("Refs")]
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private RectTransform cardsContainer;

    [Header("Card Layout")]
    [SerializeField] private float cardSpacing = 12f;
    [SerializeField] private float cardWidth = 190f;
    [SerializeField] private float cardHeight = 280f;

    [Header("Selection")]
    [SerializeField] private int maxDispatchSelection = 2;
    [SerializeField] private int maxSkillSelection = 1;

    public event Action<SelectionMode> OnModeChanged;

    public SelectionMode Mode { get; private set; } = SelectionMode.Dispatch;
    public RectTransform CardsContainer => cardsContainer != null ? cardsContainer : (RectTransform)transform;
    public float CardSpacing => cardSpacing;
    public Vector2 CardSize => new Vector2(cardWidth, cardHeight);
    public int MaxSelection =>
        Mode == SelectionMode.Dispatch ? maxDispatchSelection : maxSkillSelection;

    /// <summary>Whether this unit may be picked in the current mode.</summary>
    public bool CanSelect(UnitCharacter unit)
    {
        if (unit == null || !unit.IsAlive) return false;

        // The card answers "who am I acting with" - the action button decides
        // whether that action is legal. Gating dispatch cards on
        // CanBeSentGathering would make the skill flow unreachable for a unit
        // that already gathered today, so both flows share one rule.
        if (Mode == SelectionMode.HealTarget) return unit.state == UnitState.Hurt;

        return unit.CanBeDispatched;
    }

    public void SetMode(SelectionMode mode)
    {
        if (Mode == mode) return;

        Mode = mode;
        OnModeChanged?.Invoke(mode);
    }

    private void Awake()
    {
        if (handRoot == null) handRoot = (RectTransform)transform;
        if (cardsContainer == null) cardsContainer = handRoot;
    }
}
