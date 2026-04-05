using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sectors")]
    public SectorHandler northSector;
    public SectorHandler sharonSector;
    public SectorHandler centerSector;
    public SectorHandler eilatSector;
    public SectorHandler southSector;

    [Header("UI")]
    public TMP_Text directionIndicatorText;
    public MissileUI missileUI;
    public TMP_Text gameStateText;

    [Header("Missile Settings")]
    public float delayBeforeNextMissile = 2f;
    public float missileTravelDuration = 3f;
    public float smokeClearTime = 5f;
    public float ambulanceRecoveryDelay = 10f;

    private SectorHandler currentTargetSector;
    private bool missileEventActive = false;
    private bool missileResolved = false;
    private bool gameOver = false;

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
            if (!missileEventActive)
            {
                StartMissileEvent();
            }

            yield return new WaitUntil(() => !missileEventActive || gameOver);
            yield return new WaitForSeconds(delayBeforeNextMissile);
        }
    }

    private void StartMissileEvent()
    {
        missileEventActive = true;
        missileResolved = false;

        currentTargetSector = GetRandomSector();
        currentTargetSector.BeginIncoming();

        UpdateDirectionIndicator(currentTargetSector.sectorName);

        Vector2 startPos = GetMissileStartPosition(currentTargetSector.sectorName);
        Vector2 targetPos = GetSectorAnchoredPosition(currentTargetSector);

        missileUI.Launch(
            startPos,
            targetPos,
            missileTravelDuration,
            OnMissileImpact,
            OnMissileTapped
        );
    }

    private void OnMissileTapped()
    {
        if (!missileEventActive || missileResolved || currentTargetSector == null)
            return;

        missileResolved = true;
        missileUI.HideMissile();

        currentTargetSector.ResolveIntercepted();

        if (currentTargetSector.currentState == SectorState.Smoked)
        {
            StartCoroutine(SmokeClearRoutine(currentTargetSector));
        }

        EndMissileEvent();
    }

    private void OnMissileImpact()
    {
        if (!missileEventActive || missileResolved || currentTargetSector == null)
            return;

        missileResolved = true;
        currentTargetSector.ResolveCrash();

        if (currentTargetSector.currentState == SectorState.Lost)
        {
            TriggerGameOver();
            return;
        }

        EndMissileEvent();
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
            Debug.Log(sector.sectorName + " ambulance check complete. Returned to idle.");
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

    private void EndMissileEvent()
    {
        missileUI.HideMissile();
        directionIndicatorText.text = "-";
        missileEventActive = false;
        currentTargetSector = null;
    }

    private void TriggerGameOver()
    {
        gameOver = true;
        missileEventActive = false;
        missileUI.HideMissile();
        directionIndicatorText.text = "X";

        if (gameStateText != null)
        {
            gameStateText.text = "GAME OVER";
        }

        Debug.Log("Game Over: sector was not alerted and missile crashed.");
    }

    private SectorHandler GetRandomSector()
    {
        SectorHandler[] sectors = new SectorHandler[]
        {
            northSector, southSector, centerSector, sharonSector, eilatSector
        };

        int randomIndex = Random.Range(0, sectors.Length);
        return sectors[randomIndex];
    }

    private void UpdateDirectionIndicator(string sectorName)
    {
        directionIndicatorText.text = sectorName.ToUpper();
    }

    private Vector2 GetSectorAnchoredPosition(SectorHandler sector)
    {
        return sector.GetComponent<RectTransform>().anchoredPosition;
    }

    private Vector2 GetMissileStartPosition(string sectorName)
    {
        switch (sectorName.ToLowerInvariant())
        {
            case "north": return new Vector2(0f, 1400f);
            case "south": return new Vector2(0f, -1400f);
            case "sharon":  return new Vector2(-800f, 0f);
            case "eilat":  return new Vector2(800f, 0f);
            case "center": return new Vector2(0f, 1400f);
            default: return new Vector2(0f, 1400f);
        }
    }
}