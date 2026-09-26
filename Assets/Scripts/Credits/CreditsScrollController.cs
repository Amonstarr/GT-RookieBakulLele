using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GT.Credits
{
    /// <summary>
    /// Menggulir credits ala film: konten dibangun dari <see cref="CreditsContentSO"/>,
    /// lalu di-animasi dari bawah layar ke atas sampai habis, lalu pindah ke scene exit.
    /// Ketuk / tekan tombol apa saja untuk melompat ke akhir.
    /// </summary>
    public class CreditsScrollController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Aset konten credit (nama & role). Diisi lewat Inspector.")]
        [SerializeField] private CreditsContentSO content;

        [Header("Layout")]
        [Tooltip("Area tampil (RectMask2D) tempat credit terlihat.")]
        [SerializeField] private RectTransform viewport;
        [Tooltip("Konten hasil generate; harus ber-anchor atas & ber-VerticalLayoutGroup.")]
        [SerializeField] private RectTransform contentTransform;

        [Header("Prefab Baris")]
        [Tooltip("Prefab judul game (big title), satu buah di paling atas.")]
        [SerializeField] private GameObject titlePrefab;
        [Tooltip("Prefab judul bagian / pesan penutup.")]
        [SerializeField] private GameObject headerPrefab;
        [Tooltip("Prefab baris nama + role.")]
        [SerializeField] private GameObject rowPrefab;

        [Header("Pengaturan Scroll")]
        [Tooltip("Kecepatan scroll credits (px/detik).")]
        [SerializeField] private float scrollSpeed = 120f;
        [Tooltip("Jeda di akhir sebelum pindah scene (detik).")]
        [SerializeField] private float endHoldSeconds = 1.5f;
        [Tooltip("Scene yang dimuat setelah credit selesai.")]
        [SerializeField] private string exitSceneName = "Leaderboard";
        [Tooltip("Izinkan klik / tekanan tombol untuk melompat ke akhir credit.")]
        [SerializeField] private bool skipOnAnyInput = true;

        private bool isScrolling;
        private bool isSkipped;

        private void Start()
        {
            BuildRows();
            StartCoroutine(ScrollCredits());
        }

        private void Update()
        {
            if (!isScrolling) return;
            if (skipOnAnyInput && HasSkipInputThisFrame())
            {
                isSkipped = true;
            }
        }

        /// <summary>
        /// Lompat langsung ke akhir credit lalu lanjut ke scene exit (mis. via tombol UI).
        /// </summary>
        public void SkipToEnd()
        {
            isSkipped = true;
        }

        private static bool HasSkipInputThisFrame()
        {
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            bool keyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            return mouse || touch || keyboard;
        }

        private void BuildRows()
        {
            if (contentTransform == null) return;

            for (int i = contentTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(contentTransform.GetChild(i).gameObject);
            }

            if (content == null) return;

            if (!string.IsNullOrEmpty(content.gameTitle) && titlePrefab != null)
            {
                CreateChild(titlePrefab).GetComponent<CreditHeaderUI>()?.SetHeader(content.gameTitle);
            }

            foreach (CreditsSection section in content.sections)
            {
                if (section == null) continue;

                if (!string.IsNullOrEmpty(section.sectionTitle) && headerPrefab != null)
                {
                    CreateChild(headerPrefab).GetComponent<CreditHeaderUI>()?.SetHeader(section.sectionTitle);
                }

                foreach (CreditEntry entry in section.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.name)) continue;
                    CreateChild(rowPrefab).GetComponent<CreditRowUI>()?.SetEntry(entry.name, entry.role);
                }
            }

            if (!string.IsNullOrEmpty(content.thanksMessage) && headerPrefab != null)
            {
                CreateChild(headerPrefab).GetComponent<CreditHeaderUI>()?.SetHeader(content.thanksMessage);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
        }

        private GameObject CreateChild(GameObject prefab)
        {
            if (prefab == null) return null;

            GameObject go = Instantiate(prefab, contentTransform);
            go.SetActive(true);
            return go;
        }

        private IEnumerator ScrollCredits()
        {
            yield return new WaitForEndOfFrame();

            if (contentTransform == null) yield break;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);

            float viewHeight = viewport != null
                ? Mathf.Max(1f, viewport.rect.height)
                : Mathf.Max(1f, Screen.height);
            float contentHeight = Mathf.Max(viewHeight, contentTransform.rect.height);

            float travel = viewHeight + contentHeight;
            contentTransform.anchoredPosition = new Vector2(contentTransform.anchoredPosition.x, -viewHeight);

            float progress = 0f;
            isScrolling = true;
            isSkipped = false;

            while (progress < travel)
            {
                if (isSkipped) progress = travel;

                progress += scrollSpeed * Time.deltaTime;
                float clamped = Mathf.Min(progress, travel);
                contentTransform.anchoredPosition = new Vector2(contentTransform.anchoredPosition.x, -viewHeight + clamped);

                yield return null;
            }

            isScrolling = false;

            yield return new WaitForSeconds(endHoldSeconds);

            if (!string.IsNullOrEmpty(exitSceneName))
            {
                SceneManager.LoadScene(exitSceneName);
            }
        }
    }
}