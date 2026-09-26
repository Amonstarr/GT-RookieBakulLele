using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent (DontDestroyOnLoad) singleton that manages BGM volume, SFX volume,
/// background music playback, and brightness. All three settings are saved to PlayerPrefs,
/// so they persist across scenes AND app restarts.
///
/// SETUP:
/// 1. Create an AudioMixer asset (Assets > Create > Audio Mixer) with two child
///    groups under Master: "BGM" and "SFX".
/// 2. Right-click each child group's Volume slider in the Mixer window > "Expose to script".
///    Rename the exposed params (top-left "Exposed Parameters" panel) to
///    "BGMVolume" and "SFXVolume" exactly.
/// 3. Route your music AudioSource(s) to the "BGM" mixer group, and your sound
///    effect AudioSource(s) to the "SFX" group (AudioSource > Output).
/// 4. Create an empty GameObject named "GameSettingsManager" in your FIRST scene
///    (e.g. MainMenu), attach this script, and drag the AudioMixer asset and BGM Clip in.
///    Because of DontDestroyOnLoad you only need this in ONE scene.
/// 5. Brightness has no direct "screen brightness" API on desktop, so it's applied
///    via a full-screen dark overlay Image in each scene - see BrightnessOverlay.cs.
///    This manager just stores the value and fires OnBrightnessChanged so any
///    active BrightnessOverlay can update itself immediately.
/// </summary>
public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [Header("Audio Mixer Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";
    [SerializeField] private string bgmMixerGroupName = "BGM";
    [SerializeField] private string sfxMixerGroupName = "SFX";

    [Header("BGM Playback")]
    [Tooltip("Clip audio BGM tunggal yang akan diputar berulang-ulang (loop) di seluruh game.")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private bool autoPlayBGM = true;
    private AudioSource bgmSource;

    [Header("SFX Playback")]
    [Tooltip("Played by ButtonClickSFX on any button that doesn't specify its own clip.")]
    [SerializeField] private AudioClip defaultButtonClickClip;
    private AudioSource sfxSource;

    private const string BgmKey = "BGMVolume";
    private const string SfxKey = "SFXVolume";
    private const string BrightnessKey = "Brightness";

    /// <summary>Fired whenever brightness changes, value 0-1. Any scene's BrightnessOverlay should subscribe.</summary>
    public event Action<float> OnBrightnessChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup BGM AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f; // 2D Stereo
        if (audioMixer != null)
        {
            var bgmGroups = audioMixer.FindMatchingGroups(bgmMixerGroupName);
            if (bgmGroups.Length > 0) bgmSource.outputAudioMixerGroup = bgmGroups[0];
        }

        // Setup SFX AudioSource
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D Stereo
        if (audioMixer != null)
        {
            var sfxGroups = audioMixer.FindMatchingGroups(sfxMixerGroupName);
            if (sfxGroups.Length > 0) sfxSource.outputAudioMixerGroup = sfxGroups[0];
        }

        SetBGMVolume(PlayerPrefs.GetFloat(BgmKey, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SfxKey, 1f));
        // Brightness is just stored here; overlays pull it via GetBrightness() on their own Start().
    }

    private void Start()
    {
        // Re-apply in Start to ensure AudioMixer exposed parameters are fully registered
        SetBGMVolume(PlayerPrefs.GetFloat(BgmKey, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SfxKey, 1f));

        if (autoPlayBGM && bgmClip != null)
        {
            PlayBGM(bgmClip);
        }
    }

    /// <summary>Memutar BGM secara terus menerus (loop). Jika clip tidak disertakan, memutar bgmClip bawaan.</summary>
    public void PlayBGM(AudioClip clip = null)
    {
        if (clip != null)
        {
            bgmClip = clip;
        }

        if (bgmSource == null || bgmClip == null) return;
        if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    /// <summary>Menghentikan pemutaran BGM.</summary>
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>Pause BGM sementara.</summary>
    public void PauseBGM()
    {
        if (bgmSource != null) bgmSource.Pause();
    }

    /// <summary>Resume BGM yang sedang di-pause.</summary>
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying) bgmSource.UnPause();
    }

    public AudioClip CurrentBGMClip => bgmClip;
    public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;

    /// <summary>Plays a one-shot sound effect through the SFX mixer group (respects the SFX volume slider).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>Convenience for buttons that don't assign their own click sound.</summary>
    public void PlayButtonClick()
    {
        PlaySFX(defaultButtonClickClip);
    }

    public float GetBGMVolume() => PlayerPrefs.GetFloat(BgmKey, 1f);
    public float GetSFXVolume() => PlayerPrefs.GetFloat(SfxKey, 1f);
    public float GetBrightness() => PlayerPrefs.GetFloat(BrightnessKey, 1f);

    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(bgmParam, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(BgmKey, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        if (audioMixer != null) audioMixer.SetFloat(sfxParam, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(SfxKey, value);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        PlayerPrefs.Save();
        OnBrightnessChanged?.Invoke(value);
    }
}
