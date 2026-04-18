using UnityEngine;

public class UIAlarmVibrate : MonoBehaviour
{
   public RectTransform target;

    [Header("Vibration Settings")]
    public float angleAmount = 6f;
    public float speed = 25f;

    [Header("Pulse Settings")]
    public float vibrateDuration = 0.3f;
    public float pauseDuration = 0.4f;

    private Vector3 startPos;
    private Quaternion startRot;

    private float timer;
    private bool isVibrating = true;
    private bool playedVibrateSfxThisPulse;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        startRot = target.localRotation;
    }

    private void OnEnable()
    {
        startRot = target.localRotation;
        timer = vibrateDuration;
        isVibrating = true;
        playedVibrateSfxThisPulse = false;
    }

    private void Update()
    {
timer -= Time.deltaTime;

        if (isVibrating)
        {
            if (!playedVibrateSfxThisPulse)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayVibrate();

                    // Handheld.Vibrate();

                playedVibrateSfxThisPulse = true;
            }

            // Vibrate
float wave = Mathf.Sin(Time.time * speed);
            float z = wave * angleAmount;
            target.localRotation = startRot * Quaternion.Euler(0f, 0f, z);

            if (timer <= 0f)
            {
                isVibrating = false;
                timer = pauseDuration;

                // Reset rotation when stopping
                target.localRotation = startRot;
            }
        }
        else
        {
            // Pause (no movement)
            if (timer <= 0f)
            {
                isVibrating = true;
                timer = vibrateDuration;
                playedVibrateSfxThisPulse = false;
            }
        }
    }

    private void OnDisable()
    {
        target.localRotation = startRot;
    }
}