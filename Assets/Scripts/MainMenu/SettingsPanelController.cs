using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens/closes the existing Settings panel (BGM, SFX, Brightness sliders + Back button)
/// when the computer icon is clicked, and wires all 3 sliders to GameSettingsManager.
/// Attach to a manager GameObject in EACH scene that has the computer icon + this panel.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button computerIconButton;
    [SerializeField] private Button backButton;

    [Header("Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider brightnessSlider;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (computerIconButton != null) computerIconButton.onClick.AddListener(OpenSettings);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);

        var settings = GameSettingsManager.Instance;
        if (settings == null) return;

        if (bgmSlider != null)
        {
            bgmSlider.value = settings.GetBGMVolume();
            bgmSlider.onValueChanged.AddListener(settings.SetBGMVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = settings.GetSFXVolume();
            sfxSlider.onValueChanged.AddListener(settings.SetSFXVolume);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.value = settings.GetBrightness();
            brightnessSlider.onValueChanged.AddListener(settings.SetBrightness);
        }
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
