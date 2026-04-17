using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : MonoBehaviour
{
    public Image image;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
    }

    public void FadeOut(float fadeDuration = 1f)
    {
        StartCoroutine(FadeOutRoutine(fadeDuration));
    }

    public void FadeIn()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (image != null && !image.gameObject.activeSelf)
            image.gameObject.SetActive(true);

        Color currentColor = image.color;
        image.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeOutRoutine(float fadeDuration = 1f)
    {
        float elapsed = 0f;
        Color startColor = image.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            image.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha
            );

            yield return null;
        }

        // ensure fully invisible
        image.color = new Color(
            startColor.r,
            startColor.g,
            startColor.b,
            0f
        );
          if (gameObject != null)
            gameObject.SetActive(false);
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        Color startColor = image.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);

            image.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha
            );

            yield return null;
        }

        // ensure fully visible
        image.color = new Color(
            startColor.r,
            startColor.g,
            startColor.b,
            1f
        );
    }
}