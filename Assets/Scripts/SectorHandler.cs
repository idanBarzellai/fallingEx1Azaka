using UnityEngine;
using UnityEngine.UI;

public enum SectorState
{
    Idle,
    Alerted,
    Smoked,
    Damaged
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
    public Color alertedColor = Color.yellow;
    public Color smokedColor = Color.gray;
    public Color damagedColor = Color.red;

    private void Start()
    {
        baseImage = GetComponent<Image>();
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

            default:
                Debug.LogWarning("Unknown tool type: " + toolType);
                break;
        }
    }

    private void TryApplyAlert()
    {
        if (currentState == SectorState.Idle)
        {
            currentState = SectorState.Alerted;
            Debug.Log(sectorName + " is now ALERTED");
            UpdateVisual();
        }
        else
        {
            Debug.Log("Alert cannot be applied to " + sectorName + " while state is " + currentState);
        }
    }

    private void TryApplyRelease()
    {
        if (currentState == SectorState.Smoked)
        {
            currentState = SectorState.Idle;
            Debug.Log(sectorName + " released back to IDLE");
            UpdateVisual();
        }
        else
        {
            Debug.Log("Release cannot be applied to " + sectorName + " while state is " + currentState);
        }
    }

    private void TryApplyAmbulance()
    {
        if (currentState == SectorState.Damaged)
        {
            currentState = SectorState.Idle;
            Debug.Log("Ambulance resolved damage in " + sectorName);
            UpdateVisual();
        }
        else
        {
            Debug.Log("Ambulance cannot be applied to " + sectorName + " while state is " + currentState);
        }
    }

    public void SetState(SectorState newState)
    {
        currentState = newState;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (baseImage == null)
        {
            Debug.LogWarning("Base Image missing on " + sectorName);
            return;
        }

        switch (currentState)
        {
            case SectorState.Idle:
                baseImage.color = idleColor;
                break;

            case SectorState.Alerted:
                baseImage.color = alertedColor;
                break;

            case SectorState.Smoked:
                baseImage.color = smokedColor;
                break;

            case SectorState.Damaged:
                baseImage.color = damagedColor;
                break;
        }
    }
}