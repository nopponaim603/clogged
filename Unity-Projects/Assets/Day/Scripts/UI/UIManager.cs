using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text ResourceText;
    public TMP_Text StatusText;
    public ResourceManager ResourceManager;

    private void Update()
    {
        if (ResourceManager == null || ResourceText == null) return;

        ResourceText.text =
            $"Food: {ResourceManager.GetResource("food")} | " +
            $"Wood: {ResourceManager.GetResource("wood")} | " +
            $"Relic: {ResourceManager.GetResource("relic")} | " +
            $"Base HP: {ResourceManager.BaseHp}";
    }

    public void ShowMessage(string message)
    {
        if (StatusText != null)
        {
            StatusText.text = message;
        }

        Debug.Log(message);
    }
}
