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
        [Tooltip("SpriteRenderer utama / layer belakang (rendered behind sitting worker)")]
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [Tooltip("Optional SpriteRenderer layer depan (rendered in front of sitting worker)")]
        [SerializeField] private SpriteRenderer frontSpriteRenderer;

        [Header("Alternative: Per-Level GameObjects (2D Topdown)")]
        [Tooltip("Index 0 = Level 0 (Locked/Hidden), Index 1 = Level 1, Index 2 = Level 2, etc.")]
        [SerializeField] private List<GameObject> levelVisualGameObjects = new List<GameObject>();

        public OfficeItemSO TargetItem => targetItem;

        /// <summary>
        /// True if this office item has been purchased (Level > 0).
        /// </summary>
        public bool IsUnlocked
        {
            get
            {
                if (targetItem == null) return false;
                if (TycoonManager.Instance != null)
                {
                    return TycoonManager.Instance.GetItemLevel(targetItem) > 0;
                }
                return false;
            }
        }

        private void Awake()
        {
            if (useSpriteRenderer && targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
                if (targetSpriteRenderer == null)
                {
                    targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }

            // Sembunyikan visual secara instan saat Awake() jika belum dibeli
            RefreshVisual(0);
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
        /// Supports dual-layer (Front & Back) sprites if frontSpriteRenderer is assigned.
        /// </summary>
        public void RefreshVisual(int level)
        {
            if (targetItem == null) return;

            // 1. Update SpriteRenderer if active
            if (useSpriteRenderer)
            {
                if (level <= 0)
                {
                    if (targetSpriteRenderer != null) targetSpriteRenderer.enabled = false;
                    if (frontSpriteRenderer != null) frontSpriteRenderer.enabled = false;
                }
                else
                {
                    OfficeItemLevelData levelData = targetItem.GetLevelData(level);

                    // Layer Belakang / Utama
                    if (targetSpriteRenderer != null)
                    {
                        targetSpriteRenderer.enabled = true;
                        Sprite back = levelData.backSprite != null ? levelData.backSprite : levelData.topdownSprite;
                        if (back != null)
                        {
                            targetSpriteRenderer.sprite = back;
                        }
                    }

                    // Layer Depan (Opsional)
                    if (frontSpriteRenderer != null)
                    {
                        if (levelData.frontSprite != null)
                        {
                            frontSpriteRenderer.enabled = true;
                            frontSpriteRenderer.sprite = levelData.frontSprite;
                        }
                        else
                        {
                            frontSpriteRenderer.enabled = false;
                        }
                    }
                }
            }

            // 2. Toggle Level GameObjects if provided
            if (levelVisualGameObjects != null && levelVisualGameObjects.Count > 0)
            {
                // Cek apakah Index 0 adalah visual khusus Level 0 (Locked/Empty)
                bool indexZeroIsLevelZero = (levelVisualGameObjects.Count > targetItem.MaxLevel) ||
                    (levelVisualGameObjects[0] != null && 
                     (levelVisualGameObjects[0].name.ToLower().Contains("lvl0") || 
                      levelVisualGameObjects[0].name.ToLower().Contains("locked") || 
                      levelVisualGameObjects[0].name.ToLower().Contains("empty")));

                for (int i = 0; i < levelVisualGameObjects.Count; i++)
                {
                    if (levelVisualGameObjects[i] != null)
                    {
                        if (level <= 0)
                        {
                            // Jika level <= 0, matikan semua visual KECUALI jika Index 0 adalah khusus Level 0 (Locked/Empty)
                            levelVisualGameObjects[i].SetActive(indexZeroIsLevelZero && i == 0);
                        }
                        else
                        {
                            // Jika level > 0:
                            // Jika Index 0 = Level 0 -> target index untuk Level L adalah L
                            // Jika Index 0 = Level 1 -> target index untuk Level L adalah L - 1
                            int targetIndex = indexZeroIsLevelZero ? level : (level - 1);
                            levelVisualGameObjects[i].SetActive(i == targetIndex);
                        }
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (useSpriteRenderer && targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
                if (targetSpriteRenderer == null)
                {
                    targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }
        }
    }
}
