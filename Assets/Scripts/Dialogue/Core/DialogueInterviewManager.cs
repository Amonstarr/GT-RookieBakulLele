using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using GT.Dialogue.Data;
using GT.Dialogue.UI;
using Tycoon.Data;

namespace GT.Dialogue.Core
{
    public enum InterviewState
    {
        ShowingDialogue,
        AwaitingChoice,
        ShowingChoiceDialogue,
        Finished
    }

    /// <summary>
    /// Core manager yang mengendalikan alur wawancara berurutan untuk seluruh kandidat (Visual Novel).
    /// Mengelola state machine, input klik cerita, pemilihan titik keputusan, akumulasi skor, dan bridge ke Tycoon.
    /// </summary>
    public class DialogueInterviewManager : MonoBehaviour
    {
        public static DialogueInterviewManager Instance { get; private set; }

        [Header("Data Skenario Cerita")]
        [Tooltip("Asset urutan kandidat yang akan diwawancarai")]
        [SerializeField] private InterviewStorySequenceSO storySequence;

        [Header("Referensi UI")]
        [SerializeField] private DialogueInterviewUI interviewUI;

        [Header("Pengaturan Player")]
        [Tooltip("Nama pembicara saat player mengucapkan pertanyaan yang dipilih")]
        [SerializeField] private string playerSpeakerName = "Rookie";

        [Header("Pengaturan Transisi & Save")]
        [SerializeField] private string targetNextSceneName = "SelectCharacter";
        [SerializeField] private bool saveScoreToTycoon = true;
        private const string TYCOON_SAVE_KEY = "Tycoon_SaveData";
        private const string SCORE_PREFS_KEY = "Dialogue_TotalScore";

        // Runtime Tracking
        private int currentCandidateIndex = 0;
        private int currentLineIndex = 0;
        private int currentQuestionCount = 0;
        private int totalScore = 0;
        private int totalQuestionsAnswered = 0;
        private List<DialogueLine> activeChoiceDialogue = null;
        private int choiceDialogueIndex = 0;
        private InterviewState currentState = InterviewState.ShowingDialogue;

        public int CurrentScore => totalScore;
        public InterviewState CurrentState => currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoFixEventSystem();
        }

        /// <summary>
        /// Otomatis mengganti StandaloneInputModule lama dengan InputSystemUIInputModule baru pada EventSystem
        /// agar tidak terjadi InvalidOperationException dan klik mouse UI berfungsi normal.
        /// </summary>
        private void AutoFixEventSystem()
        {
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                var legacyModule = es.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    DestroyImmediate(legacyModule);
                    Debug.Log("[DialogueInterviewManager] Berhasil mencopot StandaloneInputModule lawas.");
                }

                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                    Debug.Log("[DialogueInterviewManager] Berhasil memasang InputSystemUIInputModule baru.");
                }
            }
        }

        private void Start()
        {
            if (interviewUI == null)
            {
                interviewUI = FindFirstObjectByType<DialogueInterviewUI>();
            }

            if (storySequence == null || storySequence.candidates.Count == 0)
            {
                Debug.LogWarning("[DialogueInterviewManager] Story Sequence belum dipasang atau kosong! Pasang asset InterviewStorySequenceSO di Inspector.");
                return;
            }

            currentCandidateIndex = 0;
            totalScore = 0;
            totalQuestionsAnswered = 0;

            StartCandidateInterview(currentCandidateIndex);
        }

        private void Update()
        {
            // Input klik kiri mouse: memajukan dialog jika tidak sedang menunggu pilihan
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (currentState != InterviewState.AwaitingChoice)
                {
                    OnAdvanceClicked();
                }
            }

            // Input shortcut keyboard: Space / Enter / NumpadEnter untuk memajukan dialog
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    OnAdvanceClicked();
                }
            }
        }

        /// <summary>
        /// Dipanggil saat pemain mengklik area dialog box atau menekan tombol Next.
        /// </summary>
        public void OnAdvanceClicked()
        {
            if (interviewUI == null) return;

            // Jika teks typewriter masih berjalan, klik pertama langsung mempercepat teks jadi lengkap
            if (interviewUI.IsTypewriting)
            {
                interviewUI.CompleteTypewriter();
                return;
            }

            // Jika sedang menunggu pemain memilih tombol pilihan, abaikan klik layar
            if (currentState == InterviewState.AwaitingChoice)
            {
                return;
            }

            // Jika sedang dalam rangkaian percakapan pilihan (mini-conversation hasil opsi yang dipilih)
            if (currentState == InterviewState.ShowingChoiceDialogue)
            {
                choiceDialogueIndex++;
                if (activeChoiceDialogue != null && choiceDialogueIndex < activeChoiceDialogue.Count)
                {
                    PlayCurrentChoiceDialogue();
                }
                else
                {
                    // Selesai seluruh dialog pilihan! Kembali ke naskah universal berikutnya
                    currentState = InterviewState.ShowingDialogue;
                    currentLineIndex++;
                    PlayCurrentDialogueLine();
                }
                return;
            }

            // Jika sedang dialog biasa, lanjut ke baris berikutnya
            if (currentState == InterviewState.ShowingDialogue)
            {
                currentLineIndex++;
                PlayCurrentDialogueLine();
            }
        }

        private void StartCandidateInterview(int index)
        {
            if (index < 0 || index >= storySequence.candidates.Count)
            {
                FinishAllInterviews();
                return;
            }

            CandidateInterviewScriptSO candidate = storySequence.candidates[index];
            if (candidate == null)
            {
                Debug.LogWarning($"[DialogueInterviewManager] Kandidat di index {index} bernilai null, melewatinya...");
                currentCandidateIndex++;
                StartCandidateInterview(currentCandidateIndex);
                return;
            }

            currentLineIndex = 0;
            currentQuestionCount = 0;
            currentState = InterviewState.ShowingDialogue;

            // Update UI Header & Profil Kandidat (tanpa roleTitle)
            if (interviewUI != null)
            {
                interviewUI.UpdateHeader(index + 1, storySequence.candidates.Count, totalScore);
                interviewUI.UpdateQuestionTracker(0, candidate.TotalDecisionPoints);
                interviewUI.SetCandidateProfile(candidate.candidateName, candidate.defaultPortrait);
            }

            PlayCurrentDialogueLine();
        }

        private void PlayCurrentDialogueLine()
        {
            CandidateInterviewScriptSO candidate = storySequence.candidates[currentCandidateIndex];
            if (candidate == null || candidate.dialogueLines == null) return;

            // Cek apakah dialog kandidat ini sudah habis
            if (currentLineIndex >= candidate.dialogueLines.Count)
            {
                OnCandidateFinished();
                return;
            }

            DialogueLine line = candidate.dialogueLines[currentLineIndex];
            if (line == null)
            {
                currentLineIndex++;
                PlayCurrentDialogueLine();
                return;
            }

            // Tentukan nama pembicara yang tampil di UI
            string speaker = line.speakerName;
            if (string.IsNullOrWhiteSpace(speaker))
            {
                speaker = candidate.candidateName;
            }

            if (line.isDecisionPoint)
            {
                currentQuestionCount++;
                totalQuestionsAnswered++;

                if (interviewUI != null)
                {
                    interviewUI.UpdateQuestionTracker(currentQuestionCount, candidate.TotalDecisionPoints);
                    interviewUI.DisplayDialogue(speaker, line.dialogueText, line.expressionSprite, () =>
                    {
                        // Begitu typewriter selesai, munculkan 3 tombol pilihan
                        PresentChoices(line.choices);
                    });
                }
            }
            else
            {
                currentState = InterviewState.ShowingDialogue;
                if (interviewUI != null)
                {
                    interviewUI.DisplayDialogue(speaker, line.dialogueText, line.expressionSprite);
                }
            }
        }

        private void PresentChoices(List<InterviewChoice> choices)
        {
            if (choices == null || choices.Count == 0)
            {
                // Fallback jika tidak ada pilihan terdaftar
                currentState = InterviewState.ShowingDialogue;
                return;
            }

            currentState = InterviewState.AwaitingChoice;

            List<string> choiceTexts = new List<string>();
            foreach (var c in choices)
            {
                choiceTexts.Add(c != null ? c.choiceText : "");
            }

            if (interviewUI != null)
            {
                interviewUI.ShowChoices(choiceTexts, OnChoiceSelected);
            }
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            CandidateInterviewScriptSO candidate = storySequence.candidates[currentCandidateIndex];
            DialogueLine line = candidate.dialogueLines[currentLineIndex];

            if (line == null || choiceIndex < 0 || choiceIndex >= line.choices.Count) return;

            InterviewChoice selectedChoice = line.choices[choiceIndex];
            if (selectedChoice == null) return;

            // Tambahkan skor
            totalScore += selectedChoice.scoreWeight;

            // Update UI Score
            if (interviewUI != null)
            {
                interviewUI.UpdateScoreDisplay(totalScore);
            }

            Debug.Log($"[DialogueInterviewManager] Opsi '{selectedChoice.choiceText}' dipilih (+{selectedChoice.scoreWeight} pts). Total: {totalScore}");

            // Siapkan rangkaian percakapan pilihan (mini-conversation):
            // Jika followUpDialogue diisi, gunakan naskah multi-baris tersebut.
            // Jika tidak, gunakan mode 2 langkah bawaan (Rookie bertanya -> Kandidat menjawab).
            if (selectedChoice.followUpDialogue != null && selectedChoice.followUpDialogue.Count > 0)
            {
                activeChoiceDialogue = selectedChoice.followUpDialogue;
            }
            else
            {
                activeChoiceDialogue = new List<DialogueLine>
                {
                    new DialogueLine { speakerName = playerSpeakerName, dialogueText = selectedChoice.choiceText },
                    new DialogueLine { speakerName = candidate.candidateName, dialogueText = selectedChoice.reactionText, expressionSprite = selectedChoice.reactionSprite }
                };
            }

            choiceDialogueIndex = 0;
            currentState = InterviewState.ShowingChoiceDialogue;
            PlayCurrentChoiceDialogue();
        }

        private void PlayCurrentChoiceDialogue()
        {
            if (activeChoiceDialogue == null || choiceDialogueIndex >= activeChoiceDialogue.Count) return;
            DialogueLine line = activeChoiceDialogue[choiceDialogueIndex];
            CandidateInterviewScriptSO candidate = storySequence.candidates[currentCandidateIndex];

            string speaker = line.speakerName;
            if (string.IsNullOrWhiteSpace(speaker))
            {
                speaker = candidate != null ? candidate.candidateName : playerSpeakerName;
            }

            if (interviewUI != null)
            {
                interviewUI.DisplayDialogue(speaker, line.dialogueText, line.expressionSprite);
            }
        }

        private void OnCandidateFinished()
        {
            currentCandidateIndex++;

            if (currentCandidateIndex < storySequence.candidates.Count)
            {
                Debug.Log($"[DialogueInterviewManager] Kandidat selesai! Berpindah ke kandidat index {currentCandidateIndex}.");
                StartCandidateInterview(currentCandidateIndex);
            }
            else
            {
                FinishAllInterviews();
            }
        }

        private void FinishAllInterviews()
        {
            currentState = InterviewState.Finished;
            Debug.Log($"[DialogueInterviewManager] Semua kandidat telah selesai diwawancarai! Total Skor: {totalScore}");

            // Simpan skor ke PlayerPrefs & Data Tycoon
            SaveInterviewResults();

            // Tampilkan Modal Summary di UI
            if (interviewUI != null)
            {
                interviewUI.ShowSummaryModal(totalScore, totalQuestionsAnswered, OnSummaryContinueClicked);
            }
        }

        private void SaveInterviewResults()
        {
            // Simpan skor mentah ke PlayerPrefs
            PlayerPrefs.SetInt(SCORE_PREFS_KEY, totalScore);

            if (saveScoreToTycoon)
            {
                TycoonSaveData data = new TycoonSaveData();

                if (PlayerPrefs.HasKey(TYCOON_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(TYCOON_SAVE_KEY);
                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            data = JsonUtility.FromJson<TycoonSaveData>(json) ?? new TycoonSaveData();
                        }
                        catch
                        {
                            data = new TycoonSaveData();
                        }
                    }
                }

                // Tambahkan modal awal koin Tycoon dari skor interview
                data.coins += totalScore;

                string updatedJson = JsonUtility.ToJson(data, true);
                PlayerPrefs.SetString(TYCOON_SAVE_KEY, updatedJson);
                PlayerPrefs.Save();

                Debug.Log($"[DialogueInterviewManager] Berhasil menyimpan bonus modal koin Tycoon sebesar +{totalScore}! Total Koin Tycoon sekarang: {data.coins}");
            }
            else
            {
                PlayerPrefs.Save();
            }
        }

        private void OnSummaryContinueClicked()
        {
            if (!string.IsNullOrEmpty(targetNextSceneName))
            {
                Debug.Log($"[DialogueInterviewManager] Memuat scene berikutnya: '{targetNextSceneName}'...");
                SceneManager.LoadScene(targetNextSceneName);
            }
            else
            {
                Debug.LogWarning("[DialogueInterviewManager] targetNextSceneName belum diisi.");
            }
        }
    }
}
