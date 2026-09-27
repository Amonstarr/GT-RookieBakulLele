using UnityEngine;
using UnityEngine.UI;

namespace Tycoon.UI
{
    public class TycoonTutorialUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;

        private void Start()
        {
            // Pause the game when the tutorial panel is active
            Time.timeScale = 0f;

            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            else
            {
                Debug.LogWarning("[TycoonTutorialUI] Start Button is not assigned!");
            }
        }

        private void OnStartButtonClicked()
        {
            // Resume the game
            Time.timeScale = 1f;

            // Hide the tutorial panel
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            }
        }
    }
}
