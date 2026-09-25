using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GT.Leaderboard
{
    /// <summary>
    /// Memuat scene tertentu saat tombol diklik. Dipakai untuk tombol "Kembali ke Menu" pada scene Leaderboard.
    /// </summary>
    public class LoadSceneOnClick : MonoBehaviour
    {
        [Header("Navigasi")]
        [Tooltip("Nama scene tujuan yang dimuat saat tombol diklik")]
        [SerializeField] private string sceneName = "MainMenu";

        [Tooltip("Referensi tombol (jika kosong, otomatis diambil dari komponen Button pada GameObject ini)")]
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[LoadSceneOnClick] Scene '{sceneName}' tidak terdaftar di Build Settings.");
            }
        }
    }
}