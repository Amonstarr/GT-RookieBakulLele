using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using Tycoon.Core;
using Tycoon.Data;

namespace Tycoon.UI
{
    /// <summary>
    /// UI Controller Otomatis untuk Scene Pemilihan Karakter (SelectCharacter).
    /// Mengelola toggle pemilihan karakter, indikator jumlah pilihan (misal: 0/3), dan preview karakter besar di sebelah kanan.
    /// </summary>
    public class TycoonCharacterUI : MonoBehaviour
    {
        [Header("Selection Limit Settings")]
        [Tooltip("Batas jumlah karakter yang bisa dipilih (Misal: 2 sekarang, ubah ke 4 nanti lewat Inspector)")]
        [Range(1, 10)]
        [SerializeField] private int maxSelectableCharacters = 2;

        [Header("Darken Visual Style")]
        [Tooltip("Warna saat maskot belum dipilih atau di-unselect (normal)")]
        [SerializeField] private Color normalColor = Color.white;
        [Tooltip("Warna saat maskot sudah dipilih (gelap/dimmed)")]
        [SerializeField] private Color selectedDarkColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        [Header("Confirm Button Setup")]
        [Tooltip("Tombol 'PILIH KARAKTER' / 'NEXT' untuk konfirmasi & masuk scene Tycoon")]
        [SerializeField] private Button confirmButton;

        [Header("Selection Indicator & Preview UI")]
        [Tooltip("Teks indikator berapa karakter terpilih (contoh: 0/3)")]
        [SerializeField] private TMP_Text selectionCountText;
        [Tooltip("Fallback UI Text biasa untuk indikator jumlah jika tidak memakai TMPro")]
        [SerializeField] private Text selectionCountLegacyText;

        [Tooltip("Gambar preview besar karakter di sebelah kanan UI")]
        [SerializeField] private Image characterPreviewImage;
        [Tooltip("Teks nama karakter di area preview (opsional)")]
        [SerializeField] private TMP_Text characterPreviewNameText;
        [SerializeField] private Text characterPreviewNameLegacyText;

        [Header("Scene Navigation")]
        [Tooltip("Nama Scene kantor Tycoon (contoh: TycoonMiniGames)")]
        [SerializeField] private string targetSceneOnSelect = "TycoonMiniGames";

        private List<CharacterSO> selectedCharacters = new List<CharacterSO>();

        private void Start()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmAndLoadScene);
                confirmButton.interactable = false;
            }

            UpdateSelectionUI(null);
        }

        /// <summary>
        /// Dipanggil langsung dari Event OnClick () Button Maskot di Inspector Unity!
        /// Jika karakter belum dipilih: menambahkan karakter & menggelapkan tombol.
        /// Jika karakter sudah dipilih: membatalkan pilihan (unselect) & mengembalikan warna tombol.
        /// </summary>
        public void SetSelectedCharacter(CharacterSO character)
        {
            if (character == null) return;

            // Cek apakah karakter sudah ada di daftar pilihan
            int existingIndex = selectedCharacters.FindIndex(c => c != null && c.characterId == character.characterId);

            if (existingIndex >= 0)
            {
                // UNSELECT: Hapus dari daftar terpilih
                selectedCharacters.RemoveAt(existingIndex);
                Debug.Log($"[TycoonCharacterUI] Membatalkan pilihan '{character.characterName}'. ({selectedCharacters.Count}/{maxSelectableCharacters})");

                // Kembalikan warna ke normal & pastikan interactable tetap true
                ApplyButtonVisual(isSelected: false);
            }
            else
            {
                // Jika sudah mencapai batas maksimal pilihan, abaikan
                if (selectedCharacters.Count >= maxSelectableCharacters)
                {
                    Debug.LogWarning($"[TycoonCharacterUI] Batas maksimal ({maxSelectableCharacters}) karakter sudah tercapai!");
                    return;
                }

                // SELECT: Tambahkan ke daftar terpilih
                selectedCharacters.Add(character);
                Debug.Log($"[TycoonCharacterUI] Memilih '{character.characterName}'. ({selectedCharacters.Count}/{maxSelectableCharacters})");

                // Ubah warna menjadi gelap & pastikan interactable tetap true agar bisa di-unselect nanti
                ApplyButtonVisual(isSelected: true);
            }

            // Perbarui Indikator (0/3) & Preview Karakter di Sebelah Kanan
            UpdateSelectionUI(character);

            // Aktifkan tombol confirm jika minimal 1 karakter terpilih
            if (confirmButton != null)
            {
                confirmButton.interactable = (selectedCharacters.Count > 0);
            }
        }

        /// <summary>
        /// Memperbarui indikator jumlah pilihan (misal 0/3) dan gambar preview karakter besar di sebelah kanan.
        /// </summary>
        private void UpdateSelectionUI(CharacterSO lastClickedCharacter)
        {
            // 1. Indikator Jumlah Pilihan (contoh: 0/3)
            string counterString = $"{selectedCharacters.Count}/{maxSelectableCharacters}";
            if (selectionCountText != null) selectionCountText.text = counterString;
            if (selectionCountLegacyText != null) selectionCountLegacyText.text = counterString;

            // 2. Preview Karakter Sebelah Kanan
            CharacterSO previewTarget = null;
            if (lastClickedCharacter != null && selectedCharacters.Contains(lastClickedCharacter))
            {
                previewTarget = lastClickedCharacter;
            }
            else if (selectedCharacters.Count > 0)
            {
                previewTarget = selectedCharacters[selectedCharacters.Count - 1];
            }

            if (previewTarget != null)
            {
                Sprite previewSprite = previewTarget.avatarIcon != null ? previewTarget.avatarIcon : previewTarget.standingSprite;
                if (characterPreviewImage != null)
                {
                    characterPreviewImage.sprite = previewSprite;
                    characterPreviewImage.enabled = (previewSprite != null);
                }

                if (characterPreviewNameText != null) characterPreviewNameText.text = previewTarget.characterName;
                if (characterPreviewNameLegacyText != null) characterPreviewNameLegacyText.text = previewTarget.characterName;
            }
            else
            {
                // Jika tidak ada karakter terpilih, sembunyikan preview
                if (characterPreviewImage != null)
                {
                    characterPreviewImage.enabled = false;
                }
                if (characterPreviewNameText != null) characterPreviewNameText.text = "";
                if (characterPreviewNameLegacyText != null) characterPreviewNameLegacyText.text = "";
            }
        }

        /// <summary>
        /// Opsional: Dipanggil jika ingin menampilkan preview karakter (misal hover/klik) tanpa langsung memilihnya.
        /// </summary>
        public void ShowCharacterPreview(CharacterSO character)
        {
            if (character != null)
            {
                UpdateSelectionUI(character);
            }
        }

        /// <summary>
        /// Mengubah visual tombol yang diklik di EventSystem (gelap saat dipilih, normal saat unselect).
        /// Tombol tetap interaktif (`interactable = true`) agar pengguna bisa mengeklik kembali untuk unselect.
        /// </summary>
        private void ApplyButtonVisual(bool isSelected)
        {
            GameObject currentObj = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (currentObj != null)
            {
                Button btn = currentObj.GetComponent<Button>();
                if (btn == null) btn = currentObj.GetComponentInParent<Button>();

                if (btn != null)
                {
                    // Pastikan tombol tetap interactable agar bisa diklik lagi (unselect)
                    btn.interactable = true;

                    // Ubah warna sesuai status terpilih
                    Image img = btn.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = isSelected ? selectedDarkColor : normalColor;
                    }
                }
            }
        }

        /// <summary>
        /// Dipanggil saat tombol Confirm ("PILIH KARAKTER") diklik.
        /// Mengunci seluruh karakter yang dipilih dan berpindah ke scene Tycoon!
        /// </summary>
        public void ConfirmAndLoadScene()
        {
            if (selectedCharacters.Count == 0)
            {
                Debug.LogWarning("[TycoonCharacterUI] Belum ada karakter yang dipilih!");
                return;
            }

            // Simpan semua karakter terpilih agar KEDUANYA muncul di Tycoon!
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.SelectChosenCharacters(selectedCharacters);
            }
            else
            {
                SaveChosenCharactersDirectly(selectedCharacters);
            }

            Debug.Log($"[TycoonCharacterUI] Total {selectedCharacters.Count} Karakter BERHASIL DISIMPAN & AKTIF di Tycoon!");

            if (!string.IsNullOrEmpty(targetSceneOnSelect))
            {
                SceneManager.LoadScene(targetSceneOnSelect);
            }
        }

        private void SaveChosenCharactersDirectly(List<CharacterSO> characters)
        {
            TycoonSaveData data = new TycoonSaveData();
            if (PlayerPrefs.HasKey("Tycoon_SaveData"))
            {
                string json = PlayerPrefs.GetString("Tycoon_SaveData");
                if (!string.IsNullOrEmpty(json))
                {
                    data = JsonUtility.FromJson<TycoonSaveData>(json) ?? new TycoonSaveData();
                }
            }

            if (data.hiredCharacterIds == null) data.hiredCharacterIds = new List<string>();
            if (data.activeCharacterIds == null) data.activeCharacterIds = new List<string>();

            data.activeCharacterIds.Clear();

            foreach (var charSO in characters)
            {
                if (charSO != null && !string.IsNullOrEmpty(charSO.characterId))
                {
                    if (!data.hiredCharacterIds.Contains(charSO.characterId))
                    {
                        data.hiredCharacterIds.Add(charSO.characterId);
                    }
                    if (!data.activeCharacterIds.Contains(charSO.characterId))
                    {
                        data.activeCharacterIds.Add(charSO.characterId);
                    }
                    InterviewRecruitmentBridge.HireCandidate(charSO.characterId);
                }
            }

            data.hasChosenCharacter = true;
            if (data.activeCharacterIds.Count > 0)
            {
                data.chosenCharacterId = data.activeCharacterIds[0];
            }

            string updatedJson = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString("Tycoon_SaveData", updatedJson);
            PlayerPrefs.Save();
        }
    }
}
