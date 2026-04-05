using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum MissileSpawnSide
{
    Eilat,
    South,
    Center,
    Sharon,
    North,
}

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

    [Header("UI")]
    public TMP_Text gameStateText;

    [Header("Missile Settings")]
    public float delayBeforeNextMissile = 2f;
    public float preLaunchWarningTime = 1.2f;
    public float missileTravelDuration = 5.5f;
    public float smokeClearTime = 5f;
    public float ambulanceRecoveryDelay = 10f;

    [Header("Visible Screen Area In Canvas Space")]
    public Rect visibleCameraRect = new Rect(-540f, -960f, 1080f, 1920f);

    private bool gameOver = false;

    private readonly List<MissileEventData> activeMissiles = new List<MissileEventData>();

    private class MissileEventData
    {
        public SectorHandler targetSector;
        public MissileUI missileUI;
        public MissileDirectionIndicator indicatorUI;
        public bool resolved;
        public MissileSpawnSide spawnSide;
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
            yield return new WaitForSeconds(delayBeforeNextMissile);
        }
    }

    private IEnumerator StartMissileEventRoutine()
    {
        SectorHandler targetSector = GetRandomSector();
        if (targetSector == null)
            yield break;

        targetSector.BeginIncoming();

        MissileSpawnSide sector = GetSpawnSideForSector(targetSector.sectorName);

        MissileDirectionIndicator indicator = Instantiate(directionIndicatorPrefab, indicatorLayer);
        indicator.Show(sector);

        MissileEventData missileData = new MissileEventData
        {
            targetSector = targetSector,
            indicatorUI = indicator,
            missileUI = null,
            resolved = false,
            spawnSide = sector
        };

        activeMissiles.Add(missileData);

        yield return new WaitForSeconds(preLaunchWarningTime);

        if (gameOver || missileData.resolved)
            yield break;

        MissileUI missile = Instantiate(missilePrefab, missileLayer);
        missileData.missileUI = missile;

        Vector2 startPos = GetMissileStartPosition(sector);
        Vector2 targetPos = GetSectorAnchoredPosition(targetSector);

        missile.Launch(
            startPos,
            targetPos,
            missileTravelDuration,
            visibleCameraRect,
            () => OnMissileEnteredScreen(missileData),
            () => OnMissileImpact(missileData),
            () => OnMissileTapped(missileData)
        );
    }

    private void OnMissileEnteredScreen(MissileEventData missileData)
    {
        if (missileData == null || missileData.indicatorUI == null)
            return;

        missileData.indicatorUI.Hide();
    }

    private void OnMissileTapped(MissileEventData missileData)
    {
        if (missileData == null || missileData.resolved || missileData.targetSector == null)
            return;

        missileData.resolved = true;

        if (missileData.missileUI != null)
            missileData.missileUI.HideMissile();

        if (missileData.indicatorUI != null)
            missileData.indicatorUI.Hide();

        missileData.targetSector.ResolveIntercepted();

        if (missileData.targetSector.currentState == SectorState.Smoked)
            StartCoroutine(SmokeClearRoutine(missileData.targetSector));

        CleanupMissileEvent(missileData);
    }

    private void OnMissileImpact(MissileEventData missileData)
    {
        if (missileData == null || missileData.resolved || missileData.targetSector == null)
            return;

        missileData.resolved = true;

        if (missileData.indicatorUI != null)
            missileData.indicatorUI.Hide();

        missileData.targetSector.ResolveCrash();

        if (missileData.targetSector.currentState == SectorState.Lost)
        {
            TriggerGameOver();
            return;
        }

        CleanupMissileEvent(missileData);
    }

    private void CleanupMissileEvent(MissileEventData missileData)
    {
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
                missile.indicatorUI.Hide();
        }

        if (gameStateText != null)
            gameStateText.text = "GAME OVER";

        Debug.Log("Game Over: sector was not alerted and missile crashed.");
    }

    private SectorHandler GetRandomSector()
    {
        SectorHandler[] sectors = new SectorHandler[]
        {
            northSector, southSector, centerSector, sharonSector, eilatSector
        };

        return sectors[Random.Range(0, sectors.Length)];
    }

    private Vector2 GetSectorAnchoredPosition(SectorHandler sector)
    {
        return sector.GetComponent<RectTransform>().anchoredPosition;
    }

    private MissileSpawnSide GetSpawnSideForSector(string sectorName)
    {
        switch (sectorName.ToLowerInvariant())
        {
            case "eilat":
                return MissileSpawnSide.Eilat;
            case "south":
                return MissileSpawnSide.South;
            case "center":
                return MissileSpawnSide.Center;
            case "sharon":
                return MissileSpawnSide.Sharon;
            case "north":
                return MissileSpawnSide.North;
            default:
                return MissileSpawnSide.North;
        }
    }

    private Vector2 GetMissileStartPosition(MissileSpawnSide sector)
    {
        switch (sector)
        {
            case MissileSpawnSide.Eilat:
                return new Vector2(900f, Random.Range(-500f, 500f));

            case MissileSpawnSide.South:
                return new Vector2(Random.Range(-350f, 350f), -1400f);

            case MissileSpawnSide.Center:
                return new Vector2(Random.Range(-350f, 350f), 1400f);

            case MissileSpawnSide.Sharon:
                return new Vector2(-900f, Random.Range(-500f, 500f));

            case MissileSpawnSide.North:
            default:
                return new Vector2(Random.Range(-350f, 350f), 1400f);
        }
    }
}