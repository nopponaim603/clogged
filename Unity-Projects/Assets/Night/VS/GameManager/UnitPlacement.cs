using UnityEngine;

public class UnitPlacement : MonoBehaviour
{
    private bool selected = false;

    public void SelectUnit()
    {
        selected = true;
        Debug.Log("เลือกยูนิตแล้ว");
    }

    private void OnMouseDown()
    {
        SelectUnit();
    }

    public bool IsSelected()
    {
        return selected;
    }

    public void PlaceUnit(Transform point)
    {
        transform.position = point.position;
        selected = false;

        Debug.Log("วางยูนิตที่ " + point.name);
    }
}