using System;
using System.Collections.Generic;
using UnityEngine;
using Tycoon.Core;
using Tycoon.Data;

namespace Tycoon.Visual
{
    /// <summary>
    /// Component placed on 2D office objects in the scene.
    /// Updates topdown sprites or enables/disables level GameObjects when an item is upgraded.
    /// </summary>
    public class OfficeItemVisual : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private OfficeItemSO targetItem;

        [Header("2D Visual Display Mode")]
        [SerializeField] private bool useSpriteRenderer = true;
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        [Header("Alternative: Per-Level GameObjects (2D Topdown)")]
        [Tooltip("Index 0 = Level 0 (Locked/Hidden), Index 1 = Level 1, Index 2 = Level 2, etc.")]
        [SerializeField] private List<GameObject> levelVisualGameObjects = new List<GameObject>();

        private void Awake()
        {
            if (useSpriteRenderer && targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded += HandleItemUpgraded;
                // Refresh visual to match current saved/initial level
                RefreshVisual(TycoonManager.Instance.GetItemLevel(targetItem));
            }
            else
            {
                RefreshVisual(0);
            }
        }

        private void OnDestroy()
        {
            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
            }
        }

        private void HandleItemUpgraded(OfficeItemSO item, int newLevel)
        {
            if (targetItem != null && item != null && item.itemId == targetItem.itemId)
            {
                RefreshVisual(newLevel);
            }
        }

        /// <summary>
        /// Updates the 2D topdown visual representation according to the current upgrade level.
        /// </summary>
        public void RefreshVisual(int level)
        {
            if (targetItem == null) return;

            // 1. Update SpriteRenderer if active
            if (useSpriteRenderer && targetSpriteRenderer != null)
            {
                if (level <= 0)
                {
                    targetSpriteRenderer.enabled = false;
                }
                else
                {
                    targetSpriteRenderer.enabled = true;
                    OfficeItemLevelData levelData = targetItem.GetLevelData(level);
                    if (levelData.topdownSprite != null)
                    {
                        targetSpriteRenderer.sprite = levelData.topdownSprite;
                    }
                }
            }

            // 2. Toggle Level GameObjects if provided
            if (levelVisualGameObjects != null && levelVisualGameObjects.Count > 0)
            {
                for (int i = 0; i < levelVisualGameObjects.Count; i++)
                {
                    if (levelVisualGameObjects[i] != null)
                    {
                        levelVisualGameObjects[i].SetActive(i == level);
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (useSpriteRenderer && targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
