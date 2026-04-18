using System.Collections;
using UnityEngine;

public class UIShake : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    private Vector2 originalAnchoredPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target != null)
            originalAnchoredPosition = target.anchoredPosition;
    }

    private void OnEnable()
    {
        if (target != null)
            originalAnchoredPosition = target.anchoredPosition;
    }

    public void Shake(float duration = 0.2f, float strength = 18f, float frequency = 30f)
    {
        if (target == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength, frequency));
    }

    private IEnumerator ShakeRoutine(float duration, float strength, float frequency)
    {
        originalAnchoredPosition = target.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * strength * damper;

            target.anchoredPosition = originalAnchoredPosition + offset;

            yield return null;
        }

        target.anchoredPosition = originalAnchoredPosition;
        shakeRoutine = null;
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (target != null)
            target.anchoredPosition = originalAnchoredPosition;
    }
}