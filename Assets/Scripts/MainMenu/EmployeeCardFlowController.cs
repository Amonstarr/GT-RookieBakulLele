using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the flow: Play button -> show Employee Card panel ->
/// player fills Nama + Kode CAAS -> Next button unlocks -> load cutscene.
/// Attach this to a manager GameObject and wire up the references in the Inspector.
/// </summary>
public class EmployeeCardFlowController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject playPanel;          // leaderboard + play button screen
    [SerializeField] private GameObject employeeCardPanel;  // "Kartu Kepegawaian" panel

    [Header("Play Button")]
    [SerializeField] private Button playButton;

    [Header("Employee Card Inputs")]
    [SerializeField] private TMP_InputField namaInput;
    [SerializeField] private TMP_InputField kodeCaasInput;
    [SerializeField] private Button nextButton;

    [Header("Cutscene")]
    [SerializeField] private string cutsceneSceneName = "Cutscene";
    // If you're not switching scenes, you can instead activate a cutscene panel/VideoPlayer here.

    private void Start()
    {
        // Start on the play screen, card panel hidden.
        if (employeeCardPanel != null) employeeCardPanel.SetActive(false);

        // Next button starts disabled until both fields are valid.
        SetNextInteractable(false);

        // Wire up listeners.
        if (playButton != null) playButton.onClick.AddListener(OnPlayPressed);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPressed);

        if (namaInput != null) namaInput.onValueChanged.AddListener(_ => ValidateInputs());
        if (kodeCaasInput != null) kodeCaasInput.onValueChanged.AddListener(_ => ValidateInputs());
    }

    private void OnPlayPressed()
    {
        if (playPanel != null) playPanel.SetActive(false);
        if (employeeCardPanel != null) employeeCardPanel.SetActive(true);

        // Reset fields each time the panel is opened, in case player replays.
        if (namaInput != null) namaInput.text = "";
        if (kodeCaasInput != null) kodeCaasInput.text = "";
        ValidateInputs();
    }

    private void ValidateInputs()
    {
        bool namaFilled = namaInput != null && !string.IsNullOrWhiteSpace(namaInput.text);
        bool kodeFilled = kodeCaasInput != null && !string.IsNullOrWhiteSpace(kodeCaasInput.text);

        SetNextInteractable(namaFilled && kodeFilled);
    }

    private void SetNextInteractable(bool value)
    {
        if (nextButton != null) nextButton.interactable = value;
    }

    private void OnNextPressed()
    {
        PlayerSession.Nama = namaInput.text.Trim();
        PlayerSession.KodeCaas = kodeCaasInput.text.Trim();

        Debug.Log("===== PLAYER DATA =====");
        Debug.Log("Nama     : " + PlayerSession.Nama);
        Debug.Log("Kode CAAS: " + PlayerSession.KodeCaas);
        Debug.Log("Player ID: " + PlayerSession.PlayerId);

        LoadCutscene();
    }

    private void LoadCutscene()
    {
        if (!string.IsNullOrEmpty(cutsceneSceneName))
        {
            SceneManager.LoadScene(cutsceneSceneName);
        }
        else
        {
            Debug.LogWarning("Cutscene scene name is not set.");
        }
    }
}
