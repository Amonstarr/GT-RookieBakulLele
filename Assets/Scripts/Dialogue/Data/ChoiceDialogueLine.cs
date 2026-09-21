using System;
using UnityEngine;

namespace GT.Dialogue.Data
{
    /// <summary>
    /// Model data baris percakapan lanjutan khusus untuk pilihan wawancara.
    /// Tidak memiliki daftar pilihan rekursif untuk memutus siklus serialisasi Unity (Serialization Depth Limit).
    /// </summary>
    [Serializable]
    public class ChoiceDialogueLine
    {
        [Tooltip("Nama karakter pembicara (misal: Rookie, Kandidat)")]
        public string speakerName = "";

        [TextArea(2, 4)]
        [Tooltip("Isi kalimat dialog")]
        public string dialogueText = "";

        [Tooltip("Opsional: Sprite pose/ekspresi karakter untuk kalimat ini.")]
        public Sprite expressionSprite;
    }
}
