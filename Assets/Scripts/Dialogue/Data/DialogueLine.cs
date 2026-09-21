using System;
using System.Collections.Generic;
using UnityEngine;

namespace GT.Dialogue.Data
{
    /// <summary>
    /// Model data untuk satu baris kalimat dialog dalam format Visual Novel.
    /// Mendukung pergantian pose per baris dan titik keputusan (Decision Point).
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Nama karakter pembicara (misal: Rookie, Kandidat, atau kosongkan jika narasi)")]
        public string speakerName = "";

        [TextArea(2, 5)]
        [Tooltip("Isi teks kalimat dialog yang diucapkan")]
        public string dialogueText = "";

        [Tooltip("Opsional: Sprite pose/ekspresi karakter untuk kalimat ini. Jika kosong, sistem otomatis mempertahankan pose sebelumnya.")]
        public Sprite expressionSprite;

        [Header("Titik Keputusan (Decision Point)")]
        [Tooltip("Centang jika baris ini merupakan titik keputusan/pertanyaan wawancara.")]
        public bool isDecisionPoint = false;

        [Tooltip("Judul/topik pertanyaan opsional (misal: 'Pertanyaan 1: Manajemen Konflik')")]
        public string questionTitle = "";

        [Tooltip("Daftar opsi pilihan jawaban (biasanya 3 pilihan) jika isDecisionPoint dicentang.")]
        public List<InterviewChoice> choices = new List<InterviewChoice>();
    }
}
