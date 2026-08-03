using UnityEngine;
using UnityEngine.UI;

public class BaseHPUI : MonoBehaviour
{
    public BaseHealth baseHealth;
    public Slider slider;

    void Start()
    {
        slider.maxValue = baseHealth.maxHP;
    }

    void Update()
    {
        slider.value = baseHealth.GetHP();
    }
}