using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// In-game warning banner: one queued message at a time, fades in, holds, fades
/// out. The text takes its colour from <see cref="UiPalette"/>, the same table
/// the log row for that message uses, so a line never looks amber in the log and
/// red on the banner.
///
/// Attach to the banner root: one CanvasGroup there fades the backdrop art and
/// the label together, so the two never drift apart.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class WarningView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("Optional separate group. Leave empty when the backdrop is a child of this object.")]
    [SerializeField] private CanvasGroup backdropGroup;
    [SerializeField] private float holdSeconds = 2.5f;
    [SerializeField] private float fadeSeconds = 0.3f;
    [SerializeField] private float spacingSeconds = 0.15f;

    private readonly Queue<KeyValuePair<string, LogKind>> queue = new Queue<KeyValuePair<string, LogKind>>();
    private CanvasGroup group;
    private Coroutine routine;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        SetAlpha(0f);
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    /// <summary>
    /// Queues a message. Pass the same <see cref="LogKind"/> that goes into the
    /// log for this event; that is what decides the colour.
    /// </summary>
    public void Show(string message, LogKind kind = LogKind.Warning)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        queue.Enqueue(new KeyValuePair<string, LogKind>(message, kind));
        if (routine == null) routine = StartCoroutine(RunQueue());
    }

    public void Clear()
    {
        queue.Clear();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        SetAlpha(0f);
    }

    private IEnumerator RunQueue()
    {
        while (queue.Count > 0)
        {
            var entry = queue.Dequeue();

            if (label != null)
            {
                label.text = entry.Key;
                label.color = UiPalette.For(entry.Value);
            }

            yield return Fade(0f, 1f, fadeSeconds);
            yield return new WaitForSeconds(holdSeconds);
            yield return Fade(1f, 0f, fadeSeconds);
            yield return new WaitForSeconds(spacingSeconds);
        }

        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        SetAlpha(from);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (group != null) group.alpha = alpha;

        // The backdrop is normally a child of this object, so the group above
        // already fades it. Only a separately parented backdrop needs its own
        // group driven.
        if (backdropGroup != null && backdropGroup != group) backdropGroup.alpha = alpha;
    }
}
