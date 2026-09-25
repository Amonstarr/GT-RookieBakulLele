using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Tycoon.Core;
using Tycoon.Data;

namespace Tycoon.Visual
{
    /// <summary>
    /// Component that connects an OfficeItemSO (Wall or Floor) to scene Tilemaps under Grid.
    /// Automatically updates tiles on 'Wall Samping', 'Wall', 'Floor', and 'WallBawah' Tilemaps upon item upgrade or Start.
    /// </summary>
    public class OfficeTilemapVisual : MonoBehaviour
    {
        [Header("Target Item Reference")]
        [SerializeField] private OfficeItemSO targetItem;

        [Header("Scene Tilemap References (Auto-Find by Name if Null)")]
        [Tooltip("Tilemap for 'Wall Samping' under Grid")]
        [SerializeField] private Tilemap wallSampingTilemap;

        [Tooltip("Tilemap for 'Wall' under Grid")]
        [SerializeField] private Tilemap wallTilemap;

        [Tooltip("Tilemap for 'Floor' under Grid")]
        [SerializeField] private Tilemap floorTilemap;

        [Tooltip("Tilemap for 'WallBawah' under Grid")]
        [SerializeField] private Tilemap wallBawahTilemap;

        [Header("Options")]
        [Tooltip("If true, auto-replaces all existing tiles within Tilemap cell bounds when level changes.")]
        [SerializeField] private bool autoFillExistingTiles = true;

        public OfficeItemSO TargetItem => targetItem;

        private void Awake()
        {
            AutoFindTilemaps();
        }

        private void Start()
        {
            AutoFindTilemaps();

            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded += HandleItemUpgraded;
                int currentLvl = TycoonManager.Instance.GetItemLevel(targetItem);
                RefreshTilemaps(currentLvl);
            }
            else
            {
                int defaultLvl = targetItem != null ? targetItem.defaultLevel : 0;
                RefreshTilemaps(defaultLvl);
            }
        }

        private void OnDestroy()
        {
            if (TycoonManager.Instance != null)
            {
                TycoonManager.Instance.OnItemUpgraded -= HandleItemUpgraded;
            }
        }

        public void AutoFindTilemaps()
        {
            Grid grid = FindObjectOfType<Grid>();
            if (grid == null) return;

            if (wallSampingTilemap == null)
            {
                Transform t = grid.transform.Find("Wall Samping");
                if (t != null) wallSampingTilemap = t.GetComponent<Tilemap>();
            }

            if (wallTilemap == null)
            {
                Transform t = grid.transform.Find("Wall");
                if (t != null) wallTilemap = t.GetComponent<Tilemap>();
            }

            if (floorTilemap == null)
            {
                Transform t = grid.transform.Find("Floor");
                if (t != null) floorTilemap = t.GetComponent<Tilemap>();
            }

            if (wallBawahTilemap == null)
            {
                Transform t = grid.transform.Find("WallBawah");
                if (t != null) wallBawahTilemap = t.GetComponent<Tilemap>();
            }
        }

        private void HandleItemUpgraded(OfficeItemSO item, int newLevel)
        {
            if (targetItem != null && item != null && item.itemId == targetItem.itemId)
            {
                RefreshTilemaps(newLevel);
            }
        }

        /// <summary>
        /// Updates scene tilemaps according to the specified item level.
        /// </summary>
        public void RefreshTilemaps(int level)
        {
            if (targetItem == null || level <= 0) return;

            AutoFindTilemaps();

            OfficeItemLevelData levelData = targetItem.GetLevelData(level);

            // 1. Wall Category Tile Updates (Wall Samping, Wall, WallBawah)
            if (targetItem.category == OfficeItemCategory.Wall ||
                levelData.wallSampingTile != null || levelData.wallTile != null || levelData.wallBawahTile != null)
            {
                if (levelData.wallSampingTile != null)
                {
                    ApplyTileToTilemap(wallSampingTilemap, levelData.wallSampingTile);
                }

                if (levelData.wallTile != null)
                {
                    ApplyTileToTilemap(wallTilemap, levelData.wallTile);
                }

                if (levelData.wallBawahTile != null)
                {
                    ApplyTileToTilemap(wallBawahTilemap, levelData.wallBawahTile);
                }
            }

            // 2. Floor Category Tile Updates (Floor)
            if (targetItem.category == OfficeItemCategory.Floor || levelData.floorTile != null)
            {
                if (levelData.floorTile != null)
                {
                    ApplyTileToTilemap(floorTilemap, levelData.floorTile);
                }
            }
        }

        private void ApplyTileToTilemap(Tilemap targetTilemap, TileBase newTile)
        {
            if (targetTilemap == null || newTile == null) return;

            BoundsInt bounds = targetTilemap.cellBounds;
            bool hasExistingTiles = false;

            if (autoFillExistingTiles && bounds.size.x > 0 && bounds.size.y > 0)
            {
                foreach (Vector3Int pos in bounds.allPositionsWithin)
                {
                    if (targetTilemap.HasTile(pos))
                    {
                        hasExistingTiles = true;
                        targetTilemap.SetTile(pos, newTile);
                    }
                }
            }

            // Fallback: If target tilemap is empty, fill area using reference tilemap bounds (e.g. wallTilemap)
            if (!hasExistingTiles)
            {
                BoundsInt refBounds = GetReferenceRoomBounds();
                if (refBounds.size.x > 0 && refBounds.size.y > 0)
                {
                    foreach (Vector3Int pos in refBounds.allPositionsWithin)
                    {
                        targetTilemap.SetTile(pos, newTile);
                    }
                }
            }

            targetTilemap.RefreshAllTiles();
        }

        private BoundsInt GetReferenceRoomBounds()
        {
            if (wallTilemap != null && wallTilemap.cellBounds.size.x > 0)
                return wallTilemap.cellBounds;
            if (wallSampingTilemap != null && wallSampingTilemap.cellBounds.size.x > 0)
                return wallSampingTilemap.cellBounds;
            if (wallBawahTilemap != null && wallBawahTilemap.cellBounds.size.x > 0)
                return wallBawahTilemap.cellBounds;

            return new BoundsInt();
        }

        private void OnValidate()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                AutoFindTilemaps();
            }
        }
    }
}
