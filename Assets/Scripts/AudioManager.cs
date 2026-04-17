using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Background Music")]
    [SerializeField] private AudioClip mainMenuBgm;
    [SerializeField] private AudioClip gameplayBgm;
    [SerializeField] private AudioClip gameOverBgm;

    [Header("General SFX")]
    [SerializeField] private AudioClip missileWarningSfx;
    [SerializeField] private AudioClip missileLaunchSfx;
    [SerializeField] private AudioClip missileTapSfx;
    [SerializeField] private AudioClip missileImpactSfx;
    [SerializeField] private AudioClip smokeSfx;
    [SerializeField] private AudioClip explosionSfx;
    [SerializeField] private AudioClip ambulanceSfx;
    [SerializeField] private AudioClip releaseSfx;
    [SerializeField] private AudioClip loseLifeSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip alertPlacedSfx;
    [SerializeField] private AudioClip invalidActionSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null || sfxSource == null)
            SetupDefaultSources();

        ApplyVolumes();
    }

    private void SetupDefaultSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (bgmSource == null)
        {
            if (sources.Length > 0)
            {
                bgmSource = sources[0];
            }
            else
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (sfxSource == null)
        {
            if (sources.Length > 1)
            {
                sfxSource = sources[1];
            }
            else
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
            bgmSource.volume = bgmVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void PlayBgm(AudioClip clip, bool restartIfSame = false)
    {
        if (bgmSource == null || clip == null)
            return;

        if (!restartIfSame && bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume * volumeMultiplier));
    }

    // ---- Easy named calls ----

    public void PlayMainMenuBgm() => PlayBgm(mainMenuBgm);
    public void PlayGameplayBgm() => PlayBgm(gameplayBgm);
    public void PlayGameOverBgm() => PlayBgm(gameOverBgm, true);

    public void PlayMissileWarning() => PlaySfx(missileWarningSfx);
    public void PlayMissileLaunch() => PlaySfx(missileLaunchSfx);
    public void PlayMissileTap() => PlaySfx(missileTapSfx);
    public void PlayMissileImpact() => PlaySfx(missileImpactSfx);
    public void PlaySmoke() => PlaySfx(smokeSfx);
    public void PlayExplosion() => PlaySfx(explosionSfx);
    public void PlayAmbulance() => PlaySfx(ambulanceSfx);
    public void PlayRelease() => PlaySfx(releaseSfx);
    public void PlayLoseLife() => PlaySfx(loseLifeSfx);
    public void PlayGameOver() => PlaySfx(gameOverSfx);
    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayAlertPlaced() => PlaySfx(alertPlacedSfx);
    public void PlayInvalidAction() => PlaySfx(invalidActionSfx);
}