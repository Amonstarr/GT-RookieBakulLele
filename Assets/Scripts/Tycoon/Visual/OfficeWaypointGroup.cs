using System.Collections.Generic;
using UnityEngine;

namespace Tycoon.Visual
{
    /// <summary>
    /// Holds reference points in the office scene for Worker AI movement:
    /// pacing waypoints, chair seating targets, and coffee break spots.
    /// </summary>
    public class OfficeWaypointGroup : MonoBehaviour
    {
        public static OfficeWaypointGroup Instance { get; private set; }

        [Header("Pacing & Wandering Targets")]
        [SerializeField] private List<Transform> pacingWaypoints = new List<Transform>();

        [Header("Seating Targets (Desks / Chairs)")]
        [SerializeField] private List<Transform> chairTargets = new List<Transform>();

        [Header("Breakroom / Coffee Targets")]
        [SerializeField] private Transform coffeeTarget;

        private HashSet<Transform> occupiedChairs = new HashSet<Transform>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-discover child transforms if empty
            if (pacingWaypoints.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("pacing") || child.name.ToLower().Contains("waypoint"))
                    {
                        pacingWaypoints.Add(child);
                    }
                    else if (child.name.ToLower().Contains("chair") || child.name.ToLower().Contains("seat"))
                    {
                        chairTargets.Add(child);
                    }
                    else if (child.name.ToLower().Contains("coffee") || child.name.ToLower().Contains("break"))
                    {
                        coffeeTarget = child;
                    }
                }
            }
        }

        public Transform GetRandomPacingWaypoint()
        {
            if (pacingWaypoints.Count == 0) return transform;
            int idx = Random.Range(0, pacingWaypoints.Count);
            return pacingWaypoints[idx];
        }

        /// <summary>
        /// Returns the pacing waypoint closest to the given world position.
        /// Used by AI to align with main walking corridors before approaching desks/items.
        /// </summary>
        public Transform GetNearestPacingWaypoint(Vector3 position)
        {
            if (pacingWaypoints == null || pacingWaypoints.Count == 0) return transform;

            Transform nearest = null;
            float minDistance = float.MaxValue;

            foreach (var wp in pacingWaypoints)
            {
                if (wp == null) continue;
                float dist = Vector3.Distance(position, wp.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = wp;
                }
            }

            return nearest != null ? nearest : transform;
        }

        private bool IsItemUnlockedForTarget(Transform target)
        {
            if (target == null) return false;
            OfficeItemVisual itemVisual = target.GetComponent<OfficeItemVisual>();
            if (itemVisual == null) itemVisual = target.GetComponentInParent<OfficeItemVisual>();

            if (itemVisual != null)
            {
                return itemVisual.IsUnlocked;
            }

            return true;
        }

        public Transform ClaimAvailableChair(Transform currentChair)
        {
            if (currentChair != null && occupiedChairs.Contains(currentChair) && IsItemUnlockedForTarget(currentChair))
            {
                return currentChair; // Keep current chair if already owned & unlocked
            }

            List<Transform> freeChairs = new List<Transform>();
            foreach (var chair in chairTargets)
            {
                if (chair != null && !occupiedChairs.Contains(chair) && IsItemUnlockedForTarget(chair))
                {
                    freeChairs.Add(chair);
                }
            }

            if (freeChairs.Count == 0)
            {
                return null; // Return null if no unlocked chairs available
            }

            Transform selected = freeChairs[Random.Range(0, freeChairs.Count)];
            occupiedChairs.Add(selected);
            return selected;
        }

        public void ReleaseChair(Transform chair)
        {
            if (chair != null && occupiedChairs.Contains(chair))
            {
                occupiedChairs.Remove(chair);
            }
        }

        public Transform GetCoffeeTarget()
        {
            if (coffeeTarget != null && IsItemUnlockedForTarget(coffeeTarget)) return coffeeTarget;
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var wp in pacingWaypoints)
            {
                if (wp != null) Gizmos.DrawWireSphere(wp.position, 0.3f);
            }

            Gizmos.color = Color.green;
            foreach (var ch in chairTargets)
            {
                if (ch != null) Gizmos.DrawWireCube(ch.position, new Vector3(0.5f, 0.5f, 0));
            }

            if (coffeeTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(coffeeTarget.position, 0.5f);
            }
        }
    }
}
