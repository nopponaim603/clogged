using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeGaugeUI : MonoBehaviour
{
    public TimeManager TimeManager;
    public Image FillImage;
    public TMP_Text TimeText;

    private void Update()
    {
        if (TimeManager == null || FillImage == null || TimeText == null)
            return;

        float ratio = Mathf.Clamp01(TimeManager.CurrentTime / TimeManager.DayLength);
        FillImage.fillAmount = ratio;
        TimeText.text = "Time: " + Mathf.CeilToInt(TimeManager.CurrentTime);
    }
}