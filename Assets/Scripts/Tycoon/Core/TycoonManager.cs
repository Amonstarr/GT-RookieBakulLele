using System;
using System.Collections.Generic;
using UnityEngine;
using Tycoon.Data;
using Tycoon.Economy;

namespace Tycoon.Core
{
    /// <summary>
    /// Core manager for the Tycoon office upgrade system.
    /// Tracks item levels, coordinates coin spending, fires upgrade events, and saves progress.
    /// </summary>
    public class TycoonManager : MonoBehaviour
    {
        public static TycoonManager Instance { get; private set; }

        [Header("Item Registry")]
        [SerializeField] private List<OfficeItemSO> availableItems = new List<OfficeItemSO>();

        [Header("Save Options")]
        [SerializeField] private bool autoSaveOnChange = true;
        private const string SAVE_KEY = "Tycoon_SaveData";

        // Dictionary to track runtime item levels: itemId -> currentLevel (0 = locked/unowned)
        private Dictionary<string, int> itemLevels = new Dictionary<string, int>();

        /// <summary>
        /// Action invoked when an item is upgraded. Parameters: (OfficeItemSO item, int newLevel).
        /// </summary>
        public event Action<OfficeItemSO, int> OnItemUpgraded;

        public IReadOnlyList<OfficeItemSO> AvailableItems => availableItems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadProgress();
        }

        /// <summary>
        /// Returns the current level of an office item (0 if locked/not upgraded yet).
        /// </summary>
        public int GetItemLevel(string itemId)
        {
            if (itemLevels.TryGetValue(itemId, out int level))
            {
                return level;
            }
            return 0;
        }

        /// <summary>
        /// Overload using OfficeItemSO.
        /// Accounts for item.defaultLevel (e.g. Wall & Floor Level 1 active on play).
        /// </summary>
        public int GetItemLevel(OfficeItemSO item)
        {
            if (item == null) return 0;
            int current = GetItemLevel(item.itemId);
            return Mathf.Max(current, item.defaultLevel);
        }

        /// <summary>
        /// Attempts to purchase the next upgrade for the specified item.
        /// Returns true if successful.
        /// </summary>
        public bool TryUpgradeItem(OfficeItemSO item)
        {
            if (item == null) return false;

            int currentLvl = GetItemLevel(item);
            if (currentLvl >= item.MaxLevel)
            {
                Debug.LogWarning($"[TycoonManager] Item '{item.itemName}' is already at Max Level ({item.MaxLevel}).");
                return false;
            }

            int cost = item.GetUpgradeCost(currentLvl);
            if (cost < 0) return false;

            if (TycoonCurrencyManager.Instance != null && !TycoonCurrencyManager.Instance.HasEnoughCoins(cost))
            {
                Debug.LogWarning($"[TycoonManager] Insufficient coins to upgrade '{item.itemName}'. Cost: {cost}");
                return false;
            }

            // Deduct coins
            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.SpendCoins(cost);
            }

            // Upgrade level
            int newLevel = currentLvl + 1;
            itemLevels[item.itemId] = newLevel;

            Debug.Log($"[TycoonManager] Successfully upgraded '{item.itemName}' to Level {newLevel}!");

            OnItemUpgraded?.Invoke(item, newLevel);

            if (autoSaveOnChange)
            {
                SaveProgress();
            }

            return true;
        }

        #region Save & Load System

        public void SaveProgress()
        {
            TycoonSaveData data = new TycoonSaveData();
            if (TycoonCurrencyManager.Instance != null)
            {
                data.coins = TycoonCurrencyManager.Instance.CurrentCoins;
            }

            foreach (var kvp in itemLevels)
            {
                data.itemLevels.Add(new ItemLevelEntry { itemId = kvp.Key, level = kvp.Value });
            }

            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[TycoonManager] Progress saved.");
        }

        public void LoadProgress()
        {
            itemLevels.Clear();

            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

            string json = PlayerPrefs.GetString(SAVE_KEY);
            if (string.IsNullOrEmpty(json)) return;

            TycoonSaveData data = JsonUtility.FromJson<TycoonSaveData>(json);
            if (data == null) return;

            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.SetCoins(data.coins);
            }

            foreach (var entry in data.itemLevels)
            {
                itemLevels[entry.itemId] = entry.level;
            }

            Debug.Log("[TycoonManager] Progress loaded.");
        }

        #endregion

        #region Inspector Debug Context Menu Commands

        [ContextMenu("Debug: Reset All Upgrades to Level 0")]
        public void DebugResetAllUpgrades()
        {
            foreach (var item in availableItems)
            {
                if (item != null)
                {
                    itemLevels[item.itemId] = 0;
                    OnItemUpgraded?.Invoke(item, 0);
                }
            }
            SaveProgress();
            Debug.Log("[TycoonManager] All upgrades reset to Level 0.");
        }

        [ContextMenu("Debug: Upgrade All Items +1 Level")]
        public void DebugUpgradeAllItemsOneLevel()
        {
            foreach (var item in availableItems)
            {
                if (item != null)
                {
                    int currentLvl = GetItemLevel(item);
                    if (currentLvl < item.MaxLevel)
                    {
                        int newLvl = currentLvl + 1;
                        itemLevels[item.itemId] = newLvl;
                        OnItemUpgraded?.Invoke(item, newLvl);
                    }
                }
            }
            SaveProgress();
            Debug.Log("[TycoonManager] All items upgraded by +1 Level.");
        }

        [ContextMenu("Debug: Clear Save Data Key")]
        public void DebugClearSaveKey()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[TycoonManager] Save key cleared from PlayerPrefs.");
        }

        #endregion
    }
}
