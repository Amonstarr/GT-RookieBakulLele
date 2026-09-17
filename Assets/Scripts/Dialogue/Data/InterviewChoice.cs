using System;
using System.Collections.Generic;
using UnityEngine;

namespace GT.Dialogue.Data
{
    /// <summary>
    /// Model data untuk satu opsi pilihan respon wawancara.
    /// Memuat teks tombol, bobot poin bebas/dinamis, dialog reaksi balasan, dan percakapan lanjutan fleksibel.
    /// </summary>
    [Serializable]
    public class InterviewChoice
    {
        [TextArea(2, 3)]
        [Tooltip("Teks pilihan jawaban pada tombol")]
        public string choiceText = "";

        [Tooltip("Poin skor dinamis (bebas diatur, misal: 15, 10, 5, 0, dsb)")]
        public int scoreWeight = 15;

        [Header("Mode Sederhana (1 Balasan Kandidat)")]
        [TextArea(2, 4)]
        [Tooltip("Dialog tanggapan/balasan kandidat (digunakan jika Percakapan Lanjutan di bawah kosong)")]
        public string reactionText = "";

        [Tooltip("Opsional: Sprite pose ekspresi reaksi kandidat saat opsi ini dipilih")]
        public Sprite reactionSprite;

        [Header("Mode Fleksibel: Percakapan Lanjutan (Multi-baris / Bergantian)")]
        [Tooltip("Jika diisi, opsi ini akan memutar rentetan dialog ini (bisa Rookie tanya beberapa kalimat, kandidat jawab beberapa kalimat) sebelum kembali ke dialog universal.")]
        public List<DialogueLine> followUpDialogue = new List<DialogueLine>();
    }
}
