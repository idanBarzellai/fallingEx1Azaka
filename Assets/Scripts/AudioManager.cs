using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource alertSource;
    [SerializeField] private AudioSource tvTalkSource;
    private bool isTvTalkPaused = false;
    private bool isAlertPaused = false;



    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Background Music")]
    [SerializeField] private AudioClip gameplayBgm;

    [Header("General SFX")]
    [SerializeField] private AudioClip missileWarningSfx;
    [SerializeField] private AudioClip missileTapSfx;
    [SerializeField] private AudioClip[] missileImpactSfxs;
    [SerializeField] private AudioClip explosionSfx;
    [SerializeField] private AudioClip ambulanceSfx;
    [SerializeField] private AudioClip releaseSfx;
    [SerializeField] private AudioClip loseLifeSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip invalidActionSfx;
    [SerializeField] private AudioClip validActionSfx;

    [SerializeField] private AudioClip vibrateSfx;

    [SerializeField] private AudioClip[] tvTalkSfx;


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

        alertSource.loop = true;
        alertSource.playOnAwake = false;

        tvTalkSource.loop = false;
        tvTalkSource.playOnAwake = false;
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

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
            return;

bgmSource.Stop();
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

    public void PlaySfxIfAssigned(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null)
            return;

        PlaySfx(clip, volumeMultiplier);
    }

    // ---- Easy named calls ----

    public void PlayGameplayBgm() => PlayBgm(gameplayBgm);

    public void PlayMissileTap() => PlaySfx(missileTapSfx, 2f);
    public void PlayExplosion() {
        PlaySfx(explosionSfx, 0.2f);
    } 
        
    public void PlayAmbulance() => PlaySfx(ambulanceSfx,0.5f);
    public void PlayRelease() => PlaySfx(releaseSfx, 0.7f);
    public void PlayLoseLife() => PlaySfx(loseLifeSfx);
    public void PlayGameOver() {
        // sfxSource.Stop(); // stop any other sfx to ensure game over is heard clearly
        PlaySfx(gameOverSfx);
    }
    public void PlayButtonClick() => PlaySfx(buttonClickSfx);
    public void PlayInvalidAction() => PlaySfx(invalidActionSfx);
    public void PlayValidAction() => PlaySfx(validActionSfx);
    public void PlayVibrate() => PlaySfx(vibrateSfx);
    public void PlayMissileImpact()
    {
        if (missileImpactSfxs != null && missileImpactSfxs.Length > 0)
        {
            int index = Random.Range(0, missileImpactSfxs.Length);
            PlaySfx(missileImpactSfxs[index]);
        }
    }

    public void PlayMissileWarning(float volumeMultiplier = 1f) {
if (alertSource == null || missileWarningSfx == null)
            return;

        alertSource.PlayOneShot(missileWarningSfx, Mathf.Clamp01(sfxVolume * volumeMultiplier));    }


    public void PlayRandomTvTalk(float volumeMultiplier = 0.5f)
    {
        if (tvTalkSfx != null && tvTalkSfx.Length > 0)
        {
            int index = Random.Range(0, tvTalkSfx.Length);
            if (tvTalkSource == null || tvTalkSfx[index] == null)
                return;
tvTalkSource.Stop();
            tvTalkSource.PlayOneShot(tvTalkSfx[index], Mathf.Clamp01(sfxVolume * volumeMultiplier));
        }
    }

    public void PauseGeneralAudio()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Pause();

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Pause();

        PauseAlert();
        PauseTvTalk();
    }

    public void UnPauseGeneralAudio()
    {
        if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.UnPause();

        if (sfxSource != null && !sfxSource.isPlaying)
            sfxSource.UnPause();

        UnPauseAlert();
        UnPauseTvTalk();
    }

    public void PauseAlert()
    {
        if (alertSource != null && alertSource.isPlaying)
        {
            alertSource.Pause();
            isAlertPaused = true;
        }
    }

    public void UnPauseAlert()
    {
        if (alertSource != null && isAlertPaused)
        {
            alertSource.UnPause();
            isAlertPaused = false;
        }
    }

    public void PauseTvTalk()
    {
        if (tvTalkSource != null && tvTalkSource.isPlaying)
        {
            tvTalkSource.Pause();
            isTvTalkPaused = true;
        }
    }

    public void UnPauseTvTalk()
    {
        if (tvTalkSource != null && isTvTalkPaused)
        {
            tvTalkSource.UnPause();
            isTvTalkPaused = false;
        }
    }


 public void StopGeneralAudio()
    {
        if (sfxSource != null)
            sfxSource.Stop();

        StopAlert();
        StopTvTalk();
    }


    public void StopAlert()
{
    if (alertSource != null && alertSource.isPlaying)
        alertSource.Stop();
}

    public void StopTvTalk()
    {
        if (tvTalkSource != null && tvTalkSource.isPlaying)
        tvTalkSource.Stop();
    }
}