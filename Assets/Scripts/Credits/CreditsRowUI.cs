using UnityEngine;
using TMPro;

namespace GT.Credits
{
    /// <summary>
    /// Satu baris kredit: nama (besar, putih) + role (lebih kecil, abu-abu).
    /// </summary>
    public class CreditRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;

        public void SetEntry(string name, string role)
        {
            if (nameText != null) nameText.text = name;
            if (roleText != null) roleText.text = role;
        }
    }

    /// <summary>
    /// Penanda judul bagian (misal "GAME DESIGN") sekaligus dipakai untuk judul game
    /// dan pesan penutup bila teks latar belakang sama.
    /// </summary>
    public class CreditHeaderUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text headerText;

        public void SetHeader(string title)
        {
            if (headerText != null) headerText.text = title;
        }
    }
}