using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using GT.Dialogue.Data;
using GT.Dialogue.UI;
using GT.Leaderboard;
using Tycoon.Data;

namespace GT.Dialogue.Core
{
    public enum InterviewState
    {
        WaitingForCVInspection, // Menunggu pemain mengklik kertas berkas CV di meja (kiri bawah)
        ViewingCV,              // Sedang membuka pop-up gambar berkas CV kandidat
        ShowingDialogue,        // Percakapan wawancara aktif berlangsung
        AwaitingChoice,         // Menunggu pemain memilih 1 dari opsi pertanyaan/tanggapan
        ShowingChoiceDialogue,  // Menampilkan tindak lanjut dialog setelah opsi dipilih
        Finished                // Seluruh rangkaian 6 kandidat telah tuntas
    }

    /// <summary>
    /// Core manager yang mengendalikan alur wawancara berurutan untuk seluruh kandidat (Visual Novel).
    /// Mengelola berkas CV pra-wawancara, state machine, input klik cerita, pemilihan titik keputusan, akumulasi skor, dan bridge ke Tycoon.
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
        [System.NonSerialized]
        private List<ChoiceDialogueLine> activeChoiceDialogue = null;
        private int choiceDialogueIndex = 0;
        private InterviewState currentState = InterviewState.WaitingForCVInspection;

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
            // Input klik kiri mouse: memajukan dialog jika sedang dalam fase percakapan aktif
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (currentState == InterviewState.ShowingDialogue || currentState == InterviewState.ShowingChoiceDialogue)
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
                    if (currentState == InterviewState.ShowingDialogue || currentState == InterviewState.ShowingChoiceDialogue)
                    {
                        OnAdvanceClicked();
                    }
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

            // Jika sedang menunggu pemain memilih tombol pilihan atau berkas CV, abaikan klik dialog
            if (currentState == InterviewState.AwaitingChoice ||
                currentState == InterviewState.WaitingForCVInspection ||
                currentState == InterviewState.ViewingCV ||
                currentState == InterviewState.Finished)
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

            // Sembunyikan karakter dan kotak dialog sebelum wawancara dimulai (ruangan masih kosong)
            if (interviewUI != null)
            {
                interviewUI.UpdateHeader(index + 1, storySequence.candidates.Count, totalScore);
                interviewUI.UpdateQuestionTracker(0, candidate.TotalDecisionPoints);
                interviewUI.SetCharacterVisible(false);
                interviewUI.SetDialogueBoxVisible(false);
                interviewUI.HideChoices();

                // Munculkan kertas CV di kiri bawah meja
                currentState = InterviewState.WaitingForCVInspection;
                interviewUI.ShowCVPaperButton(() => OnCVPaperClicked(candidate));
            }
            else
            {
                // Fallback jika tidak ada UI
                BeginInterviewConversation(candidate);
            }
        }

        private void OnCVPaperClicked(CandidateInterviewScriptSO candidate)
        {
            currentState = InterviewState.ViewingCV;
            if (interviewUI != null)
            {
                interviewUI.ShowCVModal(
                    candidate.cvDocumentSprite,
                    onStartInterview: () =>
                    {
                        // Pemain menekan tombol "Mulai Wawancara"
                        BeginInterviewConversation(candidate);
                    },
                    onClose: () =>
                    {
                        // Pemain menutup dokumen dan kembali melihat meja
                        currentState = InterviewState.WaitingForCVInspection;
                    }
                );
            }
        }

        private void BeginInterviewConversation(CandidateInterviewScriptSO candidate)
        {
            currentState = InterviewState.ShowingDialogue;

            if (interviewUI != null)
            {
                interviewUI.CloseCVModal();
                interviewUI.HideCVPaperButton();

                // Kandidat dipersilakan masuk ke ruangan (karakter muncul) dan kotak dialog aktif
                interviewUI.SetCandidateProfile(candidate.candidateName, candidate.defaultPortrait);
                interviewUI.SetCharacterVisible(true);
                interviewUI.SetDialogueBoxVisible(true);
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
                        // Begitu typewriter selesai, munculkan pilihan
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
            if (selectedChoice.followUpDialogue != null && selectedChoice.followUpDialogue.Count > 0)
            {
                activeChoiceDialogue = selectedChoice.followUpDialogue;
            }
            else
            {
                activeChoiceDialogue = new List<ChoiceDialogueLine>
                {
                    new ChoiceDialogueLine { speakerName = playerSpeakerName, dialogueText = selectedChoice.choiceText },
                    new ChoiceDialogueLine { speakerName = candidate.candidateName, dialogueText = selectedChoice.reactionText, expressionSprite = selectedChoice.reactionSprite }
                };
            }

            choiceDialogueIndex = 0;
            currentState = InterviewState.ShowingChoiceDialogue;
            PlayCurrentChoiceDialogue();
        }

        private void PlayCurrentChoiceDialogue()
        {
            if (activeChoiceDialogue == null || choiceDialogueIndex >= activeChoiceDialogue.Count) return;
            ChoiceDialogueLine line = activeChoiceDialogue[choiceDialogueIndex];
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

            // Sembunyikan karakter dan kotak dialog saat kandidat selesai
            if (interviewUI != null)
            {
                interviewUI.SetCharacterVisible(false);
                interviewUI.SetDialogueBoxVisible(false);
            }

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

            // Kirim skor total ke leaderboard Unity Gaming Services
            SubmitScoreToLeaderboard();

            // Tampilkan Modal Summary di UI
            if (interviewUI != null)
            {
                interviewUI.ShowSummaryModal(totalScore, totalQuestionsAnswered, OnSummaryContinueClicked);
            }
        }

        /// <summary>
        /// Mengirim skor akhir interview ke leaderboard tanpa menunggu hasilnya,
        /// sehingga tidak menghambat transisi ke scene berikutnya.
        /// </summary>
        private async void SubmitScoreToLeaderboard()
        {
            if (LeaderboardManager.Instance == null)
            {
                Debug.LogWarning("[DialogueInterviewManager] LeaderboardManager belum ada di scene, skor tidak dikirim ke leaderboard.");
                return;
            }

            var entry = await LeaderboardManager.Instance.SubmitScoreAsync(totalScore);
            if (entry != null)
            {
                Debug.Log($"[DialogueInterviewManager] Skor interview {totalScore} terdaftar di leaderboard dengan rank #{entry.Rank}.");
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
                            data = JsonUtility.FromJson<TycoonSaveData>(json);
                        }
                        catch
                        {
                            data = new TycoonSaveData();
                        }
                    }
                }

                if (data != null)
                {
                    data.coins += totalScore;
                    string updatedJson = JsonUtility.ToJson(data);
                    PlayerPrefs.SetString(TYCOON_SAVE_KEY, updatedJson);
                    PlayerPrefs.Save();
                    Debug.Log($"[DialogueInterviewManager] Berhasil menambahkan {totalScore} ke kas awal Tycoon! Saldo koin baru: {data.coins}");
                }
            }
        }

        private void OnSummaryContinueClicked()
        {
            Debug.Log($"[DialogueInterviewManager] Memuat scene berikutnya: {targetNextSceneName}");
            SceneManager.LoadScene(targetNextSceneName);
        }
    }
}
