using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon.Data
{
    public enum OfficeItemCategory
    {
        Desk,
        Chair,
        Computer,
        AirConditioner,
        Decor,
        Breakroom,
        Lighting
    }

    [System.Serializable]
    public struct OfficeItemLevelData
    {
        public int levelIndex;
        public string levelName;
        public int upgradeCost;
        [Tooltip("Single/Main sprite fallback")]
        public Sprite topdownSprite;
        [Tooltip("Sprite layer depan (foreground, rendered in front of sitting worker)")]
        public Sprite frontSprite;
        [Tooltip("Sprite layer belakang (background, rendered behind sitting worker)")]
        public Sprite backSprite;
        [TextArea(2, 4)]
        public string boostDescription;
    }

    [CreateAssetMenu(fileName = "NewOfficeItem", menuName = "Tycoon/Office Item SO")]
    public class OfficeItemSO : ScriptableObject
    {
        [Header("Basic Information")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)]
        public string itemDescription;
        public OfficeItemCategory category;
        public Sprite shopIcon;

        [Header("Upgrade Levels")]
        public List<OfficeItemLevelData> levels = new List<OfficeItemLevelData>();

        [Header("Dynamic Pricing Options (Optional)")]
        [Tooltip("If true, upgrade cost is calculated dynamically using formula: baseCost * (costMultiplier ^ level)")]
        public bool useFormulaCost = false;
        public int baseCost = 100;
        [Range(1.1f, 5.0f)]
        public float costMultiplier = 1.5f;

        public int MaxLevel => levels != null && levels.Count > 0 ? levels.Count : 10;

        /// <summary>
        /// Gets data for a specific level (1-indexed). Returns default if out of bounds.
        /// Level 0 means locked / not owned yet.
        /// </summary>
        public OfficeItemLevelData GetLevelData(int level)
        {
            if (level <= 0) return default;

            if (levels != null && level <= levels.Count)
            {
                return levels[level - 1];
            }

            // Fallback for formula-generated levels
            return new OfficeItemLevelData
            {
                levelIndex = level,
                levelName = $"{itemName} Lvl {level}",
                upgradeCost = CalculateFormulaCost(level - 1),
                boostDescription = $"Boost Lvl {level}"
            };
        }

        /// <summary>
        /// Gets upgrade cost to advance from currentLevel to next level.
        /// Returns -1 if already at max level.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            int nextLevel = currentLevel + 1;
            if (nextLevel > MaxLevel)
            {
                return -1; // Max level reached
            }

            if (useFormulaCost || levels == null || nextLevel > levels.Count)
            {
                return CalculateFormulaCost(currentLevel);
            }

            return levels[nextLevel - 1].upgradeCost;
        }

        public int CalculateFormulaCost(int currentLevel)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name.ToLower().Replace(" ", "_");
            }
            for (int i = 0; i < levels.Count; i++)
            {
                var lvl = levels[i];
                lvl.levelIndex = i + 1;
                levels[i] = lvl;
            }
        }
    }
}
