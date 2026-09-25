using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Tycoon.Core;

namespace Tycoon.UI
{
    /// <summary>
    /// UI Controller for the Game Timer.
    /// Updates digital clock text, visual clock hand rotation, and displays workday completion modal with next scene button.
    /// </summary>
    public class GameTimerUI : MonoBehaviour
    {
        [Header("Digital Display")]
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text statusText;

        [Header("Visual Clock Display")]
        [Tooltip("Transform of the analog clock needle / hand that rotates as time progresses.")]
        [SerializeField] private RectTransform clockHandTransform;

        [Tooltip("Optional radial fill Image (0 to 1) representing day progression.")]
        [SerializeField] private Image clockFillImage;

        [Header("Clock Hand Rotation Options")]
        [Tooltip("Number of 360 degree rotations over the full shift. Default = 1 (1 full circle per 3-min shift). Set to 8 for 1 rotation per simulated hour.")]
        [SerializeField] private float totalHandRotations = 1f;

        [Header("Workday End UI (Optional)")]
        [SerializeField] private GameObject workdayEndPanel;
        [SerializeField] private TMP_Text workdayEndTitleText;
        [SerializeField] private Button nextSceneButton;
        [Tooltip("Name of the scene to load when Next Scene Button is clicked. If empty, loads next scene index in Build Settings.")]
        [SerializeField] private string nextSceneName = "";

        private void Start()
        {
            if (workdayEndPanel != null)
            {
                workdayEndPanel.SetActive(false);
            }

            if (nextSceneButton != null)
            {
                nextSceneButton.onClick.RemoveAllListeners();
                nextSceneButton.onClick.AddListener(OnNextSceneClicked);
            }

            if (GameTimerManager.Instance != null)
            {
                GameTimerManager.Instance.OnTimeUpdated += HandleTimeUpdated;
                GameTimerManager.Instance.OnWorkdayEnded += HandleWorkdayEnded;

                // Initial refresh
                HandleTimeUpdated(
                    GameTimerManager.Instance.NormalizedProgress,
                    GameTimerManager.Instance.GetFormattedTimeString(GameTimerManager.Instance.NormalizedProgress)
                );
            }
        }

        private void OnDestroy()
        {
            if (GameTimerManager.Instance != null)
            {
                GameTimerManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                GameTimerManager.Instance.OnWorkdayEnded -= HandleWorkdayEnded;
            }
        }

        private void HandleTimeUpdated(float progress, string timeFormatted)
        {
            // 1. Digital Clock Text
            if (timeText != null)
            {
                timeText.text = timeFormatted;
            }

            // 2. Status Badge Text
            if (statusText != null && (GameTimerManager.Instance == null || !GameTimerManager.Instance.IsFinished))
            {
                statusText.text = "JAM KERJA";
            }

            // 3. Analog Clock Hand Rotation (Clockwise)
            if (clockHandTransform != null)
            {
                float zRotation = -progress * 360f * totalHandRotations;
                clockHandTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            }

            // 4. Clock Radial Fill Amount
            if (clockFillImage != null)
            {
                clockFillImage.fillAmount = progress;
            }
        }

        private void HandleWorkdayEnded()
        {
            if (statusText != null)
            {
                statusText.text = "SHIFT SELESAI";
            }

            if (workdayEndPanel != null)
            {
                workdayEndPanel.SetActive(true);
            }

            if (workdayEndTitleText != null)
            {
                workdayEndTitleText.text = "Waktu Kerja Selesai! (05:00 PM)";
            }
        }

        private void OnNextSceneClicked()
        {
            if (workdayEndPanel != null)
            {
                workdayEndPanel.SetActive(false);
            }

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"[GameTimerUI] Loading next scene by name: '{nextSceneName}'");
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (nextIndex < SceneManager.sceneCountInBuildSettings)
                {
                    Debug.Log($"[GameTimerUI] Loading next scene by build index: {nextIndex}");
                    SceneManager.LoadScene(nextIndex);
                }
                else
                {
                    Debug.LogWarning("[GameTimerUI] nextSceneName is empty and current scene is the last scene in Build Settings!");
                }
            }
        }
    }
}
