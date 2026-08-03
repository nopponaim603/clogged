using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time, start-of-run screen: offers a set number of crew (default 3),
/// each with a random appearance drawn from CrewPrefabPool and fully
/// randomized stats, shows their stats, and lets the player accept all of
/// them for free by pressing Confirm. GameManager calls ShowIntake() the
/// first time the ShipDay scene loads with no crew hired yet, and the day
/// only starts once this completes.
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
    private readonly List<Crew> _offeredCrews = new List<Crew>();
    private readonly List<Crew> _selectedCrews = new List<Crew>();
    
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
    /// Rolls a fresh set of crew, shows the panel, and invokes onComplete once
    /// the player confirms.
    /// </summary>
    public void ShowIntake(Action onComplete)
    {
        _onComplete = onComplete;
        
        _selectedCrews.Clear();

        if (ConfirmButton != null)
            ConfirmButton.interactable = false;
        
        GenerateOffer();

        if (IntakePanel != null)
            IntakePanel.SetActive(true);
        
    }

    private void GenerateOffer()
    {
        _offeredCrews.Clear();

        int count = Mathf.Min(OfferCount, CardSlots.Length);

        // Work off a copy so each offer can pick distinct appearances without
        // permanently draining the configured pool.
        var pickPool = new List<GameObject>(CrewPrefabPool);

        for (int i = 0; i < count; i++)
        {
            Crew crew;

            if (pickPool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pickPool.Count);
                GameObject appearance = pickPool[index];
                pickPool.RemoveAt(index); // no duplicate look within a single offer

                crew = CrewManager.CreateRandomCrewFromAppearance(appearance, 0);
            }
            else
            {
                // Pool empty (or none configured) — plain random crew, no particular sprite.
                crew = CrewManager.CreateRandomCrew(0);
            }

            if (crew == null) continue;

            _offeredCrews.Add(crew);
            PopulateCard(CardSlots[i], crew);
            
            
        }
        // Hide Unused Slot 
        for (int i = count; i < CardSlots.Length; i++)
        {
            if (CardSlots[i] != null &&
                CardSlots[i].SelectButton != null)
            {
                CardSlots[i].SelectButton.gameObject.SetActive(false);
            }
        }
    }

    private void PopulateCard(CrewCardSlot slot, Crew crew)
    {
        if (slot == null) return;
        
        if (slot.SelectButton != null)
            slot.SelectButton.gameObject.SetActive(true);

        if (slot.NameText != null) slot.NameText.text = crew.Name;
        if (slot.HpText != null) slot.HpText.text = "HP: " + Mathf.RoundToInt(crew.Hp);
        if (slot.SpeedText != null) slot.SpeedText.text = "Speed: " + crew.Speed.ToString("0.0");
        if (slot.GatheringText != null) slot.GatheringText.text = "Gathering: " + crew.GatheringProficiency.ToString("0.0");
        if (slot.SearchingText != null) slot.SearchingText.text = "Searching: " + crew.SearchingProficiency.ToString("0.0");
        if (slot.HuntingText != null) slot.HuntingText.text = "Hunting: " + crew.HuntingProficiency.ToString("0.0");
        if (slot.PerksText != null) slot.PerksText.text = "Perks: " + string.Join(", ", crew.Perks);
        
        if (slot.CardImage != null)
            slot.CardImage.color = Color.white;

        if (slot.SelectButton != null)
        {
            slot.SelectButton.onClick.RemoveAllListeners();
            slot.SelectButton.onClick.AddListener(() =>
            {
                ToggleCrew(slot, crew);
            });
        }
    }
    
    private void ToggleCrew(CrewCardSlot slot, Crew crew)
    {
        if (_selectedCrews.Contains(crew))
        {
            _selectedCrews.Remove(crew);

            if (slot.CardImage != null)
                slot.CardImage.color = Color.white;
        }
        else
        {
            _selectedCrews.Add(crew);

            if (slot.CardImage != null)
                slot.CardImage.color = Color.green;
        }
        if (ConfirmButton != null) ConfirmButton.interactable = _selectedCrews.Count > 0;
    }

    private void OnConfirmClicked()
    {
        foreach (var crew in _offeredCrews)
        {
            if (_selectedCrews.Contains(crew))
            {
                CrewManager.HireCrewFree(crew);
            }
            else
            {
                Destroy(crew.gameObject);
            }
        }
        Debug.Log($"Selected = {_selectedCrews.Count}");
        Debug.Log($"CrewManager.Crews = {CrewManager.Crews.Count}");

        _selectedCrews.Clear();
        _offeredCrews.Clear();

        if (IntakePanel != null) IntakePanel.SetActive(false);

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }
}

/// <summary>
/// One UI card's text fields. Assign in the Inspector to whatever
/// TextMeshPro objects make up a crew card in your panel layout.
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
