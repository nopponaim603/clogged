using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Placeholder "scout walking" animation: moves this UI element's anchored
/// position from wherever it is to a target over moveDuration seconds. Swap
/// for a sprite-sheet walk animation later - the MoveTo contract stays the same.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ScoutWalkerView : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.6f;

    private RectTransform rect;

    private void Awake()
    {
        rect = (RectTransform)transform;
    }

    public void MoveTo(Vector2 targetAnchoredPosition, Action onArrived)
    {
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(targetAnchoredPosition, onArrived));
    }

    public void SnapTo(Vector2 anchoredPosition)
    {
        StopAllCoroutines();
        rect.anchoredPosition = anchoredPosition;
    }

    private IEnumerator MoveRoutine(Vector2 target, Action onArrived)
    {
        Vector2 start = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            rect.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        rect.anchoredPosition = target;
        onArrived?.Invoke();
    }
}
