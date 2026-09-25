using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;

namespace GT.Leaderboard
{
    /// <summary>
    /// Service wrapper untuk Unity Gaming Services - Leaderboards.
    /// Mengirim skor interview ke leaderboard, dan menyediakan query rank, daftar top skor,
    /// serta rentang skor di sekitar player. Menggunakan nama player asli via UpdatePlayerNameAsync.
    ///
    /// Service diakses melalui registry (UnityServices.Instance.GetLeaderboardsService()), bukan
    /// static singleton LeaderboardsService.Instance, karena static tersebut di-reset oleh SDK pada
    /// AfterSceneLoad di Editor play mode dan bisa berujung "service has not been initialized".
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        /// <summary>
        /// Pesan error diagnostik terakhir terkait ketersediaan layanan, untuk ditampilkan ke UI.
        /// </summary>
        public static string LastError { get; private set; }

        [Header("Konfigurasi Leaderboard")]
        [Tooltip("ID leaderboard yang dibuat di Unity Dashboard (Services > Leaderboards)")]
        [SerializeField] private string leaderboardId = "interview-leaderboard";

        [Tooltip("Set nama tampilan player dari PlayerSession.Nama sebelum submit. Jika gagal, submit tetap dilanjutkan.")]
        [SerializeField] private bool updatePlayerNameFromSession = true;

        public string LeaderboardId => leaderboardId;

        private ILeaderboardsService _leaderboards;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Memastikan boot Unity Services selesai dan service Leaderboards tersedia via registry.
        /// Return null (dengan LastError diagnostik) bila layanan tidak terinisialisasi.
        /// </summary>
        private async Task<ILeaderboardsService> EnsureServiceAsync()
        {
            if (_leaderboards != null)
            {
                LastError = null;
                return _leaderboards;
            }

            await UnityServicesInitializer.EnsureServicesAsync();

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (UnityServices.Instance == null)
                    {
                        await Task.Delay(250);
                        continue;
                    }

                    ILeaderboardsService service = UnityServices.Instance.GetLeaderboardsService();
                    if (service != null)
                    {
                        _leaderboards = service;
                        LastError = null;
                        return service;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LeaderboardManager] Cek leaderboards attempt {attempt + 1}/5 gagal: {e.Message}");
                }

                await Task.Delay(250);
            }

            LastError = "Service Leaderboards belum terinisialisasi. Pastikan paket 'com.unity.services.leaderboards' terpasang dan service Leaderboards aktif di Window > General > Services / Dashboard.";
            Debug.LogError("[LeaderboardManager] " + LastError);
            return null;
        }

        /// <summary>
        /// Mengirim/memperbarui skor player ke leaderboard. Return null jika gagal (tidak menghentikan alur game).
        /// </summary>
        public async Task<LeaderboardEntry> SubmitScoreAsync(double score)
        {
            try
            {
                ILeaderboardsService service = await EnsureServiceAsync();
                if (service == null) return null;

                if (updatePlayerNameFromSession && !string.IsNullOrWhiteSpace(PlayerSession.Nama))
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(PlayerSession.Nama);
                    Debug.Log($"[LeaderboardManager] Nama player diperbarui: {PlayerSession.Nama}");
                }

                LeaderboardEntry entry = await service.AddPlayerScoreAsync(leaderboardId, score);
                Debug.Log($"[LeaderboardManager] Skor {score} berhasil dikirim ke leaderboard '{leaderboardId}' (Rank: {entry.Rank}).");
                return entry;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Gagal submit skor: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ambil entri (rank & skor) player saat ini. Return null jika belum ada entri atau terjadi error.
        /// </summary>
        public async Task<LeaderboardEntry> GetPlayerRankAsync()
        {
            try
            {
                ILeaderboardsService service = await EnsureServiceAsync();
                if (service == null) return null;

                return await service.GetPlayerScoreAsync(leaderboardId);
            }
            catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound || e.Reason == LeaderboardsExceptionReason.LeaderboardNotFound)
            {
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Gagal ambil rank player: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ambil daftar skor teratas (default ascending posisi terbaik). Return null jika terjadi error.
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetTopScoresAsync(int limit)
        {
            try
            {
                ILeaderboardsService service = await EnsureServiceAsync();
                if (service == null) return null;

                LeaderboardScoresPage page = await service.GetScoresAsync(leaderboardId,
                    new GetScoresOptions { Offset = 0, Limit = Math.Max(1, limit) });
                return page.Results ?? new List<LeaderboardEntry>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Gagal ambil top scores: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ambil entri player beserta tetangga di sekitarnya untuk tampilan "di sekitar peringkat saya".
        /// Return list kosong jika belum ada entri atau terjadi error.
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetPlayerRangeAsync(int rangeLimit)
        {
            try
            {
                ILeaderboardsService service = await EnsureServiceAsync();
                if (service == null) return new List<LeaderboardEntry>();

                LeaderboardScores scores = await service.GetPlayerRangeAsync(leaderboardId,
                    new GetPlayerRangeOptions { RangeLimit = Math.Max(1, rangeLimit) });
                return scores.Results ?? new List<LeaderboardEntry>();
            }
            catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound || e.Reason == LeaderboardsExceptionReason.LeaderboardNotFound)
            {
                return new List<LeaderboardEntry>();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Gagal ambil range player: {e.Message}");
                return new List<LeaderboardEntry>();
            }
        }
    }
}