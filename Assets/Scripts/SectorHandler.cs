using System.Collections;
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
    NeedsAmbulance,          // alerted + crashed
    Damaged,                 // optional visual/result state if you want to keep it
    Lost                     // not alerted + crashed
}


public class SectorHandler : MonoBehaviour
{
    [Header("Sector Info")]
    public string sectorName;
    public SectorState currentState = SectorState.Idle;

    [Header("Visuals")]
    public Image baseImage;

    [Header("State Colors")]
    public Color idleColor = Color.white;
    public Color incomingColor = new Color(1f, 0.6f, 0.2f);
    public Color alertedIncomingColor = Color.yellow;
    public Color smokedColor = Color.gray;
    public Color waitingForReleaseColor = Color.cyan;
    public Color needsAmbulanceCheckColor = new Color(1f, 0.5f, 0f);
    public Color needsAmbulanceColor = Color.red;
    public Color lostColor = Color.black;

    private Coroutine flickerRoutine;

    private void Start()
    {
        UpdateVisual();
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
            {
                GameManager.Instance.StartAmbulanceProcess(this);
            }
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
        currentState = newState;
        UpdateVisual();
    }

    private void StartFlicker()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
        }

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
            baseImage.enabled = true;
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            if (baseImage != null)
            {
                baseImage.enabled = !baseImage.enabled;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private void UpdateVisual()
    {
        if (baseImage == null) return;

        baseImage.enabled = true;

        switch (currentState)
        {
            case SectorState.Idle:
                baseImage.color = idleColor;
                break;
            case SectorState.Incoming:
                baseImage.color = incomingColor;
                break;
            case SectorState.AlertedIncoming:
                baseImage.color = alertedIncomingColor;
                break;
            case SectorState.Smoked:
                baseImage.color = smokedColor;
                break;
            case SectorState.WaitingForRelease:
                baseImage.color = waitingForReleaseColor;
                break;
            case SectorState.NeedsAmbulanceCheck:
                baseImage.color = needsAmbulanceCheckColor;
                break;
            case SectorState.NeedsAmbulance:
                baseImage.color = needsAmbulanceColor;
                break;
            case SectorState.Lost:
                baseImage.color = lostColor;
                break;
        }
    }
}