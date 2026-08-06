using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time, start-of-run screen: previews a set number of crew (default 3)
/// as plain CrewData — no GameObjects are created yet. The player clicks
/// cards to select/deselect, then Confirm turns ONLY the selected data into
/// real Crew GameObjects (via CrewManager.InstantiateFromData) and hires
/// them. Unpicked offers never touch the scene at all. GameManager calls
/// ShowIntake() the first time the ShipDay scene loads with no crew hired
/// yet, and the day only starts once this completes.
/// </summary>
public class CrewIntakeUI : MonoBehaviour
{
    [Header("References")]
    public CrewManager CrewManager;
    public GameObject IntakePanel;
    public Button ConfirmButton;

    [Header("Card Slots (one per offered crew)")]
    public CrewCardSlot[] CardSlots = new CrewCardSlot[3];

    [Header("Intake Settings")]
    public int OfferCount = 3;

    [Header("Crew Appearance Pool")]
    [Tooltip("Pool of visual variants (sprite/look only — any stats set on the prefab are ignored and re-randomized). Picks distinct appearances per offer where possible. Falls back to a plain random crew if the pool is empty or runs out.")]
    public List<GameObject> CrewPrefabPool = new List<GameObject>();

    private readonly List<CrewData> _offeredData = new List<CrewData>();
    private readonly List<CrewData> _selectedData = new List<CrewData>();

    private Action _onComplete;

    private void Awake()
    {
        if (ConfirmButton != null)
            ConfirmButton.onClick.AddListener(OnConfirmClicked);

        if (ConfirmButton != null)
            ConfirmButton.interactable = false;

        if (IntakePanel != null)
            IntakePanel.SetActive(false);
    }

    /// <summary>
    /// Rolls a fresh set of crew previews, shows the panel, and invokes
    /// onComplete once the player confirms.
    /// </summary>
    public void ShowIntake(Action onComplete)
    {
        _onComplete = onComplete;

        _selectedData.Clear();

        if (ConfirmButton != null)
            ConfirmButton.interactable = false;

        GenerateOffer();

        if (IntakePanel != null)
            IntakePanel.SetActive(true);
    }

    /// <summary>
    /// Builds preview data only — no GameObjects, no scene changes.
    /// </summary>
    private void GenerateOffer()
    {
        _offeredData.Clear();

        int count = Mathf.Min(OfferCount, CardSlots.Length);

        // Work off a copy so each offer can pick distinct appearances without
        // permanently draining the configured pool.
        var pickPool = new List<GameObject>(CrewPrefabPool);

        for (int i = 0; i < count; i++)
        {
            GameObject appearance = null;

            if (pickPool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pickPool.Count);
                appearance = pickPool[index];
                pickPool.RemoveAt(index); // no duplicate look within a single offer
            }

            CrewData data = CrewManager.GenerateRandomCrewData(appearance, 0);

            _offeredData.Add(data);
            PopulateCard(CardSlots[i], data);
        }

        // Hide unused slots if fewer than CardSlots.Length were offered.
        for (int i = count; i < CardSlots.Length; i++)
        {
            if (CardSlots[i] != null && CardSlots[i].SelectButton != null)
            {
                CardSlots[i].SelectButton.gameObject.SetActive(false);
            }
        }
    }

    private void PopulateCard(CrewCardSlot slot, CrewData data)
    {
        if (slot == null) return;

        if (slot.SelectButton != null)
            slot.SelectButton.gameObject.SetActive(true);

        if (slot.NameText != null) slot.NameText.text = data.Name;
        if (slot.HpText != null) slot.HpText.text = "HP: " + Mathf.RoundToInt(data.Hp);
        if (slot.SpeedText != null) slot.SpeedText.text = "Speed: " + data.Speed.ToString("0.0");
        if (slot.GatheringText != null) slot.GatheringText.text = "Gathering: " + data.GatheringProficiency.ToString("0.0");
        if (slot.SearchingText != null) slot.SearchingText.text = "Searching: " + data.SearchingProficiency.ToString("0.0");
        if (slot.HuntingText != null) slot.HuntingText.text = "Hunting: " + data.HuntingProficiency.ToString("0.0");
        if (slot.PerksText != null) slot.PerksText.text = "Perks: " + string.Join(", ", data.Perks);

        if (slot.CardImage != null)
            slot.CardImage.color = Color.white;

        if (slot.SelectButton != null)
        {
            slot.SelectButton.onClick.RemoveAllListeners();
            slot.SelectButton.onClick.AddListener(() => ToggleCrew(slot, data));
        }
    }

    private void ToggleCrew(CrewCardSlot slot, CrewData data)
    {
        if (_selectedData.Contains(data))
        {
            _selectedData.Remove(data);

            if (slot.CardImage != null)
                slot.CardImage.color = Color.white;
        }
        else
        {
            _selectedData.Add(data);

            if (slot.CardImage != null)
                slot.CardImage.color = Color.green;
        }

        if (ConfirmButton != null)
            ConfirmButton.interactable = _selectedData.Count > 0;
    }

    private void OnConfirmClicked()
    {
        // Only NOW do selected picks become real GameObjects in the scene.
        // Anything not selected is simply discarded data — nothing was ever
        // instantiated for it, so there's nothing to clean up.
        foreach (var data in _selectedData)
        {
            Crew crew = CrewManager.InstantiateFromData(data);
            CrewManager.HireCrewFree(crew);
        }

        Debug.Log($"Selected = {_selectedData.Count}");
        Debug.Log($"CrewManager.Crews = {CrewManager.Crews.Count}");

        _selectedData.Clear();
        _offeredData.Clear();

        if (IntakePanel != null) IntakePanel.SetActive(false);

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }
}

/// <summary>
/// One UI card's fields. Assign in the Inspector to whatever TextMeshPro
/// objects, Button, and background Image make up a crew card in your panel
/// layout.
/// </summary>
[Serializable]
public class CrewCardSlot
{
    public Button SelectButton;
    public Image CardImage;

    public TMP_Text NameText;
    public TMP_Text HpText;
    public TMP_Text SpeedText;
    public TMP_Text GatheringText;
    public TMP_Text SearchingText;
    public TMP_Text HuntingText;
    public TMP_Text PerksText;
}
