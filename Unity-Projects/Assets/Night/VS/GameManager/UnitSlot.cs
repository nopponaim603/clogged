using UnityEngine;

public class UnitSlot : MonoBehaviour
{
    public GameObject unitPrefab;

    private static UnitSlot selectedSlot;

    public void SelectUnit()
    {
        selectedSlot = this;

        Debug.Log("เลือกยูนิตแล้ว");
    }

    public static GameObject GetSelectedUnit()
    {
        if (selectedSlot == null)
            return null;

        return selectedSlot.unitPrefab;
    }

    public static void ClearSelection()
    {
        selectedSlot = null;
    }
}