using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Click-to-end-day on the Day frame (spec §4.4): the frame itself is the
/// button, so there is no separate End Day widget and no confirm dialog - the
/// food bill is already on screen and the log records what happened.
///
/// A uGUI click only reaches this component when a Graphic inside the frame has
/// raycastTarget on; otherwise the pointer falls through to whatever sits behind
/// it. EnsureClickSurface repairs that and complains loudly if it cannot,
/// because an invisible miss is very hard to diagnose in game.
/// </summary>
public class EndDayTrigger : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Optional hint painted on the frame; left alone when unassigned.")]
    [SerializeField] private TMP_Text hintLabel;
    [SerializeField] private string hintText = "click to end day";

    [Tooltip("Shown when the player clicks after the run is already decided.")]
    [SerializeField] private string runOverMessage = "The run is already over.";

    private void Awake()
    {
        EnsureClickSurface();
    }

    private void Start()
    {
        if (hintLabel != null && !string.IsNullOrEmpty(hintText)) hintLabel.text = hintText;
    }

    /// <summary>
    /// Guarantees at least one hit-testable Graphic under the frame, so the frame
    /// is clickable regardless of how the scene was authored.
    /// </summary>
    private void EnsureClickSurface()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.raycastTarget) return;
        }

        if (TryGetComponent(out Graphic own))
        {
            own.raycastTarget = true;
            return;
        }

        Debug.LogWarning("[EndDayTrigger] No raycast target in the Day frame - " +
                         "adding a transparent one so End Day stays clickable.");
        var hitArea = gameObject.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0f);
        hitArea.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var days = DayManager.Instance;
        if (days == null) return;

        if (days.Outcome != RunOutcome.InProgress)
        {
            Warn(runOverMessage);
            return;
        }

        days.EndDay();
    }

    private static void Warn(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        var warning = FindAnyObjectByType<WarningView>();
        if (warning != null) warning.Show(message, LogKind.Warning);
        RunLog.Add(LogKind.Warning, message);
    }
}