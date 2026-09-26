using System;
using System.Collections.Generic;
using UnityEngine;

namespace GT.Credits
{
    /// <summary>
    /// Data konten credit scene. Isi lewat Inspector: judul game, judul bagian,
    /// lalu daftar nama + role per bagian. Runtime membaca aset ini untuk membangun baris credit.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCreditList", menuName = "Game/Credit List")]
    public class CreditsContentSO : ScriptableObject
    {
        [Header("Judul Game (opsional, tampil besar di awal credit)")]
        [Tooltip("Kosongkan untuk menampilkan kredit langsung tanpa judul.")]
        public string gameTitle = "ROOKIE BAKUL LELE";

        [Tooltip("Teks penutup di akhir credit. Kosongkan untuk menonaktifkan.")]
        public string thanksMessage = "TERIMA KASIH SUDAH BERMAIN!";

        [Header("Daftar Kredit - isi nama & role saja")]
        public List<CreditsSection> sections = new List<CreditsSection>();
    }

    [Serializable]
    public class CreditsSection
    {
        [Tooltip("Judul bagian, misal: Game Design / Programming")]
        public string sectionTitle = "Bagian Baru";

        [Tooltip("Daftar pembuat beserta perannya dalam bagian ini")]
        public List<CreditEntry> entries = new List<CreditEntry>();
    }

    [Serializable]
    public class CreditEntry
    {
        public string name;
        public string role;
    }
}