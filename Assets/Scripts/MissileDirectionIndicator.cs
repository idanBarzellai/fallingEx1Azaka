using UnityEngine;
using UnityEngine.UI;

public class MissileDirectionIndicator : MonoBehaviour
{
    public RectTransform rectTransform;
    public Image image;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        gameObject.SetActive(false);
    }

    public void Show(MissileSpawnSide side)
    {
        gameObject.SetActive(true);

        switch (side)
        {
            case MissileSpawnSide.Eilat:
                rectTransform.anchoredPosition = new Vector2(850f, 0f);
                rectTransform.rotation = Quaternion.Euler(0f, 0f, 90f);
                break;

            case MissileSpawnSide.South:
                rectTransform.anchoredPosition = new Vector2(0f, -500f);
                rectTransform.rotation = Quaternion.identity;
                break;

            case MissileSpawnSide.Center:
                rectTransform.anchoredPosition = new Vector2(0f, 500f);
                rectTransform.rotation = Quaternion.Euler(0f, 0f, 180f);
                break;

            case MissileSpawnSide.Sharon:
                rectTransform.anchoredPosition = new Vector2(-850f, 0f);
                rectTransform.rotation = Quaternion.Euler(0f, 0f, -90f);
                break;

            case MissileSpawnSide.North:
                rectTransform.anchoredPosition = new Vector2(0f, 500f);
                rectTransform.rotation = Quaternion.Euler(0f, 0f, 180f);
                break;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}