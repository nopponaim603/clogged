using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Recruit event triggered by a Recruit-type Ship node. Offers a couple of
/// crew candidates with a relic cost each — same "preview data first,
/// instantiate only if actually hired" pattern as CrewIntakeUI. The player
/// can recruit as many as they can afford (0, 1, or both), then presses
/// Continue to move on.
/// </summary>
public class RecruitEventUI : MonoBehaviour
{
    [Header("References")]
    public CrewManager CrewManager;
    public ResourceManager ResourceManager;
    public GameObject RecruitPanel;
    public Button ContinueButton;

    [Header("Card Slots (one per offered candidate)")]
    public RecruitCardSlot[] CardSlots = new RecruitCardSlot[2];

    [Header("Offer Settings")]
    public int OfferCount = 2;
    public int MinHireCost = 15;
    public int MaxHireCost = 35;
    [Tooltip("Currency spent to recruit — must match a key in ResourceManager.Resources.")]
    public string CurrencyType = "relic";

    [Header("Crew Appearance Pool")]
    [Tooltip("Same pool used by CrewIntakeUI — pass the same list here so recruits look consistent with starting crew.")]
    public List<GameObject> CrewPrefabPool = new List<GameObject>();

    private readonly List<CrewData> _offeredData = new List<CrewData>();
    private Action _onComplete;

    private void Awake()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(OnContinueClicked);

        if (RecruitPanel != null)
            RecruitPanel.SetActive(false);
    }

    public void ShowRecruit(Action onComplete)
    {
        _onComplete = onComplete;
        GenerateOffer();

        if (RecruitPanel != null)
            RecruitPanel.SetActive(true);
    }

    private void GenerateOffer()
    {
        _offeredData.Clear();

        int count = Mathf.Min(OfferCount, CardSlots.Length);
        var pickPool = new List<GameObject>(CrewPrefabPool);

        for (int i = 0; i < count; i++)
        {
            GameObject appearance = null;

            if (pickPool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pickPool.Count);
                appearance = pickPool[index];
                pickPool.RemoveAt(index);
            }

            int hireCost = UnityEngine.Random.Range(MinHireCost, MaxHireCost + 1);
            CrewData data = CrewManager.GenerateRandomCrewData(appearance, hireCost);

            _offeredData.Add(data);
            PopulateCard(CardSlots[i], data);
        }

        for (int i = count; i < CardSlots.Length; i++)
        {
            if (CardSlots[i] != null && CardSlots[i].RecruitButton != null)
                CardSlots[i].RecruitButton.gameObject.SetActive(false);
        }
    }

    private void PopulateCard(RecruitCardSlot slot, CrewData data)
    {
        if (slot == null) return;

        if (slot.RecruitButton != null)
            slot.RecruitButton.gameObject.SetActive(true);

        if (slot.NameText != null) slot.NameText.text = data.Name;
        if (slot.StatsText != null)
            slot.StatsText.text = $"HP {Mathf.RoundToInt(data.Hp)} | Spd {data.Speed:0.0} | Gather {data.GatheringProficiency:0.0}";
        if (slot.CostText != null) slot.CostText.text = $"{data.HireCost} {CurrencyType}";

        if (slot.RecruitButton != null)
        {
            slot.RecruitButton.interactable = true;
            slot.RecruitButton.onClick.RemoveAllListeners();
            slot.RecruitButton.onClick.AddListener(() => OnRecruitClicked(slot, data));
        }
    }

    private void OnRecruitClicked(RecruitCardSlot slot, CrewData data)
    {
        if (!ResourceManager.SpendResource(CurrencyType, data.HireCost))
        {
            if (slot.StatsText != null)
                slot.StatsText.text = $"Not enough {CurrencyType}!";
            return;
        }

        Crew crew = CrewManager.InstantiateFromData(data);
        CrewManager.HireCrewFree(crew); // cost was already paid manually above

        _offeredData.Remove(data);

        // Mark this card as recruited — can't be picked again.
        if (slot.RecruitButton != null)
        {
            slot.RecruitButton.interactable = false;
        }
        if (slot.CostText != null)
        {
            slot.CostText.text = "Recruited!";
        }
    }

    private void OnContinueClicked()
    {
        _offeredData.Clear();

        if (RecruitPanel != null)
            RecruitPanel.SetActive(false);

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }
}

[Serializable]
public class RecruitCardSlot
{
    public Button RecruitButton;
    public TMP_Text NameText;
    public TMP_Text StatsText;
    public TMP_Text CostText;
}
