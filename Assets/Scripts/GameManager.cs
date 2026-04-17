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
    public Button resetButton;
    public Image tvVideoStaticImage;
    public VideoPlayer tvVideoPlayer;

    [Header("Lives")]
    public int startingLives = 5;
    private int currentLives;

    [Header("Penalty UI")]
    public float loseReasonMessageDuration = 2.5f;

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
        crisisAvoidedHighScoreCount = PlayerPrefs.GetInt("HighScore", 0);
        RefreshUItext();

        if (loseReasonText != null)
            loseReasonText.text = "Until today " + crisisAvoidedHighScoreCount + " crises avoided.";

        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(1f);

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
    sector.ResolveIntercepted();

    if (sector.currentState == SectorState.Smoked)
    {
        sector.PlayInterceptSmokeSequenceAt(missileHitPosition, missileLayer);
        sector.StartIconFillUp(smokeClearTime, sector.releaseIconSprite);
        StartCoroutine(SmokeClearRoutine(sector));
    }
    else if (sector.currentState == SectorState.NeedsAmbulanceCheck)
    {
        StartAmbulancePenaltyLoop(sector);
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

        if (sector.currentState == SectorState.NeedsAmbulance || sector.currentState == SectorState.Lost)
            sector.PlayCrashSequenceThenSmoke();

        if (sector.currentState == SectorState.Lost)
        {
            TriggerGameOver("Missile hit " + sector.sectorName + " without alert.");
            CleanupMissileEvent(missileData);
            return;
        }

        if (sector.currentState == SectorState.NeedsAmbulance)
            StartAmbulancePenaltyLoop(sector);

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

        // Per your rule: AmbulanceWorking uses RELEASE icon filling 0 -> 1
        sector.StartIconFillUp(ambulanceProcessTime, sector.releaseIconSprite);

        StartCoroutine(AmbulanceRoutine(sector));
    }

    private IEnumerator SmokeClearRoutine(SectorHandler sector)
    {
        yield return new WaitForSeconds(smokeClearTime);

        if (sector != null && sector.currentState == SectorState.Smoked)
        {
            yield return StartCoroutine(sector.ClearSmokeWithAnimation());
            sector.SetReadyForRelease();
            StartReleasePenaltyLoop(sector);
        }
    }

    private IEnumerator AmbulanceRoutine(SectorHandler sector)
    {
        yield return new WaitForSeconds(ambulanceProcessTime);

        if (sector == null)
            yield break;

        if (sector.currentState == SectorState.AmbulanceWorking)
        {
            sector.SetReadyForRelease();
            StartReleasePenaltyLoop(sector);
        }
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

        currentLives--;
        RefreshUItext();
        ShowLoseReason(reason, sectorName);

        Debug.Log("Lost life: " + reason + " | Lives left: " + currentLives);

        if (currentLives <= 0)
            TriggerGameOver(reason);
    }
     public void CrisisAvoided()
    {
        if (gameOver)
            return;

        crisisAvoidedCount++;
        if (crisisAvoidedCount > crisisAvoidedHighScoreCount){
            crisisAvoidedHighScoreCount = crisisAvoidedCount;
            PlayerPrefs.SetInt("HighScore", crisisAvoidedHighScoreCount);
        }
        RefreshUItext();

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
        tvVideoStaticImage.gameObject.SetActive(false);
        tvVideoPlayer.Play();
        loseReasonText.text = reason;

        yield return new WaitForSeconds(loseReasonMessageDuration);

        if (!gameOver && loseReasonText != null){
        tvVideoStaticImage.gameObject.SetActive(true);
            
            string status = currentLives == 5 ? "No crashes so far" : currentLives == 4 ? "" + (startingLives - currentLives) + "An event occured in " + sectorName : "" + (startingLives - currentLives) + "events occured all around the country";

            loseReasonText.text = "Today " + crisisAvoidedCount + " crises avoided.\n" + status;
        }

        loseReasonRoutine = null;
    }

    private void TriggerGameOver(string reason)
    {
        gameOver = true;

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
            loseReasonText.text = reason;

            if (resetButton != null)
            {
                resetButton.gameObject.SetActive(true);
            }
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}