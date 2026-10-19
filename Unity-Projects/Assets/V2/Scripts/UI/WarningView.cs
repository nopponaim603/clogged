using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum WarningKind
{
    /// <summary>Rule violations the player must fix (no cave picked, unit unavailable).</summary>
    Warning,
    /// <summary>Hard refusals such as exceeding the selection limit.</summary>
    Error,
    /// <summary>Neutral in-game notice.</summary>
    Info,
}

/// <summary>
/// In-game warning banner that replaces raw debug text. Shows one queued
/// message at a time, colored by severity, fades in, holds, then fades out.
/// Attach to a Canvas object with a TMP_Text and a CanvasGroup.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class WarningView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup backdropGroup;
    [SerializeField] private float holdSeconds = 2.5f;
    [SerializeField] private float fadeSeconds = 0.3f;
    [SerializeField] private float spacingSeconds = 0.15f;

    [Header("Colors")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("Outline colour per severity — the text stays white, only the edge changes.")]
    [SerializeField] private Color warningOutline = new Color(1f, 0.78f, 0.12f);
    [SerializeField] private Color errorOutline = new Color(1f, 0.25f, 0.22f);
    [SerializeField] private Color infoOutline = new Color(0.35f, 0.78f, 1f);

    [Header("Outline")]
    [Tooltip("TMP material outline, added at runtime. UGUI Outline does not render on TMP text.")]
    [SerializeField] private float outlineWidth = 0.28f;

    private readonly Queue<KeyValuePair<string, WarningKind>> queue = new Queue<KeyValuePair<string, WarningKind>>();
    private CanvasGroup group;
    private Coroutine routine;
    private Material outlineMaterial;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);

        // UGUI Outline/Shadow are no-ops on TMP, so the edge is painted into a
        // text material instance. Keeping the instance lets each message just
        // recolour the outline while the glyphs stay white.
        outlineMaterial = TmpOutlineUtil.CreateOutlineMaterial(label, warningOutline, outlineWidth);
        if (outlineMaterial != null)
        {
            label.fontMaterial = outlineMaterial;
            label.color = textColor;
        }
        else if (label != null)
        {
            label.color = textColor;
        }

        SetAlpha(0f);
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    /// <summary>Queues a message. Repeated identical messages refresh instead of stacking.</summary>
    public void Show(string message, WarningKind kind = WarningKind.Warning)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        queue.Enqueue(new KeyValuePair<string, WarningKind>(message, kind));
        if (routine == null) routine = StartCoroutine(RunQueue());
    }

    public void ShowWarning(string message) => Show(message, WarningKind.Warning);
    public void ShowError(string message) => Show(message, WarningKind.Error);
    public void ShowInfo(string message) => Show(message, WarningKind.Info);

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
                label.color = textColor;
                TmpOutlineUtil.SetOutlineColor(outlineMaterial, OutlineFor(entry.Value));
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
        if (backdropGroup != null) backdropGroup.alpha = alpha;
    }

    private Color OutlineFor(WarningKind kind)
    {
        switch (kind)
        {
            case WarningKind.Error: return errorOutline;
            case WarningKind.Info: return infoOutline;
            default: return warningOutline;
        }
    }
}
