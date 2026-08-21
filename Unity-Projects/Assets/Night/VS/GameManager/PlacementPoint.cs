using UnityEngine;

public class PlacementPoint : MonoBehaviour
{
    private bool occupied = false;

    private void OnMouseDown()
    {
        if (occupied)
            return;

        GameObject unitPrefab = UnitSlot.GetSelectedUnit();

        if (unitPrefab == null)
        {
            Debug.Log("ยังไม่ได้เลือกยูนิต");
            return;
        }

        GameObject unit = Instantiate(
            unitPrefab,
            transform.position,
            Quaternion.identity
        );

        occupied = true;

        UnitSlot.ClearSelection();

        Debug.Log("วางยูนิตที่ " + gameObject.name);
    }
}