using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent (DontDestroyOnLoad) singleton that manages BGM volume, SFX volume,
/// and brightness. All three are saved to PlayerPrefs, so they persist across
/// scenes AND app restarts - adjust them in any scene, they apply everywhere.
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
///    (e.g. MainMenu), attach this script, and drag the AudioMixer asset in.
///    Because of DontDestroyOnLoad you only need this in ONE scene.
/// 5. Brightness has no direct "screen brightness" API on desktop, so it's applied
///    via a full-screen dark overlay Image in each scene - see BrightnessOverlay.cs.
///    This manager just stores the value and fires OnBrightnessChanged so any
///    active BrightnessOverlay can update itself immediately.
/// </summary>
public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXVolume";
    [SerializeField] private string sfxMixerGroupName = "SFX"; // must match the child group's name in the Mixer

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

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        if (audioMixer != null)
        {
            var groups = audioMixer.FindMatchingGroups(sfxMixerGroupName);
            if (groups.Length > 0) sfxSource.outputAudioMixerGroup = groups[0];
        }

        SetBGMVolume(PlayerPrefs.GetFloat(BgmKey, 1f));
        SetSFXVolume(PlayerPrefs.GetFloat(SfxKey, 1f));
        // Brightness is just stored here; overlays pull it via GetBrightness() on their own Start().
    }

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
