using System;
using UnityEngine;

namespace Tycoon.Core
{
    /// <summary>
    /// Manages game session playtime timer (default 3 minutes real-time)
    /// and maps it linearly to simulated workday hours (09:00 AM to 05:00 PM).
    /// </summary>
    public class GameTimerManager : MonoBehaviour
    {
        public static GameTimerManager Instance { get; private set; }

        [Header("Timer Configuration")]
        [Tooltip("Real-world session duration in seconds (180s = 3 minutes).")]
        [SerializeField] private float playTimeDurationSeconds = 180f;

        [Header("Workday Hours")]
        [Tooltip("Start hour of the workday in 24-hour format (e.g. 9 = 09:00 AM).")]
        [SerializeField] private int startHour = 9;

        [Tooltip("End hour of the workday in 24-hour format (e.g. 17 = 05:00 PM).")]
        [SerializeField] private int endHour = 17;

        [Header("Options")]
        [SerializeField] private bool autoStartOnStart = true;

        private float elapsedTime = 0f;
        private bool isRunning = false;
        private bool isFinished = false;

        /// <summary>
        /// Fires every frame with (float normalizedProgress [0..1], string formattedTimeString "09:00 AM").
        /// </summary>
        public event Action<float, string> OnTimeUpdated;

        /// <summary>
        /// Fires when the workday session starts.
        /// </summary>
        public event Action OnWorkdayStarted;

        /// <summary>
        /// Fires when the workday session ends (180s reached / 05:00 PM).
        /// </summary>
        public event Action OnWorkdayEnded;

        public float ElapsedTime => elapsedTime;
        public float PlayTimeDuration => playTimeDurationSeconds;
        public float NormalizedProgress => playTimeDurationSeconds > 0f ? Mathf.Clamp01(elapsedTime / playTimeDurationSeconds) : 1f;
        public bool IsRunning => isRunning;
        public bool IsFinished => isFinished;
        public int StartHour => startHour;
        public int EndHour => endHour;

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
            if (autoStartOnStart)
            {
                StartTimer();
            }
        }

        private void Update()
        {
            if (!isRunning || isFinished) return;

            elapsedTime += Time.deltaTime;
            if (elapsedTime >= playTimeDurationSeconds)
            {
                elapsedTime = playTimeDurationSeconds;
                isFinished = true;
                isRunning = false;

                OnTimeUpdated?.Invoke(1f, GetFormattedTimeString(1f));
                OnWorkdayEnded?.Invoke();
                Debug.Log("[GameTimerManager] Workday completed! Reached 05:00 PM (180s).");
            }
            else
            {
                float progress = NormalizedProgress;
                OnTimeUpdated?.Invoke(progress, GetFormattedTimeString(progress));
            }
        }

        public void StartTimer()
        {
            elapsedTime = 0f;
            isFinished = false;
            isRunning = true;
            OnWorkdayStarted?.Invoke();
            OnTimeUpdated?.Invoke(0f, GetFormattedTimeString(0f));
            Debug.Log("[GameTimerManager] Workday timer started (09:00 AM).");
        }

        public void PauseTimer()
        {
            isRunning = false;
        }

        public void ResumeTimer()
        {
            if (!isFinished)
            {
                isRunning = true;
            }
        }

        public void ResetTimer()
        {
            elapsedTime = 0f;
            isFinished = false;
            isRunning = false;
            OnTimeUpdated?.Invoke(0f, GetFormattedTimeString(0f));
        }

        /// <summary>
        /// Returns simulated formatted time string (e.g. "09:00 AM", "12:30 PM", "05:00 PM") based on progress [0..1].
        /// </summary>
        public string GetFormattedTimeString(float progress)
        {
            progress = Mathf.Clamp01(progress);
            float totalSimulatedMinutes = (endHour - startHour) * 60f;
            float currentSimulatedMinutes = (startHour * 60f) + (progress * totalSimulatedMinutes);

            int hours = Mathf.FloorToInt(currentSimulatedMinutes / 60f);
            int minutes = Mathf.FloorToInt(currentSimulatedMinutes % 60f);

            string ampm = hours >= 12 ? "PM" : "AM";
            int displayHour = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);

            return $"{displayHour:D2}:{minutes:D2} {ampm}";
        }
    }
}
