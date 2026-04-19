using UnityEngine;
using UnityEngine.UI;

public class MissileDirectionIndicator : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image image;
    public float edgePadding = 70f;

    private RectTransform targetMissile;
    private RectTransform boundsRect;
    private bool isTracking = false;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        gameObject.SetActive(false);
    }

    public void BeginTracking(RectTransform missileRect, RectTransform trackingBounds)
    {
        targetMissile = missileRect;
        boundsRect = trackingBounds;
        isTracking = true;
        gameObject.SetActive(true);
        UpdatePositionImmediate();
    }

    public void StopTracking()
    {
        isTracking = false;
        targetMissile = null;
        boundsRect = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isTracking || targetMissile == null || boundsRect == null)
            return;

        UpdatePositionImmediate();
    }

    private void UpdatePositionImmediate()
    {
        Rect rect = boundsRect.rect;

        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMin = rect.yMin;
        float yMax = rect.yMax;

        // Convert missile position into boundsRect local space
        Vector2 missileWorldPos = targetMissile.TransformPoint(targetMissile.rect.center);
        Vector2 missileLocalPos = boundsRect.InverseTransformPoint(missileWorldPos);

        if (rect.Contains(missileLocalPos))
        {
            gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        float clampedX = Mathf.Clamp(
            missileLocalPos.x,
            xMin + edgePadding,
            xMax - edgePadding
        );

        float clampedY = Mathf.Clamp(
            missileLocalPos.y,
            yMin + edgePadding,
            yMax - edgePadding
        );

        bool outsideHorizontal = missileLocalPos.x < xMin || missileLocalPos.x > xMax;
        bool outsideVertical = missileLocalPos.y < yMin || missileLocalPos.y > yMax;

        if (outsideHorizontal && outsideVertical)
        {
            float dxLeft = Mathf.Abs(missileLocalPos.x - xMin);
            float dxRight = Mathf.Abs(missileLocalPos.x - xMax);
            float dyBottom = Mathf.Abs(missileLocalPos.y - yMin);
            float dyTop = Mathf.Abs(missileLocalPos.y - yMax);

            float minDist = Mathf.Min(dxLeft, dxRight, dyBottom, dyTop);

            if (minDist == dxLeft) clampedX = xMin + edgePadding;
            else if (minDist == dxRight) clampedX = xMax - edgePadding;
            else if (minDist == dyBottom) clampedY = yMin + edgePadding;
            else clampedY = yMax - edgePadding;
        }
        else
        {
            if (missileLocalPos.x < xMin) clampedX = xMin + edgePadding;
            if (missileLocalPos.x > xMax) clampedX = xMax - edgePadding;
            if (missileLocalPos.y < yMin) clampedY = yMin + edgePadding;
            if (missileLocalPos.y > yMax) clampedY = yMax - edgePadding;
        }

        rectTransform.anchoredPosition = new Vector2(clampedX, clampedY);
    }
}