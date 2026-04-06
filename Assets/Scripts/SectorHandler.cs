using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SectorState
{
    Idle,
    Incoming,                // target sector while missile is approaching
    AlertedIncoming,         // incoming + player alerted it
    Smoked,                  // alerted + intercepted
    WaitingForRelease,       // smoke cleared or ambulance finished, ready for release
    NeedsAmbulanceCheck,     // not alerted + intercepted
    AmbulanceWorking,
    NeedsAmbulance,          // alerted + crashed
    Damaged,                 // optional visual/result state if you want to keep it
    Lost                     // not alerted + crashed
}

public class SectorHandler : MonoBehaviour
{
    [Header("Sector Info")]
    public SectorName sectorName;
    public SectorState currentState = SectorState.Idle;

    [Header("Visual References")]
    [Tooltip("The Image that changes color/flickers. Can be on this object or a child.")]
    public Image baseImage;

    [Tooltip("Parent under which smoke / explosion UI prefabs will spawn.")]
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

private Coroutine stateTimerRoutine;

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

        if (vfxAnchor == null)
        {
            vfxAnchor = transform as RectTransform;
        }

        if (vfxAnchor != null)
            vfxAnchor.SetAsLastSibling();

            if (stateTimerImage == null)
{
    Transform timer = transform.Find("StateTimer");
    if (timer != null)
        stateTimerImage = timer.GetComponent<Image>();
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
        SetState(SectorState.Idle);
        Debug.Log(sectorName + " released back to idle.");
    }
    else
    {
        Debug.Log("Release cannot be applied to " + sectorName + " while state is " + currentState);
    }
}
    private void TryApplyAmbulance()
    {
        if (currentState == SectorState.NeedsAmbulance || currentState == SectorState.NeedsAmbulanceCheck)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartAmbulanceProcess(this);
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
        {
            SetState(SectorState.Smoked);
        }
        else if (currentState == SectorState.Incoming)
        {
            SetState(SectorState.NeedsAmbulanceCheck);
        }
    }

    public void ResolveCrash()
    {
        StopFlicker();

        if (currentState == SectorState.AlertedIncoming)
        {
            SetState(SectorState.NeedsAmbulance);
        }
        else if (currentState == SectorState.Incoming)
        {
            SetState(SectorState.Lost);
        }
    }

   public void SetReadyForRelease()
{
    SetState(SectorState.WaitingForRelease);
}

    public void SetState(SectorState newState)
{
    if (currentState != newState)
    {
        StopStateTimer();
    }

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
        rt.SetAsLastSibling();
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
        {
            animator.SetBool("Clear", true);
        }
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
        {
            childrenToDestroy.Add(vfxAnchor.GetChild(i).gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
        {
            Destroy(child);
        }
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
            case SectorState.Idle:
                targetColor = idleColor;
                break;
            case SectorState.Incoming:
                targetColor = incomingColor;
                break;
            case SectorState.AlertedIncoming:
                targetColor = alertedIncomingColor;
                break;
            case SectorState.Smoked:
                targetColor = smokedColor;
                break;
            case SectorState.WaitingForRelease:
                targetColor = waitingForReleaseColor;
                break;
            case SectorState.NeedsAmbulanceCheck:
                targetColor = needsAmbulanceCheckColor;
                break;
                case SectorState.AmbulanceWorking:
    targetColor = ambulanceWorkingColor;
    break;
            case SectorState.NeedsAmbulance:
                targetColor = needsAmbulanceColor;
                break;
            case SectorState.Lost:
                targetColor = lostColor;
                break;
        }

        float currentAlpha = baseImage.color.a;
        targetColor.a = currentAlpha <= 0f ? 1f : currentAlpha;
        baseImage.color = targetColor;
    }

    public void ShowStateTimer(float normalizedValue = 1f)
{
    if (stateTimerImage == null) return;

    stateTimerImage.gameObject.SetActive(true);
    stateTimerImage.fillAmount = Mathf.Clamp01(normalizedValue);
}

public void HideStateTimer()
{
    if (stateTimerImage == null) return;

    stateTimerImage.gameObject.SetActive(false);
}

public void StartStateTimer(float duration, System.Action onComplete = null)
{
    if (stateTimerRoutine != null)
        StopCoroutine(stateTimerRoutine);

    stateTimerRoutine = StartCoroutine(StateTimerRoutine(duration, onComplete));
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

private IEnumerator StateTimerRoutine(float duration, System.Action onComplete)
{
    if (duration <= 0f)
    {
        HideStateTimer();
        onComplete?.Invoke();
        yield break;
    }

    ShowStateTimer(1f);

    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;

        if (stateTimerImage != null)
        {
            stateTimerImage.fillAmount = 1f - Mathf.Clamp01(elapsed / duration);
        }

        yield return null;
    }

    HideStateTimer();
    stateTimerRoutine = null;
    onComplete?.Invoke();
}
}