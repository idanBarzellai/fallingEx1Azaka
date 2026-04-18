using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIImageFrameAnimator : MonoBehaviour
{
    [Header("References")]
    public Image targetImage;

    [Header("Frames")]
    public Sprite[] frames;

    [Header("Playback")]
    public float framesPerSecond = 12f;
    public bool playOnEnable = false;

    private Coroutine playRoutine;
    private int currentFrameIndex = 0;
    private bool isPaused = false;
    private bool isPlaying = false;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayFromStart();
    }

    public void PlayFromStart()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        Stop();

        currentFrameIndex = 0;
        isPaused = false;
        isPlaying = true;
        targetImage.sprite = frames[currentFrameIndex];

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Resume()
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        if (isPlaying || !isPaused)
            return;

        isPaused = false;
        isPlaying = true;
        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Pause()
    {
        if (!isPlaying)
            return;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        isPlaying = false;
        isPaused = true;
    }

    public void Stop()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        isPlaying = false;
        isPaused = false;
    }

    public void ShowFirstFrame()
    {
        Stop();

        currentFrameIndex = 0;

        if (targetImage != null && frames != null && frames.Length > 0)
            targetImage.sprite = frames[0];
    }

    private IEnumerator PlayRoutine()
    {
        float frameDelay = 1f / Mathf.Max(1f, framesPerSecond);

        while (currentFrameIndex < frames.Length)
        {
            targetImage.sprite = frames[currentFrameIndex];
            yield return new WaitForSecondsRealtime(frameDelay);
            currentFrameIndex++;
        }

        currentFrameIndex = Mathf.Clamp(currentFrameIndex - 1, 0, frames.Length - 1);
        isPlaying = false;
        isPaused = false;
        playRoutine = null;
    }

    public bool IsPlaying => isPlaying;
    public bool IsPaused => isPaused;
}