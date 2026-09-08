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
        public Sprite topdownSprite;
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

        public int MaxLevel => levels.Count;

        /// <summary>
        /// Gets data for a specific level (1-indexed). Returns default if out of bounds.
        /// Level 0 means locked / not owned yet.
        /// </summary>
        public OfficeItemLevelData GetLevelData(int level)
        {
            if (level <= 0 || level > levels.Count)
            {
                return default;
            }
            return levels[level - 1];
        }

        /// <summary>
        /// Gets upgrade cost to advance from currentLevel to next level.
        /// Returns -1 if already at max level.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            int nextLevel = currentLevel + 1;
            if (nextLevel > levels.Count)
            {
                return -1; // Max level reached
            }
            return levels[nextLevel - 1].upgradeCost;
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
