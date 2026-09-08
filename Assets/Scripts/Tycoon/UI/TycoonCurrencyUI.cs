using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tycoon.Economy;

namespace Tycoon.UI
{
    /// <summary>
    /// Displays current coin count in the UI. Supports both TextMeshPro and standard UI Text.
    /// </summary>
    public class TycoonCurrencyUI : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TMP_Text tmpCoinsText;
        [SerializeField] private Text uiCoinsText;
        [SerializeField] private string formatString = "{0:N0}";

        private void Start()
        {
            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged += UpdateCoinsText;
                UpdateCoinsText(TycoonCurrencyManager.Instance.CurrentCoins);
            }
        }

        private void OnDestroy()
        {
            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged -= UpdateCoinsText;
            }
        }

        private void UpdateCoinsText(int coins)
        {
            string formattedText = string.Format(formatString, coins);

            if (tmpCoinsText != null)
            {
                tmpCoinsText.text = formattedText;
            }

            if (uiCoinsText != null)
            {
                uiCoinsText.text = formattedText;
            }
        }

        private void OnValidate()
        {
            if (tmpCoinsText == null) tmpCoinsText = GetComponent<TMP_Text>();
            if (uiCoinsText == null) uiCoinsText = GetComponent<Text>();
        }
    }
}
