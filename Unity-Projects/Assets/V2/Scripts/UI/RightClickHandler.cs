using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Reports right-clicks on a UI element. Unity's Button only raises left
/// clicks, but the skill triggers use right-click to clear a queued count.
/// Lives on the same object as the Button so the click area is identical.
/// </summary>
public class RightClickHandler : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        Clicked?.Invoke();
    }
}
