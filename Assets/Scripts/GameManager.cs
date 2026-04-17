using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sectors")]
    public SectorHandler northSector;
    public SectorHandler sharonSector;
    public SectorHandler centerSector;
    public SectorHandler eilatSector;
    public SectorHandler southSector;

    [Header("Prefabs / Parents")]
    public MissileUI missilePrefab;
    public MissileDirectionIndicator directionIndicatorPrefab;
    public RectTransform missileLayer;
    public RectTransform indicatorLayer;

    [Header("Camera Frame In Canvas Space")]
    public Rect visibleCameraRect = new Rect(-540f, -960f, 1080f, 1920f);

    [Header("UI")]
    public TMP_Text crisisAvoidedCounterText;
    public TMP_Text highScoreText;
    private int crisisAvoidedCount = 0;
    private int crisisAvoidedHighScoreCount = 0;
    public TMP_Text livesText;
    public TMP_Text loseReasonText;
    public GameObject gameOverPanel;
    public GameObject tvUI;
    public Image tvVideoStaticImage;
    public VideoPlayer tvVideoPlayer;
    public GameObject toolBar;


    [Header("Lives")]
    public int startingLives = 5;
    private int currentLives;
    public Image[] livesImages; // Assign in inspector: 0 - life1, 1 - life2, etc.
    public Image livesUIFrame;
    public Image livesUIBG;


    [Header("Penalty UI")]
    public float loseReasonMessageDuration = 2.5f;
    public float gameOverUiTransitionDuration = 0.6f;

    [Header("Missile Settings")]
    public float preLaunchWarningTime = 1.2f;
    public float missileTravelDuration = 5.5f;
    public float smokeClearTime = 5f;
    public float ambulanceProcessTime = 10f;

    [Header("Penalty Timers")]
    public float ambulanceTooLateInterval = 10f;
    public float releaseNeglectInterval = 10f;




    [SerializeField] private float minLaunchDelay = 3.5f;
    [SerializeField] private float maxLaunchDelay = 6.5f;

    private bool gameOver = false;
    private Coroutine loseReasonRoutine;
    private Coroutine gameLoopRoutine;
    private Coroutine uiScaleCenterRoutine;
    private readonly Dictionary<RectTransform, UITransformSnapshot> uiOriginalTransforms = new Dictionary<RectTransform, UITransformSnapshot>();

    private struct UITransformSnapshot
    {
        public Vector2 anchoredPosition;
        public Vector3 localScale;
    }

    private readonly List<MissileEventData> activeMissiles = new List<MissileEventData>();

    private class MissileEventData
    {
        public SectorHandler targetSector;
        public MissileUI missileUI;
        public MissileDirectionIndicator indicatorUI;
        public bool resolved;
        public SectorName spawnSide;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        currentLives = startingLives;
        FadeOutLives();
        crisisAvoidedHighScoreCount = PlayerPrefs.GetInt("HighScore", 0);
        RefreshUItext();
        tvVideoPlayer.loopPointReached +=  VideoStopped;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayBgm();

        if (loseReasonText != null)
            loseReasonText.text = "Until today " + crisisAvoidedHighScoreCount + " crises avoided.";

            if(tvUI != null){
                    ScaleAndCenterUI(tvUI);
PlayVideo();
                }

        gameLoopRoutine = StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(3f);

         if (tvUI != null)
            RestoreUIOriginalTransform(tvUI);
        yield return new WaitForSeconds(1f);
        FadeInLives();


        while (!gameOver)
        {
            StartCoroutine(StartMissileEventRoutine());
            float delay = Random.Range(minLaunchDelay, maxLaunchDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator StartMissileEventRoutine()
    {
        SectorHandler targetSector = GetRandomSector();
        if (targetSector == null)
            yield break;

        targetSector.BeginIncoming();
        targetSector.ShowAlertHint();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMissileWarning();

        MissileDirectionIndicator indicator = null;
        if (directionIndicatorPrefab != null && indicatorLayer != null)
            indicator = Instantiate(directionIndicatorPrefab, indicatorLayer);

        MissileEventData missileData = new MissileEventData
        {
            targetSector = targetSector,
            indicatorUI = indicator,
            missileUI = null,
            resolved = false,
            spawnSide = targetSector.sectorName
        };

        activeMissiles.Add(missileData);

        yield return new WaitForSeconds(preLaunchWarningTime);

        if (gameOver || missileData.resolved)
        {
            CleanupMissileEvent(missileData);
            yield break;
        }

        MissileUI missile = Instantiate(missilePrefab, missileLayer);
        missileData.missileUI = missile;

        Vector2 startPos = GetMissileStartPosition(missileData.spawnSide);
        Vector2 targetPos = GetSectorAnchoredPosition(missileData.targetSector);

        missile.Launch(
            startPos,
            targetPos,
            missileTravelDuration,
            () => OnMissileImpact(missileData),
            () => OnMissileTapped(missileData)
        );


        if (missileData.indicatorUI != null)
            missileData.indicatorUI.BeginTracking(missile.RectTransform, visibleCameraRect);
    }

    private bool HasActiveIncomingMissiles()
{
    foreach (var m in activeMissiles)
    {
        if (m != null && !m.resolved)
            return true;
    }
    return false;
}

private void OnMissileTapped(MissileEventData missileData)
{
    if (missileData == null || missileData.resolved || missileData.targetSector == null || gameOver)
        return;

  

    missileData.resolved = true;

    Vector2 missileHitPosition = Vector2.zero;
    if (missileData.missileUI != null)
        missileHitPosition = missileData.missileUI.RectTransform.anchoredPosition;

    if (missileData.missileUI != null)
        missileData.missileUI.HideMissile();

    if (missileData.indicatorUI != null)
        missileData.indicatorUI.StopTracking();

    SectorHandler sector = missileData.targetSector;
    SectorState previousState = sector.currentState;

    sector.ResolveIntercepted();

      if (AudioManager.Instance != null){
        AudioManager.Instance.PlayMissileTap();

        if(!HasActiveIncomingMissiles())
    AudioManager.Instance.StopAlert();
    }

    // Alerted + intercepted = smoke -> release
    if (sector.currentState == SectorState.Smoked)
    {
        sector.PlayInterceptSmokeSequenceAt(missileHitPosition, missileLayer);
        // sector.StartIconFillUp(smokeClearTime, sector.releaseIconSprite);
        StartCoroutine(SmokeClearRoutine(sector));

        CleanupMissileEvent(missileData);
        return;
    }

    // Not alerted + intercepted = lose 1 life + smoke + ambulance needed
    if (sector.currentState == SectorState.NeedsAmbulanceCheck && previousState == SectorState.Incoming)
    {
        LoseLife("Intercepted missile in " + sector.sectorName + " without alert.", sector.sectorName.ToString());

        sector.PlayInterceptSmokeSequenceAt(missileHitPosition, missileLayer);

        StartAmbulancePenaltyLoop(sector);

        CleanupMissileEvent(missileData);
        return;
    }

    CleanupMissileEvent(missileData);
}

    private void OnMissileImpact(MissileEventData missileData)
    {
        if (missileData == null || missileData.resolved || missileData.targetSector == null || gameOver)
            return;

       

        missileData.resolved = true;

        if (missileData.indicatorUI != null)
            missileData.indicatorUI.StopTracking();

        SectorHandler sector = missileData.targetSector;
        sector.ResolveCrash();

         if (AudioManager.Instance != null){
            AudioManager.Instance.PlayMissileImpact();

        if(!HasActiveIncomingMissiles())
    AudioManager.Instance.StopAlert();
        }

        if (sector.currentState == SectorState.NeedsAmbulance || sector.currentState == SectorState.Lost)
        {
            sector.PlayCrashSequenceThenSmoke();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayExplosion();
        }

        if (sector.currentState == SectorState.Lost)
        {
            TriggerGameOver("Missile hit " + sector.sectorName + " without alert.");
            CleanupMissileEvent(missileData);
            return;
        }

        if (sector.currentState == SectorState.NeedsAmbulance){
    LoseLife("Missile hit alerted sector " + sector.sectorName, sector.sectorName.ToString());
            StartAmbulancePenaltyLoop(sector);

        }

        CleanupMissileEvent(missileData);
    }

    private void CleanupMissileEvent(MissileEventData missileData)
    {
        if (missileData == null)
            return;

        if (missileData.missileUI != null)
            Destroy(missileData.missileUI.gameObject);

        if (missileData.indicatorUI != null)
            Destroy(missileData.indicatorUI.gameObject);

        activeMissiles.Remove(missileData);
    }

    public void StartAmbulanceProcess(SectorHandler sector)
    {
        if (sector == null || gameOver)
            return;

        SectorState requestedState = sector.currentState;

        if (requestedState != SectorState.NeedsAmbulance &&
            requestedState != SectorState.NeedsAmbulanceCheck)
            return;

        sector.StopRepeatingStateTimer();
        sector.SetState(SectorState.AmbulanceWorking);

        if (AudioManager.Instance != null){
            AudioManager.Instance.PlayAmbulance();
            AudioManager.Instance.PlayValidAction();

        }

        // Per your rule: AmbulanceWorking uses RELEASE icon filling 0 -> 1
        // sector.StartIconFillUp(ambulanceProcessTime, sector.releaseIconSprite);

        StartCoroutine(AmbulanceRoutine(sector));
    }

private IEnumerator SmokeClearRoutine(SectorHandler sector)
{
    if (sector == null)
        yield break;

    // sector.StartIconFillUp(smokeClearTime, sector.releaseIconSprite);

    yield return new WaitForSeconds(smokeClearTime);

    if (sector == null || sector.currentState != SectorState.Smoked)
        yield break;

    sector.StopStateTimer();

    // switch to ready immediately
    sector.SetReadyForRelease();
    StartReleasePenaltyLoop(sector);

    // let the smoke visual clear afterward
    yield return StartCoroutine(sector.ClearSmokeWithAnimation());
}

private IEnumerator AmbulanceRoutine(SectorHandler sector)
{
    yield return new WaitForSeconds(ambulanceProcessTime);

    if (sector == null)
        yield break;

    if (sector.currentState != SectorState.AmbulanceWorking)
        yield break;

    sector.SetReadyForRelease();
    StartReleasePenaltyLoop(sector);

    yield return StartCoroutine(sector.ClearSmokeWithAnimation());
}

    private void StartAmbulancePenaltyLoop(SectorHandler sector)
    {
        if (sector == null || gameOver)
            return;

        sector.StartRepeatingIconCountdown(ambulanceTooLateInterval, sector.ambulanceIconSprite, () =>
        {
            if (sector.currentState == SectorState.NeedsAmbulance ||
                sector.currentState == SectorState.NeedsAmbulanceCheck)
            {
                LoseLife("Ambulance was too late in " + sector.sectorName + ".", sector.sectorName.ToString()   );
            }
        });
    }

    private void StartReleasePenaltyLoop(SectorHandler sector)
    {
        if (sector == null || gameOver)
            return;

        sector.StartRepeatingIconCountdown(releaseNeglectInterval, sector.releaseIconSprite, () =>
        {
            if (sector.currentState == SectorState.WaitingForRelease)
            {
                LoseLife("Citizens were not released in time in " + sector.sectorName + ".", sector.sectorName.ToString());
            }
        });
    }

    public void LoseLife(string reason, string sectorName)
    {
        if (gameOver)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLoseLife();

        currentLives--;
        livesImages[currentLives].GetComponent<UIFade>().FadeOut();
        RefreshUItext();
        ShowLoseReason(reason, sectorName);

        Debug.Log("Lost life: " + reason + " | Lives left: " + currentLives);

        if (currentLives <= 0)
            TriggerGameOver(reason);
    }
     public void CrisisAvoided(string sectorName)
    {
        if (gameOver)
            return;

        crisisAvoidedCount++;
        if (crisisAvoidedCount > crisisAvoidedHighScoreCount){
            crisisAvoidedHighScoreCount = crisisAvoidedCount;
            PlayerPrefs.SetInt("HighScore", crisisAvoidedHighScoreCount);
        }
        RefreshUItext();
         ShowLoseReason("Crisis avoided in " + sectorName + "!", sectorName);


        Debug.Log("Crisis avoided count updated");
    }

    




    private void RefreshUItext()
    {
        if (livesText != null)
            livesText.text = "Lives: " + currentLives;

        if (crisisAvoidedCounterText != null)
            crisisAvoidedCounterText.text = "Crisis Avoided: " + crisisAvoidedCount;

        if (highScoreText != null)            {
            if(crisisAvoidedHighScoreCount == 0)
                crisisAvoidedHighScoreCount = PlayerPrefs.GetInt("HighScore", 0);
            highScoreText.text = "High Score: " + crisisAvoidedHighScoreCount;  }  
    }

    private void ShowLoseReason(string reason, string sectorName)
    {
        if (loseReasonText == null)
            return;

        if (loseReasonRoutine != null)
            StopCoroutine(loseReasonRoutine);

        loseReasonRoutine = StartCoroutine(ShowLoseReasonRoutine(reason, sectorName));
    }

    private IEnumerator ShowLoseReasonRoutine(string reason, string sectorName)
    {
        PlayVideo();
        loseReasonText.text = reason;

        yield return new WaitForSeconds(loseReasonMessageDuration);

        if (!gameOver && loseReasonText != null){
            
            string status = currentLives == 5 ? "No crashes so far" : currentLives == 4 ? "An event occured in the " + sectorName : "" + (startingLives - currentLives) + " events occured all around the country";

            loseReasonText.text = "Today " + crisisAvoidedCount + " crises avoided.\n" + status;
        }

        loseReasonRoutine = null;
    }

    private void TriggerGameOver(string reason)
    {
        gameOver = true;

        if (gameLoopRoutine != null)
        {
            StopCoroutine(gameLoopRoutine);
            gameLoopRoutine = null;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }

        foreach (var missile in activeMissiles)
        {
            if (missile.missileUI != null)
                missile.missileUI.HideMissile();

            if (missile.indicatorUI != null)
                missile.indicatorUI.StopTracking();
        }

        SectorHandler[] sectors = new SectorHandler[]
        {
            northSector, southSector, centerSector, sharonSector, eilatSector
        };

        foreach (var sector in sectors)
        {
            if (sector == null) continue;
            sector.StopStateTimer();
            sector.StopRepeatingStateTimer();
            sector.SetState(SectorState.Lost);
        }

        if (livesText != null)
            livesText.text = "GAME OVER";

        if (loseReasonText != null)
            loseReasonText.text = "No one wins at war.\n" + reason + "\nToday " + crisisAvoidedCount + " crises avoided.";

            if (gameOverPanel != null)
            {
                gameOverPanel.GetComponent<UIFade>().FadeIn();
            }

            FadeOutLives();
            if(toolBar != null)
                toolBar.GetComponent<UIFade>().FadeOut();

                if(tvUI != null){
                    ScaleAndCenterUI(tvUI);
PlayVideo();
                }

    }

    private void FadeOutLives(float fadeDuration = 0.1f){
     if(livesImages != null)
            {
                foreach (var lifeImage in livesImages)
                {
                    if (lifeImage != null)
                        lifeImage.GetComponent<UIFade>().FadeOut(fadeDuration);
                }
            }

            if(livesUIFrame != null)
                livesUIFrame.GetComponent<UIFade>().FadeOut(fadeDuration);
            if(livesUIBG != null)                
                livesUIBG.GetComponent<UIFade>().FadeOut(fadeDuration);
    }

    private void FadeInLives(){
        if (livesImages != null)
        {
            foreach (var lifeImage in livesImages)
            {
                if (lifeImage != null)
                    lifeImage.GetComponent<UIFade>()?.FadeIn();
            }
        }

        if (livesUIFrame != null)
            livesUIFrame.GetComponent<UIFade>()?.FadeIn();

        if (livesUIBG != null)
            livesUIBG.GetComponent<UIFade>()?.FadeIn();
    }

    private void PlayVideo()
    {
                tvVideoStaticImage.GetComponent<UIFade>()?.FadeOut(0.1f);
        if (tvVideoPlayer != null){
            tvVideoPlayer.Play();

            if(AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomTvTalk();
        }

    }

    private void VideoStopped(VideoPlayer vp)
    {
        if (tvVideoStaticImage != null)
            tvVideoStaticImage.GetComponent<UIFade>().FadeIn();
    }
    private void ScaleAndCenterUI(GameObject uiElement)
    {
        RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        if (!uiOriginalTransforms.ContainsKey(rectTransform))
        {
            uiOriginalTransforms[rectTransform] = new UITransformSnapshot
            {
                anchoredPosition = rectTransform.anchoredPosition,
                localScale = rectTransform.localScale
            };
        }

        // Calculate scale factor to fit the visible camera rect
        float scaleX = visibleCameraRect.width / rectTransform.rect.width;
        float scaleY = visibleCameraRect.height / rectTransform.rect.height;
        float scaleAdjust = 0.9f; // Optional: Adjust to add some padding around the UI
        float scaleFactor = Mathf.Min(scaleX, scaleY) * scaleAdjust;
        Vector3 targetScale = new Vector3(scaleFactor, scaleFactor, 1f);

        // Center the UI element in the visible camera rect
        Vector2 centeredPosition = new Vector2(
            visibleCameraRect.x + visibleCameraRect.width / 2f,
            visibleCameraRect.y + visibleCameraRect.height / 2f - 300f
        );

        if (uiScaleCenterRoutine != null)
            StopCoroutine(uiScaleCenterRoutine);

        uiScaleCenterRoutine = StartCoroutine(ScaleAndCenterUIRoutine(rectTransform, centeredPosition, targetScale, gameOverUiTransitionDuration));
    }

    public void RestoreUIOriginalTransform(GameObject uiElement)
    {
        if (uiElement == null)
            return;

        RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
        if (rectTransform == null)
            return;

        UITransformSnapshot originalTransform;
        if (!uiOriginalTransforms.TryGetValue(rectTransform, out originalTransform))
            return;

        if (uiScaleCenterRoutine != null)
            StopCoroutine(uiScaleCenterRoutine);

        uiScaleCenterRoutine = StartCoroutine(ScaleAndCenterUIRoutine(
            rectTransform,
            originalTransform.anchoredPosition,
            originalTransform.localScale,
            gameOverUiTransitionDuration
        ));
    }

    private IEnumerator ScaleAndCenterUIRoutine(RectTransform rectTransform, Vector2 targetPosition, Vector3 targetScale, float duration)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = targetPosition;
            rectTransform.localScale = targetScale;
            uiScaleCenterRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        uiScaleCenterRoutine = null;
    }
    private SectorHandler GetRandomSector()
    {
        List<SectorHandler> availableSectors = new List<SectorHandler>();

        SectorHandler[] sectors = new SectorHandler[]
        {
            northSector, southSector, centerSector, sharonSector, eilatSector
        };

        foreach (SectorHandler sector in sectors)
        {
            if (sector == null)
                continue;

            if (sector.currentState == SectorState.Idle)
                availableSectors.Add(sector);
        }

        if (availableSectors.Count == 0)
            return null;

        return availableSectors[Random.Range(0, availableSectors.Count)];
    }

    private Vector2 GetSectorAnchoredPosition(SectorHandler sector)
    {
        return sector.GetComponent<RectTransform>().anchoredPosition;
    }

    private Vector2 GetMissileStartPosition(SectorName sector)
    {
        switch (sector)
        {
            case SectorName.Eilat:
                return new Vector2(1500f, Random.Range(-500f, 500f));

            case SectorName.South:
                return new Vector2(Random.Range(-350f, 350f), -2000f);

            case SectorName.Center:
                return new Vector2(Random.Range(-250f, 250f), 2000f);

            case SectorName.Sharon:
                return new Vector2(-1500f, Random.Range(-500f, 500f));

            case SectorName.North:
            default:
                return new Vector2(Random.Range(-350f, 350f), 2000f);
        }
    }

    public void ResetGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
            AudioManager.Instance.PlayGameplayBgm();
        }

        gameOver = false;

        if (loseReasonRoutine != null)
        {
            StopCoroutine(loseReasonRoutine);
            loseReasonRoutine = null;
        }

        if (uiScaleCenterRoutine != null)
        {
            StopCoroutine(uiScaleCenterRoutine);
            uiScaleCenterRoutine = null;
        }

        if (gameLoopRoutine != null)
        {
            StopCoroutine(gameLoopRoutine);
            gameLoopRoutine = null;
        }

        for (int i = activeMissiles.Count - 1; i >= 0; i--)
        {
            MissileEventData missile = activeMissiles[i];
            if (missile == null)
                continue;

            if (missile.missileUI != null)
                Destroy(missile.missileUI.gameObject);

            if (missile.indicatorUI != null)
                Destroy(missile.indicatorUI.gameObject);
        }
        activeMissiles.Clear();

        SectorHandler[] sectors = new SectorHandler[]
        {
            northSector, southSector, centerSector, sharonSector, eilatSector
        };

        foreach (var sector in sectors)
        {
            if (sector == null)
                continue;

            sector.StopStateTimer();
            sector.StopRepeatingStateTimer();
            sector.StopAllSectorVfx();
            sector.SetState(SectorState.Idle);
        }

        currentLives = startingLives;
        crisisAvoidedCount = 0;
        RefreshUItext();

        if (loseReasonText != null)
            loseReasonText.text = "Until today " + crisisAvoidedHighScoreCount + " crises avoided.";

        if (gameOverPanel != null)
            gameOverPanel.GetComponent<UIFade>()?.FadeOut(0.1f);


        if (toolBar != null)
            toolBar.GetComponent<UIFade>()?.FadeIn();

        if (tvVideoPlayer != null)
            tvVideoPlayer.Stop();

        if (tvVideoStaticImage != null)
            tvVideoStaticImage.GetComponent<UIFade>()?.FadeIn();

  

        gameLoopRoutine = StartCoroutine(GameLoop());
    }
}