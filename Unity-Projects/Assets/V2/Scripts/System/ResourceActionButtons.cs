using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceActionButtons : MonoBehaviour
{
    [Header("Selected Unit")]
    [SerializeField] private UnitCharacter selectedUnit;

    [Header("Buttons")]
    [SerializeField] private Button cookButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button gatherMedicineButton;

    [Header("UI Text")]
    [SerializeField] private TMP_Text apText;
    [SerializeField] private TMP_Text statusText;

    private RunResources resources;

    private void Start()
    {
        resources = RunResources.Instance;

        if (resources == null)
        {
            Debug.LogError("ไม่พบ RunResources ในฉาก");
            enabled = false;
            return;
        }

        UpdateUI();
    }

    // รับตัวละครจากระบบเลือกตัวละคร
    public void SetSelectedUnit(UnitCharacter unit)
    {
        selectedUnit = unit;
        UpdateUI();
    }

    // ปุ่ม 1: ใช้ CookingMaterial 1 + AP 1 ได้ Food 1
    public void CookFood()
    {
        if (!CanPerformAction())
            return;

        if (resources.CookingMaterial < 1)
        {
            ShowMessage("CookingMaterial ไม่เพียงพอ");
            return;
        }

        if (!selectedUnit.TrySpendActionPoints(1))
        {
            ShowMessage("AP ไม่เพียงพอ");
            UpdateUI();
            return;
        }

        resources.TryConsumeItem(
            InventoryItemType.CookingMaterial, 1);

        resources.AddFood(1);

        ShowMessage("ทำอาหารสำเร็จ: Food +1");
        UpdateUI();
    }

    // ปุ่ม 2: รักษาอาการบาดเจ็บ
    // มียาใช้ AP 1 / ไม่มียาใช้ AP 2
    public void HealInjury()
    {
        if (!CanPerformAction())
            return;

        if (selectedUnit.state != UnitState.Hurt)
        {
            ShowMessage("ตัวละครไม่มีอาการบาดเจ็บ");
            return;
        }

        bool hasMedicine = resources.Medicine >= 1;
        int apCost = hasMedicine ? 1 : 2;

        if (!selectedUnit.TrySpendActionPoints(apCost))
        {
            ShowMessage("AP ไม่เพียงพอ");
            UpdateUI();
            return;
        }

        if (hasMedicine)
        {
            resources.TryConsumeItem(
                InventoryItemType.Medicine, 1);
        }

        selectedUnit.state = UnitState.Idle;

        ShowMessage(
            hasMedicine
                ? "รักษาสำเร็จ: ใช้ Medicine 1"
                : "รักษาสำเร็จ: ใช้ AP 2 โดยไม่ใช้ยา"
        );

        UpdateUI();
    }

    // ปุ่ม 3: ใช้ Materials 1 + AP 1 ได้ Medicine 1
    public void GatherMedicine()
    {
        if (!CanPerformAction())
            return;

        if (resources.Materials < 1)
        {
            ShowMessage("Materials ไม่เพียงพอ");
            return;
        }

        if (!selectedUnit.TrySpendActionPoints(1))
        {
            ShowMessage("AP ไม่เพียงพอ");
            UpdateUI();
            return;
        }

        resources.TryConsumeItem(
            InventoryItemType.Materials, 1);

        resources.AddItem(
            InventoryItemType.Medicine, 1);

        ShowMessage("ได้รับ Medicine +1");
        UpdateUI();
    }

    // ตรวจสอบว่าตัวละครพร้อมทำกิจกรรมหรือไม่
    private bool CanPerformAction()
    {
        if (selectedUnit == null)
        {
            ShowMessage("กรุณาเลือกตัวละครก่อน");
            return false;
        }

        if (selectedUnit.state != UnitState.Idle &&
            selectedUnit.state != UnitState.Hurt)
        {
            ShowMessage("ตัวละครนี้ไม่พร้อมทำกิจกรรม");
            return false;
        }

        if (resources == null)
        {
            ShowMessage("ไม่พบ RunResources");
            return false;
        }

        return true;
    }

    private void UpdateUI()
    {
        if (selectedUnit == null)
        {
            if (apText != null)
                apText.text = "AP: -";

            SetButtons(false, false, false);
            return;
        }

        if (apText != null)
        {
            int actualCost = selectedUnit.GetActualApCost(1);

            apText.text =
                "AP: " + selectedUnit.currentActionPoints +
                "/" + selectedUnit.maxActionPoints +
                "\nค่า AP ต่อการกระทำ 1 หน่วย: " + actualCost;
        }

        bool canAct =
            selectedUnit.state == UnitState.Idle ||
            selectedUnit.state == UnitState.Hurt;

        SetButtons(
            canAct && resources.CookingMaterial >= 1,
            selectedUnit.state == UnitState.Hurt,
            canAct && resources.Materials >= 1
        );
    }

    private void SetButtons(
        bool canCook,
        bool canHeal,
        bool canGatherMedicine)
    {
        if (cookButton != null)
            cookButton.interactable = canCook;

        if (healButton != null)
            healButton.interactable = canHeal;

        if (gatherMedicineButton != null)
            gatherMedicineButton.interactable = canGatherMedicine;
    }

    private void ShowMessage(string message)
    {
        Debug.Log(message);

        if (statusText != null)
            statusText.text = message;
    }
}