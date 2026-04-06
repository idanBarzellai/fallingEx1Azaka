using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SectorState
{
    Idle,
    Incoming,
    AlertedIncoming,
    Smoked,
    WaitingForRelease,
    NeedsAmbulanceCheck,
    AmbulanceWorking,
    NeedsAmbulance,
    Lost
}

public class SectorHandler : MonoBehaviour
{
    [Header("Sector Info")]
    public SectorName sectorName;
    public SectorState currentState = SectorState.Idle;

    [Header("Visual References")]
    public Image baseImage;
    public RectTransform vfxAnchor;

    [Header("UI VFX Prefabs")]
    public GameObject smokePrefab;
    public GameObject explosionPrefab;

    [Header("State Colors")]
    public Color idleColor = Color.white;
    public Color incomingColor = new Color(1f, 0.6f, 0.2f);
    public Color alertedIncomingColor = Color.yellow;
    public Color smokedColor = Color.gray;
    public Color waitingForReleaseColor = Color.cyan;
    public Color needsAmbulanceCheckColor = new Color(1f, 0.5f, 0f);
    public Color ambulanceWorkingColor = new Color(0.3f, 1f, 0.3f);
    public Color needsAmbulanceColor = Color.red;
    public Color lostColor = Color.black;

    [Header("Crash VFX")]
    public int crashExplosionCount = 4;
    public float timeBetweenCrashExplosions = 0.18f;
    public float crashExplosionRadius = 50f;

    [Header("Smoke VFX")]
    public int smokeCloudCount = 6;
    public float smokeScatterRadius = 80f;
    public float smokeClearAnimationDuration = 0.8f;

    [Header("State Timer UI")]
    public Image stateTimerImage;

    [Header("State Timer Icons")]
    public Sprite alertIconSprite;
    public Sprite releaseIconSprite;
    public Sprite ambulanceIconSprite;

    [Range(0f, 1f)] public float fadedHintAlpha = 0.35f;
    [Range(0f, 1f)] public float activeTimerAlpha = 0.95f;

    private Coroutine stateTimerRoutine;
    private Coroutine repeatingStateTimerRoutine;
    private Coroutine flickerRoutine;
    private Coroutine activeVfxRoutine;

    private readonly List<GameObject> activeSmokeInstances = new List<GameObject>();

    private void Awake()
    {
        AutoAssignReferences();
        UpdateVisual();
        HideStateTimer();
    }

    private void AutoAssignReferences()
    {
        if (baseImage == null)
        {
            baseImage = GetComponent<Image>();

            if (baseImage == null)
                baseImage = GetComponentInChildren<Image>(true);
        }

        if (vfxAnchor == null)
        {
            Transform anchor = transform.Find("VFXAnchor");
            if (anchor != null)
                vfxAnchor = anchor as RectTransform;
        }

        if (stateTimerImage == null)
        {
            Transform timer = transform.Find("StateTimer");
            if (timer != null)
                stateTimerImage = timer.GetComponent<Image>();
        }

        if (vfxAnchor == null)
            vfxAnchor = transform as RectTransform;

        // Keep VFX below timer
        if (vfxAnchor != null && stateTimerImage != null)
        {
            int timerIndex = stateTimerImage.transform.GetSiblingIndex();
            vfxAnchor.SetSiblingIndex(Mathf.Max(0, timerIndex - 1));
        }
    }

    public void HandleTool(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.Alert:
                TryApplyAlert();
                break;
            case ToolType.Release:
                TryApplyRelease();
                break;
            case ToolType.Ambulance:
                TryApplyAmbulance();
                break;
        }
    }

    private void TryApplyAlert()
    {
        if (currentState == SectorState.Incoming)
        {
            SetState(SectorState.AlertedIncoming);
            HideStateTimer();
            Debug.Log(sectorName + " alerted during incoming missile.");
        }
        else
        {
            Debug.Log("Alert cannot be applied to " + sectorName + " while state is " + currentState);
        }
    }

    private void TryApplyRelease()
    {
        if (currentState == SectorState.WaitingForRelease)
        {
            StopAllSectorVfx();
            StopStateTimer();
            StopRepeatingStateTimer();
            SetState(SectorState.Idle);
            Debug.Log(sectorName + " released back to idle.");
            return;
        }

        if (currentState == SectorState.Smoked)
        {
            GameManager.Instance?.LoseLife("Released " + sectorName + " before smoke cleared.");
            return;
        }

        if (currentState == SectorState.AmbulanceWorking)
        {
            GameManager.Instance?.LoseLife("Released " + sectorName + " before ambulance finished.");
            return;
        }

        Debug.Log("Release cannot be applied to " + sectorName + " while state is " + currentState);
    }

    private void TryApplyAmbulance()
    {
        if (currentState == SectorState.NeedsAmbulance || currentState == SectorState.NeedsAmbulanceCheck)
        {
            GameManager.Instance?.StartAmbulanceProcess(this);
        }
        else
        {
            Debug.Log("Ambulance cannot be applied to " + sectorName + " while state is " + currentState);
        }
    }

    public bool IsAlertedForCurrentMissile()
    {
        return currentState == SectorState.AlertedIncoming;
    }

    public void BeginIncoming()
    {
        SetState(SectorState.Incoming);
        StartFlicker();
    }

    public void ResolveIntercepted()
    {
        StopFlicker();

        if (currentState == SectorState.AlertedIncoming)
            SetState(SectorState.Smoked);
        else if (currentState == SectorState.Incoming)
            SetState(SectorState.NeedsAmbulanceCheck);
    }

    public void ResolveCrash()
    {
        StopFlicker();

        if (currentState == SectorState.AlertedIncoming)
            SetState(SectorState.NeedsAmbulance);
        else if (currentState == SectorState.Incoming)
            SetState(SectorState.Lost);
    }

    public void SetReadyForRelease()
    {
        SetState(SectorState.WaitingForRelease);
    }

    public void SetState(SectorState newState)
    {
        currentState = newState;
        UpdateVisual();
    }

    public void PlayInterceptSmokeSequence()
    {
        if (activeVfxRoutine != null)
            StopCoroutine(activeVfxRoutine);

        activeVfxRoutine = StartCoroutine(InterceptSmokeRoutine());
    }

    public void PlayCrashSequenceThenSmoke()
    {
        if (activeVfxRoutine != null)
            StopCoroutine(activeVfxRoutine);

        activeVfxRoutine = StartCoroutine(CrashSequenceRoutine());
    }

    private IEnumerator InterceptSmokeRoutine()
    {
        StopAllSectorVfx();
        SpawnSmoke();
        yield break;
    }

    private IEnumerator CrashSequenceRoutine()
    {
        StopAllSectorVfx();

        for (int i = 0; i < crashExplosionCount; i++)
        {
            SpawnExplosionAtRandomOffset();
            yield return new WaitForSeconds(timeBetweenCrashExplosions);
        }

        SpawnSmoke();
    }

    private void SpawnSmoke()
    {
        if (smokePrefab == null || vfxAnchor == null)
            return;

        StopSmokeOnly();

        for (int i = 0; i < smokeCloudCount; i++)
        {
            GameObject smoke = Instantiate(smokePrefab, vfxAnchor);
            Vector2 offset = Random.insideUnitCircle * smokeScatterRadius;
            SetupSpawnedUI(smoke, offset);
            activeSmokeInstances.Add(smoke);
        }
    }

    private void SpawnExplosionAtRandomOffset()
    {
        if (explosionPrefab == null || vfxAnchor == null)
            return;

        GameObject boom = Instantiate(explosionPrefab, vfxAnchor);
        Vector2 offset = Random.insideUnitCircle * crashExplosionRadius;
        SetupSpawnedUI(boom, offset);
    }

    private void SetupSpawnedUI(GameObject go, Vector2 anchoredPos)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // Smoke/explosions stay below timer
        if (stateTimerImage != null)
            rt.SetSiblingIndex(Mathf.Max(0, stateTimerImage.transform.GetSiblingIndex() - 1));
    }

    private void StopSmokeOnly()
    {
        for (int i = activeSmokeInstances.Count - 1; i >= 0; i--)
        {
            if (activeSmokeInstances[i] != null)
                Destroy(activeSmokeInstances[i]);
        }

        activeSmokeInstances.Clear();
    }

    public IEnumerator ClearSmokeWithAnimation()
    {
        for (int i = activeSmokeInstances.Count - 1; i >= 0; i--)
        {
            GameObject smoke = activeSmokeInstances[i];
            if (smoke == null)
                continue;

            Animator animator = smoke.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.SetBool("Clear", true);
        }

        yield return new WaitForSeconds(smokeClearAnimationDuration);
        StopSmokeOnly();
    }

    public void StopAllSectorVfx()
    {
        if (activeVfxRoutine != null)
        {
            StopCoroutine(activeVfxRoutine);
            activeVfxRoutine = null;
        }

        if (vfxAnchor == null)
            return;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        for (int i = 0; i < vfxAnchor.childCount; i++)
            childrenToDestroy.Add(vfxAnchor.GetChild(i).gameObject);

        foreach (GameObject child in childrenToDestroy)
            Destroy(child);

        activeSmokeInstances.Clear();
    }

    private void StartFlicker()
    {
        if (flickerRoutine != null)
            StopCoroutine(flickerRoutine);

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private void StopFlicker()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        if (baseImage != null)
        {
            Color c = baseImage.color;
            c.a = 1f;
            baseImage.color = c;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        bool dim = false;

        while (true)
        {
            if (baseImage != null)
            {
                Color c = baseImage.color;
                c.a = dim ? 0.35f : 1f;
                baseImage.color = c;
                dim = !dim;
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    private void UpdateVisual()
    {
        if (baseImage == null)
            return;

        baseImage.enabled = true;

        Color targetColor = idleColor;

        switch (currentState)
        {
            case SectorState.Idle: targetColor = idleColor; break;
            case SectorState.Incoming: targetColor = incomingColor; break;
            case SectorState.AlertedIncoming: targetColor = alertedIncomingColor; break;
            case SectorState.Smoked: targetColor = smokedColor; break;
            case SectorState.WaitingForRelease: targetColor = waitingForReleaseColor; break;
            case SectorState.NeedsAmbulanceCheck: targetColor = needsAmbulanceCheckColor; break;
            case SectorState.AmbulanceWorking: targetColor = ambulanceWorkingColor; break;
            case SectorState.NeedsAmbulance: targetColor = needsAmbulanceColor; break;
            case SectorState.Lost: targetColor = lostColor; break;
        }

        float currentAlpha = baseImage.color.a;
        targetColor.a = currentAlpha <= 0f ? 1f : currentAlpha;
        baseImage.color = targetColor;
    }

    public void ShowAlertHint()
    {
        if (stateTimerImage == null || alertIconSprite == null)
            return;

        stateTimerImage.gameObject.SetActive(true);
        stateTimerImage.sprite = alertIconSprite;
        stateTimerImage.type = Image.Type.Simple;

        Color c = stateTimerImage.color;
        c.a = fadedHintAlpha;
        stateTimerImage.color = c;

        stateTimerImage.transform.SetAsLastSibling();
    }

    private void PrepareTimerIcon(Sprite icon, float alpha)
    {
        if (stateTimerImage == null || icon == null)
            return;

        stateTimerImage.gameObject.SetActive(true);
        stateTimerImage.sprite = icon;
        stateTimerImage.type = Image.Type.Filled;
        stateTimerImage.fillMethod = Image.FillMethod.Radial360;
        stateTimerImage.fillOrigin = 2;
        stateTimerImage.fillClockwise = false;

        Color c = stateTimerImage.color;
        c.a = alpha;
        stateTimerImage.color = c;

        stateTimerImage.transform.SetAsLastSibling();
    }

    public void HideStateTimer()
    {
        if (stateTimerImage == null)
            return;

        stateTimerImage.gameObject.SetActive(false);
    }

    public void StartIconCountdown(float duration, Sprite icon, System.Action onComplete = null)
    {
        StopStateTimer();
        stateTimerRoutine = StartCoroutine(IconCountdownRoutine(duration, icon, onComplete));
    }

    public void StartIconFillUp(float duration, Sprite icon, System.Action onComplete = null)
    {
        StopStateTimer();
        stateTimerRoutine = StartCoroutine(IconFillUpRoutine(duration, icon, onComplete));
    }

    public void StartRepeatingIconCountdown(float duration, Sprite icon, System.Action onTick)
    {
        StopRepeatingStateTimer();
        repeatingStateTimerRoutine = StartCoroutine(RepeatingIconCountdownRoutine(duration, icon, onTick));
    }

    public void StartRepeatingIconFillUp(float duration, Sprite icon, System.Action onTick)
    {
        StopRepeatingStateTimer();
        repeatingStateTimerRoutine = StartCoroutine(RepeatingIconFillUpRoutine(duration, icon, onTick));
    }

    public void StopStateTimer()
    {
        if (stateTimerRoutine != null)
        {
            StopCoroutine(stateTimerRoutine);
            stateTimerRoutine = null;
        }

        HideStateTimer();
    }

    public void StopRepeatingStateTimer()
    {
        if (repeatingStateTimerRoutine != null)
        {
            StopCoroutine(repeatingStateTimerRoutine);
            repeatingStateTimerRoutine = null;
        }

        HideStateTimer();
    }

    private IEnumerator IconCountdownRoutine(float duration, Sprite icon, System.Action onComplete)
    {
        if (duration <= 0f)
        {
            HideStateTimer();
            onComplete?.Invoke();
            yield break;
        }

        PrepareTimerIcon(icon, activeTimerAlpha);
        stateTimerImage.fillAmount = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            stateTimerImage.fillAmount = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        HideStateTimer();
        stateTimerRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator IconFillUpRoutine(float duration, Sprite icon, System.Action onComplete)
    {
        if (duration <= 0f)
        {
            HideStateTimer();
            onComplete?.Invoke();
            yield break;
        }

        PrepareTimerIcon(icon, activeTimerAlpha);
        stateTimerImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            stateTimerImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        HideStateTimer();
        stateTimerRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator RepeatingIconCountdownRoutine(float duration, Sprite icon, System.Action onTick)
    {
        while (true)
        {
            PrepareTimerIcon(icon, activeTimerAlpha);
            stateTimerImage.fillAmount = 1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stateTimerImage.fillAmount = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            onTick?.Invoke();
        }
    }

    private IEnumerator RepeatingIconFillUpRoutine(float duration, Sprite icon, System.Action onTick)
    {
        while (true)
        {
            PrepareTimerIcon(icon, activeTimerAlpha);
            stateTimerImage.fillAmount = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                stateTimerImage.fillAmount = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            onTick?.Invoke();
        }
    }
}