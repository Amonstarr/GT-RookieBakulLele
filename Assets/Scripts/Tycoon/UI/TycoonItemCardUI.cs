using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tycoon.Core;
using Tycoon.Data;
using Tycoon.Economy;

namespace Tycoon.UI
{
    /// <summary>
    /// UI Card representing a single upgradeable item in the Tycoon Shop.
    /// Handles visual states, upgrade button interactivity, and purchase callbacks.
    /// </summary>
    public class TycoonItemCardUI : MonoBehaviour
    {
        [Header("Item Info Displays")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text boostText;
        [SerializeField] private TMP_Text costText;

        [Header("Upgrade Button")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text buttonLabelText;

        private OfficeItemSO currentItem;

        public OfficeItemSO CurrentItem => currentItem;

        public void Setup(OfficeItemSO item)
        {
            currentItem = item;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            }

            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded += HandleItemUpgraded;
            }

            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            }

            RefreshCard();
        }

        private void OnDestroy()
        {
            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
            }

            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
            }
        }

        private void HandleItemUpgraded(OfficeItemSO upgradedItem, int newLevel)
        {
            if (currentItem != null && upgradedItem != null && upgradedItem.itemId == currentItem.itemId)
            {
                RefreshCard();
            }
        }

        private void HandleCoinsChanged(int currentCoins)
        {
            RefreshCard();
        }

        public void RefreshCard()
        {
            if (currentItem == null) return;

            int currentLvl = TycoonManager.Instance != null ? TycoonManager.Instance.GetItemLevel(currentItem) : 0;
            int maxLvl = currentItem.MaxLevel;

            // 1. Basic Info & Next-Level Preview Icon Display
            int targetPreviewLvl = currentLvl >= maxLvl ? maxLvl : currentLvl + 1;
            if (iconImage != null) iconImage.sprite = currentItem.GetShopIconForLevel(targetPreviewLvl);
            if (nameText != null) nameText.text = currentItem.itemName;
            if (descriptionText != null) descriptionText.text = currentItem.itemDescription;

            // 2. Level Display
            if (levelText != null)
            {
                levelText.text = currentLvl == 0 ? "Lvl 0 (Belum Dibeli)" : $"Lvl {currentLvl} / {maxLvl}";
            }

            // 3. Boost Description
            if (boostText != null)
            {
                if (currentLvl > 0)
                {
                    var currentLvlData = currentItem.GetLevelData(currentLvl);
                    boostText.text = currentLvlData.boostDescription;
                }
                else
                {
                    var lvl1Data = currentItem.GetLevelData(1);
                    boostText.text = $"Unlock: {lvl1Data.boostDescription}";
                }
            }

            // 4. Upgrade Cost & Button State
            if (currentLvl >= maxLvl)
            {
                if (costText != null) costText.text = "MAXED";
                if (buttonLabelText != null) buttonLabelText.text = "MAX LEVEL";
                if (upgradeButton != null) upgradeButton.interactable = false;
            }
            else
            {
                int cost = currentItem.GetUpgradeCost(currentLvl);
                bool hasEnoughCoins = TycoonCurrencyManager.Instance != null && TycoonCurrencyManager.Instance.HasEnoughCoins(cost);

                if (costText != null) costText.text = $"{cost:N0} Coins";
                if (buttonLabelText != null) buttonLabelText.text = currentLvl == 0 ? "BUY" : "UPGRADE";
                if (upgradeButton != null) upgradeButton.interactable = hasEnoughCoins;
            }
        }

        private void OnUpgradeButtonClicked()
        {
            if (currentItem == null || TycoonManager.Instance == null) return;
            TycoonManager.Instance.TryUpgradeItem(currentItem);
        }
    }
}
