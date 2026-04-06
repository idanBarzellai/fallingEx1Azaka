using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


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
    public TMP_Text gameStateText;

    [Header("Missile Settings")]
    public float preLaunchWarningTime = 1.2f;
    public float missileTravelDuration = 5.5f;
    public float smokeClearTime = 5f;
    public float ambulanceRecoveryDelay = 10f;
    [SerializeField] private float minLaunchDelay = 3.5f;
[SerializeField] private float maxLaunchDelay = 6.5f;

    private bool gameOver = false;
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


        MissileDirectionIndicator indicator = null;
        if (directionIndicatorPrefab != null && indicatorLayer != null)
        {
            indicator = Instantiate(directionIndicatorPrefab, indicatorLayer);
        }

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
        {
            missileData.indicatorUI.BeginTracking(missile.RectTransform, visibleCameraRect);
        }
    }

    private void OnMissileTapped(MissileEventData missileData)
    {
        if (missileData == null || missileData.resolved || missileData.targetSector == null)
            return;

        missileData.resolved = true;

        if (missileData.missileUI != null)
            missileData.missileUI.HideMissile();

        if (missileData.indicatorUI != null)
            missileData.indicatorUI.StopTracking();

       missileData.targetSector.ResolveIntercepted();

if (missileData.targetSector.currentState == SectorState.Smoked)
{
    missileData.targetSector.PlayInterceptSmokeSequence();
    StartCoroutine(SmokeClearRoutine(missileData.targetSector));
}

        CleanupMissileEvent(missileData);
    }

    private void OnMissileImpact(MissileEventData missileData)
    {
        if (missileData == null || missileData.resolved || missileData.targetSector == null)
            return;

        missileData.resolved = true;

        if (missileData.indicatorUI != null)
            missileData.indicatorUI.StopTracking();

missileData.targetSector.ResolveCrash();

if (missileData.targetSector.currentState == SectorState.NeedsAmbulance ||
    missileData.targetSector.currentState == SectorState.Lost)
{
    missileData.targetSector.PlayCrashSequenceThenSmoke();
}

if (missileData.targetSector.currentState == SectorState.Lost)
{
    TriggerGameOver();
    return;
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
        StartCoroutine(AmbulanceRoutine(sector));
    }

private IEnumerator SmokeClearRoutine(SectorHandler sector)
{
    yield return new WaitForSeconds(smokeClearTime);

    if (sector.currentState == SectorState.Smoked)
    {
        yield return StartCoroutine(sector.ClearSmokeWithAnimation());
        sector.SetReadyForRelease();
        Debug.Log(sector.sectorName + " smoke cleared. Ready for release.");
    }
}
    private IEnumerator AmbulanceRoutine(SectorHandler sector)
    {
        SectorState initialState = sector.currentState;

        Debug.Log("Ambulance dispatched to " + sector.sectorName);

        if (initialState == SectorState.NeedsAmbulanceCheck)
        {
            yield return new WaitForSeconds(2f);
            sector.SetReadyForRelease();
            Debug.Log(sector.sectorName + " ambulance check complete. Ready for release.");
        }
        else if (initialState == SectorState.NeedsAmbulance)
        {
            yield return new WaitForSeconds(2f);
            yield return new WaitForSeconds(ambulanceRecoveryDelay);

            if (sector.currentState == SectorState.NeedsAmbulance)
            {
                sector.SetReadyForRelease();
                Debug.Log(sector.sectorName + " ambulance finished. Ready for release.");
            }
        }
    }

    private void TriggerGameOver()
    {
        gameOver = true;

        foreach (var missile in activeMissiles)
        {
            if (missile.missileUI != null)
                missile.missileUI.HideMissile();

            if (missile.indicatorUI != null)
                missile.indicatorUI.StopTracking();
        }

        if (gameStateText != null)
            gameStateText.text = "GAME OVER";

        Debug.Log("Game Over: sector was not alerted and missile crashed.");
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
}