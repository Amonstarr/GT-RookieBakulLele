using System;
using System.Collections.Generic;
using UnityEngine;
using Tycoon.Data;
using Tycoon.Visual;

namespace Tycoon.Core
{
    /// <summary>
    /// Core manager in the Tycoon scene for registering, hiring, and spawning autonomous worker characters.
    /// Spawns WorkerCharacterAI prefabs for all employees hired via Interview.
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [Header("Character Registry")]
        [SerializeField] private List<CharacterSO> characterRegistry = new List<CharacterSO>();

        [Header("Worker Spawning Setup")]
        [SerializeField] private GameObject workerPrefab;
        [SerializeField] private Transform workerSpawnParent;
        [SerializeField] private bool autoSpawnOnStart = true;

        private List<CharacterSO> hiredCharacters = new List<CharacterSO>();
        private List<string> activeCharacterIds = new List<string>();
        private List<WorkerCharacterAI> activeWorkerInstances = new List<WorkerCharacterAI>();

        public event Action<CharacterSO> OnCharacterHired;
        public event Action OnActiveCharactersChanged;

        public IReadOnlyList<CharacterSO> HiredCharacters => hiredCharacters;
        public IReadOnlyList<WorkerCharacterAI> ActiveWorkerInstances => activeWorkerInstances;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            LoadHiredCharacters();

            if (autoSpawnOnStart)
            {
                SpawnHiredWorkers();
            }
        }

        /// <summary>
        /// Reads hired character IDs from persistent save data and populates hiredCharacters list.
        /// </summary>
        public void LoadHiredCharacters()
        {
            hiredCharacters.Clear();

            List<string> hiredIds = InterviewRecruitmentBridge.GetHiredCandidateIds();
            foreach (var id in hiredIds)
            {
                CharacterSO charSO = GetCharacterById(id);
                if (charSO != null && !hiredCharacters.Contains(charSO))
                {
                    hiredCharacters.Add(charSO);
                }
            }

            // Load active IDs from save data
            activeCharacterIds.Clear();
            if (PlayerPrefs.HasKey("Tycoon_SaveData"))
            {
                string json = PlayerPrefs.GetString("Tycoon_SaveData");
                if (!string.IsNullOrEmpty(json))
                {
                    TycoonSaveData data = JsonUtility.FromJson<TycoonSaveData>(json);
                    if (data != null && data.activeCharacterIds != null)
                    {
                        activeCharacterIds = new List<string>(data.activeCharacterIds);
                    }
                }
            }

            // If no active list is saved yet, default all hired characters to active
            if (activeCharacterIds.Count == 0 && hiredCharacters.Count > 0)
            {
                foreach (var c in hiredCharacters)
                {
                    if (c != null) activeCharacterIds.Add(c.characterId);
                }
            }

            Debug.Log($"[CharacterManager] Loaded {hiredCharacters.Count} hired workers ({activeCharacterIds.Count} active) into Tycoon scene.");
        }

        public bool IsCharacterActive(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            return activeCharacterIds.Contains(characterId);
        }

        public bool IsCharacterActive(CharacterSO character)
        {
            if (character == null) return false;
            return IsCharacterActive(character.characterId);
        }

        /// <summary>
        /// Toggles a hired character between active (deployed in office) and inactive (resting off-duty).
        /// </summary>
        public void ToggleCharacterActive(CharacterSO character)
        {
            if (character == null || !hiredCharacters.Contains(character)) return;

            if (activeCharacterIds.Contains(character.characterId))
            {
                activeCharacterIds.Remove(character.characterId);
            }
            else
            {
                activeCharacterIds.Add(character.characterId);
            }

            SaveActiveCharacters();
            SpawnHiredWorkers();
            OnActiveCharactersChanged?.Invoke();
        }

        private void SaveActiveCharacters()
        {
            if (!PlayerPrefs.HasKey("Tycoon_SaveData")) return;
            string json = PlayerPrefs.GetString("Tycoon_SaveData");
            if (string.IsNullOrEmpty(json)) return;

            TycoonSaveData data = JsonUtility.FromJson<TycoonSaveData>(json);
            if (data != null)
            {
                data.activeCharacterIds = new List<string>(activeCharacterIds);
                string newJson = JsonUtility.ToJson(data, true);
                PlayerPrefs.SetString("Tycoon_SaveData", newJson);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Hires a character at runtime (e.g. from shop or debug) and saves to persistent storage.
        /// </summary>
        public bool HireCharacter(CharacterSO character)
        {
            if (character == null) return false;

            if (!hiredCharacters.Contains(character))
            {
                hiredCharacters.Add(character);
                if (!activeCharacterIds.Contains(character.characterId))
                {
                    activeCharacterIds.Add(character.characterId);
                }

                InterviewRecruitmentBridge.HireCandidate(character.characterId);
                SaveActiveCharacters();
                OnCharacterHired?.Invoke(character);

                SpawnWorkerVisual(character);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Spawns 2D autonomous worker AI instances for active characters in the office.
        /// </summary>
        public void SpawnHiredWorkers()
        {
            // Clear existing worker instances
            foreach (var worker in activeWorkerInstances)
            {
                if (worker != null) Destroy(worker.gameObject);
            }
            activeWorkerInstances.Clear();

            foreach (var character in hiredCharacters)
            {
                if (character != null && IsCharacterActive(character))
                {
                    SpawnWorkerVisual(character);
                }
            }
        }

        private void SpawnWorkerVisual(CharacterSO character)
        {
            if (character == null) return;

            Vector3 spawnPos = Vector3.zero;
            if (OfficeWaypointGroup.Instance != null)
            {
                Transform wp = OfficeWaypointGroup.Instance.GetRandomPacingWaypoint();
                if (wp != null) spawnPos = wp.position;
            }

            GameObject obj = null;
            if (workerPrefab != null)
            {
                obj = Instantiate(workerPrefab, spawnPos, Quaternion.identity, workerSpawnParent != null ? workerSpawnParent : transform);
            }
            else
            {
                // Fallback default GameObject creation with required components
                obj = new GameObject($"Worker_{character.characterName}");
                obj.transform.position = spawnPos;
                if (workerSpawnParent != null) obj.transform.SetParent(workerSpawnParent);
                obj.AddComponent<SpriteRenderer>();
                obj.AddComponent<WorkerCharacterAI>();
            }

            WorkerCharacterAI workerAI = obj.GetComponent<WorkerCharacterAI>();
            if (workerAI != null)
            {
                workerAI.SetupCharacter(character);
                activeWorkerInstances.Add(workerAI);
            }
        }

        public CharacterSO GetCharacterById(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            return characterRegistry.Find(c => c != null && c.characterId == characterId);
        }
    }
}
