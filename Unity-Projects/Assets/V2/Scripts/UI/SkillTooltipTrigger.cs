using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Opens the shared <see cref="SkillTooltip"/> when the pointer rests on a
/// skill button.
///
/// The delay matters: without it every pass of the cursor across the column
/// flashes a popup. Leaving cancels a pending show as well as an open one, so
/// a fast sweep never leaves a tooltip behind.
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string title;
    [SerializeField] private string body;
    [SerializeField] private string leftHint;
    [SerializeField] private string rightHint;
    [SerializeField] private Sprite leftIcon;
    [SerializeField] private Sprite rightIcon;
    [SerializeField] private float showDelay = 0.35f;

    private Coroutine pending;

    /// <summary>Fills in the content. Called by the panel that owns the buttons.</summary>
    public void Configure(string tipTitle, string tipBody, string leftClickHint, string rightClickHint,
        Sprite leftClickIcon, Sprite rightClickIcon)
    {
        title = tipTitle;
        body = tipBody;
        leftHint = leftClickHint;
        rightHint = rightClickHint;
        leftIcon = leftClickIcon;
        rightIcon = rightClickIcon;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cancel();
        if (!isActiveAndEnabled) return;
        pending = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData) => Cancel();

    private void OnDisable() => Cancel();

    private void Cancel()
    {
        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }

        SkillTooltip.Hide();
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, showDelay));
        pending = null;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        SkillTooltip.ShowNow(canvas, (RectTransform)transform, leftIcon, rightIcon, title, body, leftHint, rightHint);
    }
}
