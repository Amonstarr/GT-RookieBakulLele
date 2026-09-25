using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Leaderboards.Models;

namespace GT.Leaderboard
{
    /// <summary>
    /// Controller tampilan papan peringkat yang siap ditaruh di scene Credit.
    /// Menampilkan deretan skor teratas + peringkat player sendiri, dengan state loading/error/retry.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("Pencarian Data")]
        [SerializeField] private LeaderboardManager leaderboardManager;
        [Tooltip("Jumlah entri teratas yang ditampilkan")]
        [SerializeField] private int topEntriesToShow = 10;

        [Header("Daftar Skor (ScrollView)")]
        [Tooltip("Parent / Content dari ScrollRect tempat baris di-instantiate")]
        [SerializeField] private Transform rowsParent;
        [Tooltip("Prefab baris yang memiliki komponen LeaderboardRowUI")]
        [SerializeField] private GameObject rowPrefab;

        [Header("Teks Peringkat Player")]
        [SerializeField] private TMP_Text playerRankText;
        [SerializeField] private TMP_Text playerScoreText;

        [Header("State UI")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button retryButton;

        [Header("Tampilan Baris Player")]
        [SerializeField] private Color playerRowColor = new Color(1f, 0.83f, 0.3f, 1f);

        private void Start()
        {
            if (leaderboardManager == null)
            {
                leaderboardManager = FindFirstObjectByType<LeaderboardManager>();
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(LoadLeaderboard);
            }

            LoadLeaderboard();
        }

        /// <summary>
        /// Memuat ulang papan peringkat (dipanggil otomatis saat Start dan lewat tombol Retry).
        /// </summary>
        public void LoadLeaderboard()
        {
            _ = LoadLeaderboardAsync();
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
            }
        }

        private async Task LoadLeaderboardAsync()
        {
            SetLoadingState(true);
            SetFeedback(null, false);

            if (leaderboardManager == null)
            {
                SetLoadingState(false);
                SetFeedback("LeaderboardManager belum terpasang di scene ini!", true);
                return;
            }

            // Muat deretan teratas dan entri player secara paralel
            Task<List<LeaderboardEntry>> topTask = leaderboardManager.GetTopScoresAsync(topEntriesToShow);
            Task<LeaderboardEntry> selfTask = leaderboardManager.GetPlayerRankAsync();
            await Task.WhenAll(topTask, selfTask);

            List<LeaderboardEntry> topEntries = topTask.Result;
            LeaderboardEntry selfEntry = selfTask.Result;

            SetLoadingState(false);

            if (topEntries == null)
            {
                SetFeedback("Gagal memuat papan peringkat. Periksa koneksi internet lalu coba lagi.", true);
                return;
            }

            PopulateTopRows(topEntries, selfEntry?.Rank ?? 0);
            UpdatePlayerSummary(selfEntry);

            // Sembunyikan tombol retry setelah berhasil memuat
            if (retryButton != null) retryButton.gameObject.SetActive(false);
        }

        private void PopulateTopRows(List<LeaderboardEntry> entries, int selfRank)
        {
            if (rowPrefab == null || rowsParent == null)
            {
                SetFeedback("rowPrefab / rowsParent belum di-set di Inspector!", true);
                return;
            }

            // Bersihkan baris lama
            for (int i = rowsParent.childCount - 1; i >= 0; i--)
            {
                Destroy(rowsParent.GetChild(i).gameObject);
            }

            if (entries.Count == 0)
            {
                SetFeedback("Belum ada pemain di papan peringkat ini.", false);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry entry = entries[i];
                if (entry == null) continue;

                GameObject rowGO = Instantiate(rowPrefab, rowsParent);
                rowGO.SetActive(true);

                LeaderboardRowUI row = rowGO.GetComponent<LeaderboardRowUI>();
                if (row != null)
                {
                    bool isPlayer = selfRank > 0 && entry.Rank == selfRank;
                    row.SetEntry(entry, isPlayer, playerRowColor, i);
                }
            }
        }

        private void UpdatePlayerSummary(LeaderboardEntry selfEntry)
        {
            if (selfEntry == null)
            {
                if (playerRankText != null) playerRankText.text = "Belum ada skor";
                if (playerScoreText != null) playerScoreText.text = "Selesaikan interview untuk masuk papan peringkat!";
                return;
            }

            if (playerRankText != null)
            {
                playerRankText.text = $"Peringkat kamu: <b>#{selfEntry.Rank}</b>";
            }

            if (playerScoreText != null)
            {
                playerScoreText.text = $"Skor: <b>{Mathf.RoundToInt((float)selfEntry.Score)}</b> poin";
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(isLoading);
            if (retryButton != null) retryButton.gameObject.SetActive(!isLoading);
        }

        private void SetFeedback(string message, bool isError)
        {
            if (feedbackText == null) return;
            feedbackText.gameObject.SetActive(!string.IsNullOrEmpty(message));
            feedbackText.text = message;
            feedbackText.color = isError ? Color.red : Color.white;
        }
    }

    /// <summary>
    /// Satu baris papan peringkat. Pasang di prefab baris dengan 3 TMP_Text
    /// (RankText, NameText, ScoreText) dan optional Image background.
    /// </summary>
    public class LeaderboardRowUI : MonoBehaviour
    {
        [Header("Referensi Teks Baris")]
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image backgroundImage;

        public void SetEntry(LeaderboardEntry entry, bool isPlayer, Color playerColor, int rowIndex = 0)
        {
            if (rankText != null) rankText.text = $"#{entry.Rank}";
            if (nameText != null) nameText.text = string.IsNullOrEmpty(entry.PlayerName) ? "Player" : entry.PlayerName;
            if (scoreText != null) scoreText.text = Mathf.RoundToInt((float)entry.Score).ToString();

            if (backgroundImage != null)
            {
                backgroundImage.color = isPlayer ? playerColor : (rowIndex % 2 == 0 ? Color.white * 0.9f : Color.white * 0.8f);
            }

            if (isPlayer && nameText != null)
            {
                nameText.text = $"<color=#FFD700>{nameText.text}</color>  (Kamu)";
            }
        }
    }
}