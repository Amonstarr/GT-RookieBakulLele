using System.Collections.Generic;
using UnityEngine;

namespace GT.Dialogue.Data
{
    /// <summary>
    /// ScriptableObject yang menampung profil dan naskah percakapan lengkap untuk 1 kandidat.
    /// Naskah berisi dialog mengalir baris demi baris, pergantian pose, serta 3 pertanyaan/titik keputusan.
    /// </summary>
    [CreateAssetMenu(fileName = "CandidateScript_New", menuName = "GT Rookie/Dialogue/Candidate Interview Script", order = 1)]
    public class CandidateInterviewScriptSO : ScriptableObject
    {
        [Header("Profil Kandidat (Bebas & Dinamis)")]
        [Tooltip("ID unik kandidat (bisa disinkronkan dengan Tycoon, misal: worker_1, worker_dev_1, dll)")]
        public string candidateId = "candidate_01";

        [Tooltip("Nama lengkap kandidat")]
        public string candidateName = "Nama Kandidat";

        [TextArea(2, 3)]
        [Tooltip("Deskripsi singkat / bio kandidat")]
        public string candidateBio = "";

        [Header("Visual Default")]
        [Tooltip("Pose/portrait netral default saat kandidat pertama kali masuk ruangan wawancara")]
        public Sprite defaultPortrait;

        [Header("Dokumen CV")]
        [Tooltip("Sprite/gambar berkas CV lengkap kandidat ini yang ditampilkan saat pemain memeriksa kertas di meja")]
        public Sprite cvDocumentSprite;

        [Header("Naskah Dialog & Pertanyaan")]
        [Tooltip("Daftar dialog mengalir kalimat demi kalimat lengkap dengan 3 titik keputusan")]
        public List<DialogueLine> dialogueLines = new List<DialogueLine>();

        /// <summary>
        /// Menghitung total pertanyaan / decision points di dalam naskah kandidat ini.
        /// </summary>
        public int TotalDecisionPoints
        {
            get
            {
                int count = 0;
                foreach (var line in dialogueLines)
                {
                    if (line != null && line.isDecisionPoint) count++;
                }
                return count;
            }
        }
    }
}
