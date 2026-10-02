using UnityEngine;

/// <summary>
/// One clickable point on the gather map. Multiple nodes can share the same
/// zone (e.g. 2 "Near" nodes) - they're purely a visual/selection choice,
/// all nodes of the same zone resolve through identical GatherResolver logic.
/// </summary>
[System.Serializable]
public class MapNodeData
{
    public string nodeId;
    public GatherZone zone;
    public Vector2 anchoredPosition;
}
