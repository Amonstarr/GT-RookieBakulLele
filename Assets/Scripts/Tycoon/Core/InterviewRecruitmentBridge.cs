using System.Collections.Generic;
using UnityEngine;
using Tycoon.Data;

namespace Tycoon.Core
{
    /// <summary>
    /// Static & Scene bridge component for transferring interview recruitment results from DialogueInterview scene
    /// into Tycoon persistent save data.
    /// </summary>
    public class InterviewRecruitmentBridge : MonoBehaviour
    {
        private const string SAVE_KEY = "Tycoon_SaveData";

        /// <summary>
        /// Call this method when a candidate is hired in the DialogueInterview scene.
        /// </summary>
        public static bool HireCandidate(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;

            TycoonSaveData data = LoadSaveData();
            if (data == null)
            {
                data = new TycoonSaveData();
            }

            if (!data.hiredCharacterIds.Contains(characterId))
            {
                data.hiredCharacterIds.Add(characterId);
                SaveData(data);
                Debug.Log($"[InterviewRecruitmentBridge] Candidate '{characterId}' successfully hired into Tycoon save data!");
                return true;
            }

            Debug.LogWarning($"[InterviewRecruitmentBridge] Candidate '{characterId}' is already hired.");
            return false;
        }

        public static bool IsCandidateHired(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return false;
            TycoonSaveData data = LoadSaveData();
            return data != null && data.hiredCharacterIds.Contains(characterId);
        }

        public static List<string> GetHiredCandidateIds()
        {
            TycoonSaveData data = LoadSaveData();
            return data != null ? new List<string>(data.hiredCharacterIds) : new List<string>();
        }

        private static TycoonSaveData LoadSaveData()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return new TycoonSaveData();
            string json = PlayerPrefs.GetString(SAVE_KEY);
            if (string.IsNullOrEmpty(json)) return new TycoonSaveData();
            try
            {
                return JsonUtility.FromJson<TycoonSaveData>(json);
            }
            catch
            {
                return new TycoonSaveData();
            }
        }

        private static void SaveData(TycoonSaveData data)
        {
            if (data == null) return;
            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        #region Inspector Callback Helper
        [Header("Quick Hire Trigger (For Inspector Testing)")]
        [SerializeField] private string debugHireCharacterId = "worker_dev_1";

        [ContextMenu("Debug: Hire Character From Inspector")]
        public void TriggerHireFromInspector()
        {
            HireCandidate(debugHireCharacterId);
        }
        #endregion
    }
}
