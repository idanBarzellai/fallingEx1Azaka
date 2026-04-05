using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MissileUI : MonoBehaviour, IPointerDownHandler
{
    public RectTransform rectTransform;
    public Image image;

    private Coroutine moveRoutine;
    private Action tapCallback;
    private bool isActiveMissile = false;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            image.raycastTarget = true;

        HideMissile();
    }

    public void Launch(Vector2 startPos, Vector2 endPos, float duration, Action onImpact, Action onTapped)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        tapCallback = onTapped;
        isActiveMissile = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        rectTransform.anchoredPosition = startPos;

        Vector2 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

        moveRoutine = StartCoroutine(MoveRoutine(startPos, endPos, duration, onImpact));
    }

    private IEnumerator MoveRoutine(Vector2 startPos, Vector2 endPos, float duration, Action onImpact)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        rectTransform.anchoredPosition = endPos;
        isActiveMissile = false;
        onImpact?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveMissile) return;

        isActiveMissile = false;
        tapCallback?.Invoke();
    }

    public void HideMissile()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isActiveMissile = false;
        tapCallback = null;
        gameObject.SetActive(false);
    }
}