using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// v1 Ship Phase screen. Shows 2-3 node cards (Normal / Bonus / Recruit),
/// the player picks ONE, and it resolves immediately:
/// - Normal: straight through.
/// - Bonus: sets GameManager's resource spawn bias for today, then through.
/// - Recruit: opens RecruitEventUI (paid crew hiring) before continuing.
///
/// One card pick = one block (this does not implement a deeper multi-node
/// path — see chat notes if that gets built later).
/// </summary>
public class ShipNavigatorUI : MonoBehaviour
{
    [Header("References")]
    public GameObject NavigatorPanel;
    public RecruitEventUI RecruitEventUI;

    [Header("Card Slots (one per offered node)")]
    public ShipNodeCardSlot[] CardSlots = new ShipNodeCardSlot[3];

    [Header("Offer Settings")]
    public int OfferCount = 3;
    [Tooltip("Weights don't need to add up to 1 — they're normalized automatically.")]
    public float NormalWeight = 0.5f;
    public float BonusWeight = 0.25f;
    public float RecruitWeight = 0.25f;

    [Header("Bonus Node Settings")]
    public string[] BonusResourceTypes = { "wood", "food", "relic" };

    private Action _onComplete;

    private void Awake()
    {
        if (NavigatorPanel != null)
            NavigatorPanel.SetActive(false);
    }

    /// <summary>
    /// Rolls a fresh set of node cards and shows the panel. onComplete is
    /// invoked once the player's pick has fully resolved (immediately for
    /// Normal/Bonus, or after the Recruit event's own Continue for Recruit).
    /// </summary>
    public void ShowNavigator(Action onComplete)
    {
        _onComplete = onComplete;
        GenerateOffer();

        if (NavigatorPanel != null)
            NavigatorPanel.SetActive(true);
    }

    private void GenerateOffer()
    {
        int count = Mathf.Min(OfferCount, CardSlots.Length);

        // Picks a distinct node type per slot — no two cards on screen at
        // once will share a Type (e.g. two Bonus cards can't both appear).
        List<ShipNodeType> types = PickDistinctTypes(count);

        for (int i = 0; i < types.Count; i++)
        {
            ShipNodeOption option = CreateOption(types[i]);
            PopulateCard(CardSlots[i], option);
        }

        for (int i = types.Count; i < CardSlots.Length; i++)
        {
            if (CardSlots[i] != null && CardSlots[i].SelectButton != null)
                CardSlots[i].SelectButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Weighted sample WITHOUT replacement over the three node types, so the
    /// offered cards never repeat a type. Weights still bias which types are
    /// more likely to show up when count is less than the full type set
    /// (e.g. with 2 slots, Recruit is less likely to appear than Normal).
    /// If count >= the number of distinct types, all types are returned
    /// (shuffled by weight) and no further duplicates are possible anyway.
    /// </summary>
    private List<ShipNodeType> PickDistinctTypes(int count)
    {
        var remaining = new List<(ShipNodeType type, float weight)>
        {
            (ShipNodeType.Normal, NormalWeight),
            (ShipNodeType.Bonus, BonusWeight),
            (ShipNodeType.Recruit, RecruitWeight)
        };

        var result = new List<ShipNodeType>();
        int pickCount = Mathf.Min(count, remaining.Count);

        for (int i = 0; i < pickCount; i++)
        {
            float total = 0f;
            foreach (var entry in remaining) total += entry.weight;

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            int chosenIndex = remaining.Count - 1; // fallback: last one

            for (int j = 0; j < remaining.Count; j++)
            {
                cumulative += remaining[j].weight;
                if (roll < cumulative)
                {
                    chosenIndex = j;
                    break;
                }
            }

            result.Add(remaining[chosenIndex].type);
            remaining.RemoveAt(chosenIndex);
        }

        return result;
    }

    private ShipNodeOption CreateOption(ShipNodeType type)
    {
        switch (type)
        {
            case ShipNodeType.Normal:
                return new ShipNodeOption
                {
                    Type = ShipNodeType.Normal,
                    Title = "Quiet Path",
                    Description = "Nothing unusual. Straight to work."
                };

            case ShipNodeType.Bonus:
                string resourceType = BonusResourceTypes[UnityEngine.Random.Range(0, BonusResourceTypes.Length)];
                return new ShipNodeOption
                {
                    Type = ShipNodeType.Bonus,
                    Title = "Rich Grounds",
                    Description = $"Today's map will favor {resourceType}.",
                    BonusResourceType = resourceType
                };

            case ShipNodeType.Recruit:
                return new ShipNodeOption
                {
                    Type = ShipNodeType.Recruit,
                    Title = "Wandering Stranger",
                    Description = "Someone's looking for work — for the right price."
                };

            default:
                Debug.LogWarning($"ShipNavigatorUI: Unhandled ShipNodeType {type}, defaulting to Normal.");
                return new ShipNodeOption
                {
                    Type = ShipNodeType.Normal,
                    Title = "Quiet Path",
                    Description = "Nothing unusual. Straight to work."
                };
        }
    }

    private void PopulateCard(ShipNodeCardSlot slot, ShipNodeOption option)
    {
        if (slot == null) return;

        if (slot.SelectButton != null)
            slot.SelectButton.gameObject.SetActive(true);

        if (slot.TitleText != null) slot.TitleText.text = option.Title;
        if (slot.DescriptionText != null) slot.DescriptionText.text = option.Description;
        if (slot.TypeText != null) slot.TypeText.text = option.Type.ToString();

        if (slot.SelectButton != null)
        {
            slot.SelectButton.onClick.RemoveAllListeners();
            slot.SelectButton.onClick.AddListener(() => OnCardPicked(option));
        }
    }

    private void OnCardPicked(ShipNodeOption option)
    {
        if (NavigatorPanel != null)
            NavigatorPanel.SetActive(false);

        switch (option.Type)
        {
            case ShipNodeType.Normal:
                CompleteAndContinue();
                break;

            case ShipNodeType.Bonus:
                GameManager.Instance.SetNextDayResourceBias(option.BonusResourceType);
                CompleteAndContinue();
                break;

            case ShipNodeType.Recruit:
                if (RecruitEventUI != null)
                {
                    RecruitEventUI.ShowRecruit(CompleteAndContinue);
                }
                else
                {
                    Debug.LogWarning("ShipNavigatorUI: Recruit node picked but no RecruitEventUI assigned.");
                    CompleteAndContinue();
                }
                break;
        }
    }

    private void CompleteAndContinue()
    {
        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }
}

/// <summary>
/// One navigator card's fields. Assign in the Inspector.
/// </summary>
[Serializable]
public class ShipNodeCardSlot
{
    public Button SelectButton;
    public TMP_Text TitleText;
    public TMP_Text DescriptionText;
    public TMP_Text TypeText;
}