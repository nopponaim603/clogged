
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MissionGateEffect : MonoBehaviour
{
    [Header("Gate UI")]
    public RawImage gateImage;

    [Header("Glow Effect")]
    public Color glowColor = new Color(0.7f, 0.1f, 1f, 1f);
    public float pulseSpeed = 2f;

    [Header("Movement")]
    public float scaleAmount = 0.04f;

    private Color originalColor;
    private Vector3 originalScale;
    private Coroutine effectCoroutine;

    private void Awake()
    {
        if (gateImage == null)
            gateImage = GetComponent<RawImage>();

        originalScale = transform.localScale;

        if (gateImage != null)
            originalColor = gateImage.color;
    }

    public void StartMission()
    {
        if (gateImage == null)
        {
            Debug.LogError("MissionGateEffect: ไม่พบ RawImage!");
            return;
        }

        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(GlowRoutine());
    }

    public void FinishMission()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        if (gateImage != null)
            gateImage.color = originalColor;

        transform.localScale = originalScale;
    }

    private IEnumerator GlowRoutine()
    {
        while (true)
        {
            float pulse =
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            gateImage.color =
                Color.Lerp(originalColor, glowColor, pulse);

            transform.localScale =
                originalScale * (1f + pulse * scaleAmount);

            yield return null;
        }
    }

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        if (gateImage != null)
            gateImage.color = originalColor;

        transform.localScale = originalScale;
    }
}
