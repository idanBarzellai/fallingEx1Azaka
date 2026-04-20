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

    //     [Header("Release Confetti")]
    // public ParticleSystem confettiPrefab;

    [Range(0f, 1f)] public float fadedHintAlpha = 0.35f;
    [Range(0f, 1f)] public float activeTimerAlpha = 0.95f;

    private Coroutine stateTimerRoutine;
    private Coroutine repeatingStateTimerRoutine;
    private Coroutine flickerRoutine;
    private Coroutine activeVfxRoutine;
    private Coroutine timerPulseRoutine;
    private Coroutine timerBreathingRoutine;
    private Coroutine timerClearRoutine;

    private Vector3 timerRestScale = Vector3.one;

    private const string TimerClearVObjectName = "ClearVAnimation";
    private const float clearVDrawDuration = 0.16f;
    private const float clearVStaggerDelay = 0.06f;
    private const float clearVHoldDuration = 0.28f;
    private static Sprite cachedWhiteSprite;

    private readonly List<GameObject> activeSmokeInstances = new List<GameObject>();

    private Coroutine invalidActionFlashRoutine;

[Header("Invalid Action Feedback")]
public Color invalidActionFlashColor = new Color(1f, 0.15f, 0.15f, 1f);
public float invalidActionFlashDuration = 0.45f;
public float invalidActionFlashInterval = 0.08f;

[Header("Emoji Burst VFX")]
public GameObject emojiBurstItemPrefab;
public Sprite angryFaceSprite;
public Sprite happyFaceSprite;

public int angryBurstCount = 10;
public int happyBurstCount = 12;
public float emojiBurstRadius = 95f;
public Vector2 emojiSizeRange = new Vector2(20f, 38f);
public float emojiBurstLifetime = 1.0f;

    private void Awake()
    {
        AutoAssignReferences();
        if (stateTimerImage != null)
            timerRestScale = stateTimerImage.rectTransform.localScale;

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
            AudioManager.Instance?.PlayValidAction();
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
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayValidAction();
            AudioManager.Instance.PlayRelease();
            }
            StopAllSectorVfx();
            StopStateTimer();
            StopRepeatingStateTimer();
            SetState(SectorState.Idle);
            Debug.Log(sectorName + " released back to idle.");
            GameManager.Instance?.CrisisAvoided(sectorName.ToString());
            PlayHappyBurst();

        // StartCoroutine(PlayConfettiBurst());

            return;
        }

        if (currentState == SectorState.Smoked)
        {
            AudioManager.Instance.PlayInvalidAction();
                PlayAngryBurst();

            PlayInvalidActionFlash();
            GameManager.Instance?.LoseLife("Released " + sectorName + " before smoke cleared.", sectorName.ToString());
            return;
        }

        if (currentState == SectorState.AmbulanceWorking)
        {
            AudioManager.Instance.PlayInvalidAction();
                PlayAngryBurst();

PlayInvalidActionFlash();
            GameManager.Instance?.LoseLife("Released " + sectorName + " before ambulance finished.", sectorName.ToString());
            return;
        }

        Debug.Log("Release cannot be applied to " + sectorName + " while state is " + currentState);
    }

    // private IEnumerator PlayConfettiBurst()
    // {
        
    //     if (confettiPrefab == null)
    //         yield break;

    //         confettiPrefab.Play();
    // }

    private void TryApplyAmbulance()
    {
        if (currentState == SectorState.NeedsAmbulance || currentState == SectorState.NeedsAmbulanceCheck)
        {
            AudioManager.Instance?.PlayValidAction();
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
        PlayReleasePromptAnimation();
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

    public void PlayInterceptSmokeSequenceAt(Vector2 missileAnchoredPosition, RectTransform sourceLayer)
{
    if (activeVfxRoutine != null)
        StopCoroutine(activeVfxRoutine);

    activeVfxRoutine = StartCoroutine(InterceptSmokeAtPointRoutine(missileAnchoredPosition, sourceLayer));
}

private IEnumerator InterceptSmokeAtPointRoutine(Vector2 missileAnchoredPosition, RectTransform sourceLayer)
{
    StopAllSectorVfx();
    SpawnSmokeAtPoint(missileAnchoredPosition, sourceLayer);
    yield break;
}

private void SpawnSmokeAtPoint(Vector2 worldAnchoredPosition, RectTransform sourceLayer)
{
    if (smokePrefab == null || vfxAnchor == null || sourceLayer == null)
        return;

    StopSmokeOnly();

    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, sourceLayer.TransformPoint(worldAnchoredPosition));

    RectTransform parentRect = vfxAnchor;
    Vector2 localPoint;

    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, null, out localPoint))
        localPoint = Vector2.zero;

    int puffCount = 3;
    float smallRadius = 80f;

    for (int i = 0; i < puffCount; i++)
    {
        GameObject smoke = Instantiate(smokePrefab, vfxAnchor);
        Vector2 offset = Random.insideUnitCircle * smallRadius;
        SetupSpawnedUI(smoke, localPoint + offset);
        activeSmokeInstances.Add(smoke);
    }
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

   private void SetupSpawnedUI(GameObject vfxObject, Vector2 localPosition)
{
    RectTransform rt = vfxObject.GetComponent<RectTransform>();
    if (rt != null)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPosition;
        rt.localScale = Vector3.one;
    }

    vfxObject.transform.SetAsLastSibling();
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

        // float currentAlpha = baseImage.color.a;
        // targetColor.a = currentAlpha <= 0f ? 0.5f : currentAlpha;
        baseImage.color = targetColor;
    }

    public void ShowAlertHint()
    {
        if (stateTimerImage == null || alertIconSprite == null)
            return;

        stateTimerImage.gameObject.SetActive(true);
        stateTimerImage.sprite = alertIconSprite;
        stateTimerImage.type = Image.Type.Simple;

        // Color c = stateTimerImage.color;
        // c.a = fadedHintAlpha;
        // stateTimerImage.color = c;

        stateTimerImage.transform.SetAsLastSibling();
        StartTimerBreathing();
    }

    private void PrepareTimerIcon(Sprite icon, float alpha, Image.Type imageType = Image.Type.Filled)
    {
        if (stateTimerImage == null || icon == null)
            return;

        stateTimerImage.gameObject.SetActive(true);
        stateTimerImage.sprite = icon;
        stateTimerImage.type = imageType;

        if (imageType == Image.Type.Filled)
        {
            stateTimerImage.fillMethod = Image.FillMethod.Radial360;
            stateTimerImage.fillOrigin = 2;
            stateTimerImage.fillClockwise = false;
        }

        Color c = stateTimerImage.color;
        c.a = alpha;
        stateTimerImage.color = c;

        stateTimerImage.transform.SetAsLastSibling();
    }

    private void PlayReleasePromptAnimation()
    {
        if (stateTimerImage == null || releaseIconSprite == null)
            return;

        if (timerPulseRoutine != null)
            StopCoroutine(timerPulseRoutine);

        timerPulseRoutine = StartCoroutine(ReleasePromptRoutine());
    }

    private void StartTimerBreathing()
    {
        if (stateTimerImage == null)
            return;

        if (timerBreathingRoutine != null)
            return;

        timerBreathingRoutine = StartCoroutine(TimerBreathingRoutine());
    }

    private IEnumerator ReleasePromptRoutine()
    {
        PrepareTimerIcon(releaseIconSprite, activeTimerAlpha, Image.Type.Simple);

        if (stateTimerImage == null)
        {
            timerPulseRoutine = null;
            yield break;
        }

        RectTransform timerRect = stateTimerImage.rectTransform;
        Vector3 baseScale = timerRestScale;
        Vector3 startScale = baseScale * 0.72f;
        Vector3 peakScale = baseScale * 1.2f;

        yield return ScaleTimerRect(timerRect, startScale, peakScale, 0.16f);
        yield return ScaleTimerRect(timerRect, peakScale, baseScale, 0.14f);

        timerPulseRoutine = null;
        StartTimerBreathing();
    }

    private IEnumerator TimerBreathingRoutine()
    {
        if (stateTimerImage == null)
        {
            timerBreathingRoutine = null;
            yield break;
        }

        RectTransform timerRect = stateTimerImage.rectTransform;
        Vector3 baseScale = timerRestScale;
        const float minScaleFactor = 0.96f;
        const float maxScaleFactor = 1.04f;
        const float cycleDuration = 1.2f;

        while (true)
        {
            float cycle = Mathf.PingPong(Time.time / cycleDuration, 1f);
            float scaleFactor = Mathf.Lerp(minScaleFactor, maxScaleFactor, cycle);
            timerRect.localScale = baseScale * scaleFactor;
            yield return null;
        }
    }

    private IEnumerator ScaleTimerRect(RectTransform timerRect, Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (timerRect == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            timerRect.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        timerRect.localScale = toScale;
    }

    private void StopTimerAnimations()
    {
        if (timerPulseRoutine != null)
        {
            StopCoroutine(timerPulseRoutine);
            timerPulseRoutine = null;
        }

        if (timerBreathingRoutine != null)
        {
            StopCoroutine(timerBreathingRoutine);
            timerBreathingRoutine = null;
        }

        if (stateTimerImage != null)
            stateTimerImage.rectTransform.localScale = timerRestScale;
    }

    public void HideStateTimer()
    {
        if (stateTimerImage == null)
            return;

        StopClearAnimation();

        StopTimerAnimations();
        stateTimerImage.gameObject.SetActive(false);
    }

    private void StopClearAnimation()
    {
        if (timerClearRoutine != null)
        {
            StopCoroutine(timerClearRoutine);
            timerClearRoutine = null;
        }

        DestroyClearVObject();
    }

    private IEnumerator PlayClearVAnimationThenHide()
    {
        if (stateTimerImage == null)
            yield break;

        StopTimerAnimations();
        DestroyClearVObject();

        stateTimerImage.gameObject.SetActive(true);
        stateTimerImage.type = Image.Type.Simple;

        Color timerColor = stateTimerImage.color;
        timerColor.a = 0f;
        stateTimerImage.color = timerColor;

        RectTransform hostRect = stateTimerImage.rectTransform;
        float minSize = Mathf.Min(hostRect.rect.width, hostRect.rect.height);
        float armLength = Mathf.Max(16f, minSize * 0.46f);
        float armThickness = Mathf.Max(3f, minSize * 0.08f);

        GameObject clearVRoot = new GameObject(TimerClearVObjectName, typeof(RectTransform));
        clearVRoot.transform.SetParent(stateTimerImage.transform, false);

        RectTransform rootRect = clearVRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = Vector2.zero;

        RectTransform leftArm = CreateClearVArm(clearVRoot.transform, armLength, armThickness, 34f);
        RectTransform rightArm = CreateClearVArm(clearVRoot.transform, armLength, armThickness, -34f);

        yield return ScaleRectY(leftArm, 0f, 1f, clearVDrawDuration);
        yield return ScaleRectY(rightArm, 0f, 1f, clearVDrawDuration - clearVStaggerDelay);
        yield return new WaitForSeconds(clearVHoldDuration);

        DestroyClearVObject();
        stateTimerImage.gameObject.SetActive(false);
    }

    private RectTransform CreateClearVArm(Transform parent, float armLength, float armThickness, float rotationZ)
    {
        GameObject arm = new GameObject("Arm", typeof(RectTransform), typeof(Image));
        arm.transform.SetParent(parent, false);

        RectTransform armRect = arm.GetComponent<RectTransform>();
        armRect.anchorMin = new Vector2(0.5f, 0.5f);
        armRect.anchorMax = new Vector2(0.5f, 0.5f);
        armRect.pivot = new Vector2(0.5f, 1f);
        armRect.anchoredPosition = new Vector2(0f, armLength * 0.35f);
        armRect.sizeDelta = new Vector2(armThickness, armLength);
        armRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        armRect.localScale = new Vector3(1f, 0f, 1f);

        Image armImage = arm.GetComponent<Image>();
        armImage.sprite = GetWhiteSprite();
        armImage.type = Image.Type.Simple;
        armImage.raycastTarget = false;
        armImage.color = new Color(0.45f, 1f, 0.45f, activeTimerAlpha);

        return armRect;
    }

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null)
            return cachedWhiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedWhiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return cachedWhiteSprite;
    }

    private IEnumerator ScaleRectY(RectTransform rect, float from, float to, float duration)
    {
        if (rect == null)
            yield break;

        float clampedDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < clampedDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / clampedDuration);
            float y = Mathf.Lerp(from, to, t);
            rect.localScale = new Vector3(1f, y, 1f);
            yield return null;
        }

        rect.localScale = new Vector3(1f, to, 1f);
    }

    private void DestroyClearVObject()
    {
        if (stateTimerImage == null)
            return;

        Transform clearV = stateTimerImage.transform.Find(TimerClearVObjectName);
        if (clearV != null)
            Destroy(clearV.gameObject);
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
            timerClearRoutine = StartCoroutine(PlayClearVAnimationThenHide());
            yield return timerClearRoutine;
            timerClearRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        PrepareTimerIcon(icon, activeTimerAlpha);
        StartTimerBreathing();
        stateTimerImage.fillAmount = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            stateTimerImage.fillAmount = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        timerClearRoutine = StartCoroutine(PlayClearVAnimationThenHide());
        yield return timerClearRoutine;
        timerClearRoutine = null;
        stateTimerRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator IconFillUpRoutine(float duration, Sprite icon, System.Action onComplete)
    {
        if (duration <= 0f)
        {
            timerClearRoutine = StartCoroutine(PlayClearVAnimationThenHide());
            yield return timerClearRoutine;
            timerClearRoutine = null;
            onComplete?.Invoke();
            yield break;
        }

        PrepareTimerIcon(icon, activeTimerAlpha);
        StartTimerBreathing();
        stateTimerImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            stateTimerImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        timerClearRoutine = StartCoroutine(PlayClearVAnimationThenHide());
        yield return timerClearRoutine;
        timerClearRoutine = null;
        stateTimerRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator RepeatingIconCountdownRoutine(float duration, Sprite icon, System.Action onTick)
    {
        while (true)
        {
            PrepareTimerIcon(icon, activeTimerAlpha);
            StartTimerBreathing();
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
            StartTimerBreathing();
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

    public void PlayInvalidActionFlash()
{
    if (baseImage == null)
        return;

    if (invalidActionFlashRoutine != null)
        StopCoroutine(invalidActionFlashRoutine);

    invalidActionFlashRoutine = StartCoroutine(InvalidActionFlashRoutine());
}

private IEnumerator InvalidActionFlashRoutine()
{
    float elapsed = 0f;
    Color originalColor = baseImage.color;

    while (elapsed < invalidActionFlashDuration)
    {
        float wave = (Mathf.Sin(elapsed * 40f) + 1f) * 0.5f;
        Color flashColor = Color.Lerp(originalColor, invalidActionFlashColor, wave);
        flashColor.a = originalColor.a;
        baseImage.color = flashColor;

        elapsed += Time.deltaTime;
        yield return null;
    }

    UpdateVisual();
    invalidActionFlashRoutine = null;
}

public void PlayEmojiBurst(Sprite emojiSprite, int count)
{
    if (emojiBurstItemPrefab == null || emojiSprite == null || vfxAnchor == null)
        return;

    for (int i = 0; i < count; i++)
    {
        GameObject emoji = Instantiate(emojiBurstItemPrefab, vfxAnchor);
        RectTransform rt = emoji.GetComponent<RectTransform>();
        Image img = emoji.GetComponent<Image>();
        CanvasGroup cg = emoji.GetComponent<CanvasGroup>();
        AutoDestroyAfterSeconds autoDestroy = emoji.GetComponent<AutoDestroyAfterSeconds>();

        if (img != null)
            img.sprite = emojiSprite;

        if (autoDestroy != null)
            autoDestroy.lifetime = emojiBurstLifetime;

        Vector2 offset = Random.insideUnitCircle * emojiBurstRadius;
        float size = Random.Range(emojiSizeRange.x, emojiSizeRange.y);

        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(size, size);
            float flip = Random.value > 0.5f ? 1f : -1f;

rt.localScale = new Vector3(flip, 1f, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
        }

        emoji.transform.SetAsLastSibling();

        StartCoroutine(AnimateEmojiBurstItem(rt, cg));
    }
}

private IEnumerator AnimateEmojiBurstItem(RectTransform rt, CanvasGroup cg)
{
    if (rt == null)
        yield break;

    Vector2 startPos = rt.anchoredPosition;
    Vector2 drift = Random.insideUnitCircle * 18f;

    float duration = emojiBurstLifetime;
    float elapsed = 0f;

    float flip = rt.localScale.x >= 0 ? 1f : -1f;

Vector3 startScale = new Vector3(flip, 1f, 1f) * Random.Range(0.85f, 1.0f);
Vector3 peakScale = startScale * Random.Range(1.08f, 1.2f);

    if (cg != null)
        cg.alpha = 0f;

    while (elapsed < duration)
    {
         if (rt == null || cg == null || cg.gameObject == null)
        yield break;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        if (cg != null)
        {
            if (t < 0.15f)
                cg.alpha = Mathf.Lerp(0f, 1f, t / 0.15f);
            else if (t > 0.75f)
                cg.alpha = Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f);
            else
                cg.alpha = 1f;
        }

        rt.anchoredPosition = Vector2.Lerp(startPos, startPos + drift, t);

        if (t < 0.2f)
            rt.localScale = Vector3.Lerp(startScale, peakScale, t / 0.2f);
        else
            rt.localScale = Vector3.Lerp(peakScale, startScale, (t - 0.2f) / 0.8f);

        yield return null;
    }
}

public void PlayAngryBurst()
{
    PlayEmojiBurst(angryFaceSprite, angryBurstCount);
}

public void PlayHappyBurst()
{
    PlayEmojiBurst(happyFaceSprite, happyBurstCount);
}

    public void ResetSectorCompletely()
{
    StopAllCoroutines();

    flickerRoutine = null;
    activeVfxRoutine = null;
    stateTimerRoutine = null;
    repeatingStateTimerRoutine = null;
    timerPulseRoutine = null;
    timerBreathingRoutine = null;
    timerClearRoutine = null;

    StopAllSectorVfx();
    HideStateTimer();

    if (baseImage != null)
    {
        Color c = idleColor;
        c.a = 1f;
        baseImage.color = c;
    }

    currentState = SectorState.Idle;
    UpdateVisual();
}
}