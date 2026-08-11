using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-day summary screen. GameManager.HandleDayEnded() calls Show() once
/// the day's timer runs out, passing what was gained today, what was lost
/// overnight, and whether everyone got fed. The player reviews it and
/// presses Continue to move on (GameManager wires this to GoToNight()).
/// </summary>
public class DayEndSummaryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject SummaryPanel;
    public Button ContinueButton;

    [Header("Text Fields")]
    public TMP_Text FoodStatusText;
    public TMP_Text GainedText;
    public TMP_Text LostText;

    [Header("Messages")]
    public string FedEveryoneMessage = "Everyone ate well tonight.";
    public string WentHungryMessage = "Food ran out — the crew went hungry!";
    public string NothingGainedMessage = "Nothing gathered today.";
    public string NothingLostMessage = "Nothing lost overnight.";

    private Action _onContinue;

    private void Awake()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(OnContinueClicked);

        if (SummaryPanel != null)
            SummaryPanel.SetActive(false);
    }

    /// <summary>
    /// Populates and shows the summary panel. onContinue is invoked once the
    /// player presses the Continue button.
    /// </summary>
    public void Show(Dictionary<string, int> gainedToday, Dictionary<string, int> lostOvernight, bool fedEveryone, Action onContinue)
    {
        _onContinue = onContinue;

        if (FoodStatusText != null)
            FoodStatusText.text = fedEveryone ? FedEveryoneMessage : WentHungryMessage;

        if (GainedText != null)
            GainedText.text = BuildResourceLines(gainedToday, "+", NothingGainedMessage);

        if (LostText != null)
            LostText.text = BuildResourceLines(lostOvernight, "-", NothingLostMessage);

        if (SummaryPanel != null)
            SummaryPanel.SetActive(true);
    }

    private string BuildResourceLines(Dictionary<string, int> resources, string sign, string emptyMessage)
    {
        if (resources == null || resources.Count == 0)
            return emptyMessage;

        var sb = new StringBuilder();
        bool any = false;

        foreach (var kvp in resources)
        {
            if (kvp.Value <= 0) continue;

            if (any) sb.Append('\n');
            sb.Append(CapitalizeFirst(kvp.Key)).Append(": ").Append(sign).Append(kvp.Value);
            any = true;
        }

        return any ? sb.ToString() : emptyMessage;
    }

    private string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return char.ToUpper(text[0]) + text.Substring(1);
    }

    private void OnContinueClicked()
    {
        if (SummaryPanel != null)
            SummaryPanel.SetActive(false);

        var callback = _onContinue;
        _onContinue = null;
        callback?.Invoke();
    }
}
