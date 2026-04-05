using UnityEngine;
using UnityEngine.UI;

public class MissileDirectionIndicator : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image image;
    public float edgePadding = 70f;

    private Rect visibleRect;
    private RectTransform targetMissile;
    private bool isTracking = false;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponentInChildren<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        gameObject.SetActive(false);
    }

    public void BeginTracking(RectTransform missileRect, Rect cameraRect)
    {
        targetMissile = missileRect;
        visibleRect = cameraRect;
        isTracking = true;
        gameObject.SetActive(true);
        UpdatePositionImmediate();
    }

    public void StopTracking()
    {
        isTracking = false;
        targetMissile = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isTracking || targetMissile == null)
            return;

        UpdatePositionImmediate();
    }

    private void UpdatePositionImmediate()
    {
        Vector2 missilePos = targetMissile.anchoredPosition;

        if (visibleRect.Contains(missilePos))
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        float clampedX = Mathf.Clamp(
            missilePos.x,
            visibleRect.xMin + edgePadding,
            visibleRect.xMax - edgePadding
        );

        float clampedY = Mathf.Clamp(
            missilePos.y,
            visibleRect.yMin + edgePadding,
            visibleRect.yMax - edgePadding
        );

        bool outsideHorizontal = missilePos.x < visibleRect.xMin || missilePos.x > visibleRect.xMax;
        bool outsideVertical = missilePos.y < visibleRect.yMin || missilePos.y > visibleRect.yMax;

        if (outsideHorizontal && outsideVertical)
        {
            float dxLeft = Mathf.Abs(missilePos.x - visibleRect.xMin);
            float dxRight = Mathf.Abs(missilePos.x - visibleRect.xMax);
            float dyBottom = Mathf.Abs(missilePos.y - visibleRect.yMin);
            float dyTop = Mathf.Abs(missilePos.y - visibleRect.yMax);

            float minDist = Mathf.Min(dxLeft, dxRight, dyBottom, dyTop);

            if (minDist == dxLeft) clampedX = visibleRect.xMin + edgePadding;
            else if (minDist == dxRight) clampedX = visibleRect.xMax - edgePadding;
            else if (minDist == dyBottom) clampedY = visibleRect.yMin + edgePadding;
            else clampedY = visibleRect.yMax - edgePadding;
        }
        else
        {
            if (missilePos.x < visibleRect.xMin) clampedX = visibleRect.xMin + edgePadding;
            if (missilePos.x > visibleRect.xMax) clampedX = visibleRect.xMax - edgePadding;
            if (missilePos.y < visibleRect.yMin) clampedY = visibleRect.yMin + edgePadding;
            if (missilePos.y > visibleRect.yMax) clampedY = visibleRect.yMax - edgePadding;
        }

        rectTransform.anchoredPosition = new Vector2(clampedX, clampedY);

        // Vector2 toMissile = missilePos - rectTransform.anchoredPosition;
        // float angle = Mathf.Atan2(toMissile.y, toMissile.x) * Mathf.Rad2Deg;
        // rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}