using System.Collections.Generic;
using UnityEngine;

namespace GT.Dialogue.Data
{
    /// <summary>
    /// ScriptableObject kontainer yang menampung urutan seluruh kandidat yang akan diwawancarai.
    /// Defaultnya berisi 6 kandidat yang dieksekusi secara berurutan.
    /// </summary>
    [CreateAssetMenu(fileName = "InterviewSequence_Master", menuName = "GT Rookie/Dialogue/Interview Story Sequence", order = 2)]
    public class InterviewStorySequenceSO : ScriptableObject
    {
        [Header("Urutan Kandidat")]
        [Tooltip("Daftar ScriptableObject kandidat yang akan diwawancarai berurutan (1 s.d. 6)")]
        public List<CandidateInterviewScriptSO> candidates = new List<CandidateInterviewScriptSO>();

        public int TotalCandidates => candidates != null ? candidates.Count : 0;
    }
}
