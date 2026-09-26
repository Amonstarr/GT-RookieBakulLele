using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any Button to play a click sound effect through the persistent
/// GameSettingsManager (routed via the SFX mixer group, so it respects the
/// SFX volume slider).
///
/// Leave "Click Clip" empty to use GameSettingsManager's default button-click
/// clip (e.g. ketik (1).mp3) - assign a clip here only if this specific button
/// needs a different sound.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSFX : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (GameSettingsManager.Instance == null) return;

        if (clickClip != null)
            GameSettingsManager.Instance.PlaySFX(clickClip);
        else
            GameSettingsManager.Instance.PlayButtonClick();
    }
}
