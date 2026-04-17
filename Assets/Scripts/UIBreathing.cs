using UnityEngine;

public class UIBreathing : MonoBehaviour
{
    public RectTransform target;

    [Header("Breathing Settings")]
    public float scaleAmount = 0.05f; // how much it grows (5%)
    public float speed = 1.5f;        // how fast it breathes

    private Vector3 baseScale;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        baseScale = target.localScale;
    }

    private void OnEnable()
    {
        baseScale = target.localScale;
    }

    private void Update()
    {
        float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f; 
        float scale = 1f + (wave * scaleAmount);

        target.localScale = baseScale * scale;
    }

    private void OnDisable()
    {
        target.localScale = baseScale;
    }
}