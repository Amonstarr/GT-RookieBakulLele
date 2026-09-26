using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GT.Dialogue.Core;

namespace GT.Dialogue.UI
{
    /// <summary>
    /// Controller tampilan UI Visual Novel untuk scene DialogueInterview.
    /// Mengelola berkas CV pra-wawancara, teks typewriter, animasi mikro-bounce portrait, tombol pilihan, dan modal rekapitulasi.
    /// </summary>
    public class DialogueInterviewUI : MonoBehaviour
    {
        [Header("Header Tracker")]
        [SerializeField] private TMP_Text candidateProgressText;
        [SerializeField] private TMP_Text questionProgressText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Stage Karakter")]
        [SerializeField] private Image candidatePortraitImage;
        [SerializeField] private TMP_Text candidateNameText;
        [SerializeField] private TMP_Text candidateRoleText;
        [SerializeField] private RectTransform characterPortraitRect;

        [Header("Dialogue Box")]
        [SerializeField] private GameObject dialogueBoxPanel;
        [SerializeField] private Button advanceDialogueButton;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueContentText;
        [SerializeField] private GameObject nextArrowIndicator;
        [SerializeField] private float typewriterSpeed = 0.025f;

        [Header("Choice Container")]
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private List<Button> choiceButtons = new List<Button>();
        [SerializeField] private List<TMP_Text> choiceButtonTexts = new List<TMP_Text>();

        [Header("CV Paper Trigger (Kiri Bawah)")]
        [SerializeField] private GameObject cvPaperContainer;
        [SerializeField] private Button cvPaperButton;

        [Header("CV Document Modal (Tengah Layar)")]
        [SerializeField] private GameObject cvModalPanel;
        [SerializeField] private Image cvDisplayImage;
        [SerializeField] private Button startInterviewButton;
        [SerializeField] private Button closeCVButton;

        [Header("Summary Modal")]
        [SerializeField] private GameObject summaryModalPanel;
        [SerializeField] private TMP_Text summaryTotalScoreText;
        [SerializeField] private TMP_Text summaryFeedbackText;
        [SerializeField] private Button summaryContinueButton;

        // Internal State
        private Coroutine typewriterCoroutine;
        private Coroutine bounceCoroutine;
        private string currentFullText = "";
        private Sprite lastActiveSprite;
        private Vector3 originalPortraitScale = Vector3.one;

        public bool IsTypewriting => typewriterCoroutine != null;

        private void Awake()
        {
            if (characterPortraitRect != null)
            {
                originalPortraitScale = characterPortraitRect.localScale;
            }

            if (choicesPanel != null) choicesPanel.SetActive(false);
            if (summaryModalPanel != null) summaryModalPanel.SetActive(false);
            if (nextArrowIndicator != null) nextArrowIndicator.SetActive(false);
            if (cvModalPanel != null) cvModalPanel.SetActive(false);

            if (advanceDialogueButton != null)
            {
                advanceDialogueButton.onClick.AddListener(() =>
                {
                    if (DialogueInterviewManager.Instance != null)
                    {
                        DialogueInterviewManager.Instance.OnAdvanceClicked();
                    }
                });
            }
        }

        #region Header Updates

        public void UpdateHeader(int candidateIndex, int totalCandidates, int currentScore)
        {
            if (candidateProgressText != null)
            {
                candidateProgressText.text = $"Kandidat {candidateIndex} / {totalCandidates}";
            }

            UpdateScoreDisplay(currentScore);
        }

        public void UpdateQuestionTracker(int questionIndex, int totalQuestions)
        {
            if (questionProgressText == null) return;

            if (questionIndex > 0 && totalQuestions > 0)
            {
                questionProgressText.gameObject.SetActive(true);
                questionProgressText.text = $"Pertanyaan {questionIndex} / {totalQuestions}";
            }
            else
            {
                questionProgressText.gameObject.SetActive(false);
            }
        }

        public void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Modal Keberanian: <color=#FFD700><b>{score}</b> pts</color>";
            }
        }

        #endregion

        #region Candidate Visibility, Portrait & Bio

        public void SetCharacterVisible(bool visible)
        {
            if (candidatePortraitImage != null)
            {
                candidatePortraitImage.gameObject.SetActive(visible);
            }
            if (candidateNameText != null)
            {
                candidateNameText.gameObject.SetActive(visible);
            }
            if (candidateRoleText != null)
            {
                candidateRoleText.gameObject.SetActive(false);
            }
        }

        public void SetDialogueBoxVisible(bool visible)
        {
            if (dialogueBoxPanel != null)
            {
                dialogueBoxPanel.SetActive(visible);
            }
        }

        public void SetCandidateProfile(string name, Sprite defaultPortrait)
        {
            if (candidateNameText != null) candidateNameText.text = name;
            if (candidateRoleText != null) candidateRoleText.gameObject.SetActive(false);

            lastActiveSprite = defaultPortrait;
            UpdatePortraitSprite(defaultPortrait, triggerBounce: true);
        }

        public void UpdatePortraitSprite(Sprite newSprite, bool triggerBounce = true)
        {
            if (newSprite != null)
            {
                lastActiveSprite = newSprite;
            }

            if (candidatePortraitImage != null)
            {
                if (lastActiveSprite != null)
                {
                    candidatePortraitImage.sprite = lastActiveSprite;
                    candidatePortraitImage.color = Color.white;
                    candidatePortraitImage.gameObject.SetActive(true);
                }
                else
                {
                    // Placeholder jika belum ada gambar
                    candidatePortraitImage.color = new Color(1f, 1f, 1f, 0.4f);
                }
            }

            if (triggerBounce)
            {
                PlayPunchBounce();
            }
        }

        public void PlayPunchBounce()
        {
            if (characterPortraitRect == null) return;
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(DoPunchBounce());
        }

        private IEnumerator DoPunchBounce()
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 targetScale = originalPortraitScale * 1.04f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                characterPortraitRect.localScale = Vector3.Lerp(originalPortraitScale, targetScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            characterPortraitRect.localScale = originalPortraitScale;
        }

        #endregion

        #region CV Paper Trigger & Document Modal

        public void ShowCVPaperButton(Action onPaperClicked)
        {
            if (cvPaperContainer != null) cvPaperContainer.SetActive(true);
            else if (cvPaperButton != null) cvPaperButton.gameObject.SetActive(true);

            if (cvPaperButton != null)
            {
                cvPaperButton.onClick.RemoveAllListeners();
                cvPaperButton.onClick.AddListener(() => onPaperClicked?.Invoke());
            }
        }

        public void HideCVPaperButton()
        {
            if (cvPaperContainer != null) cvPaperContainer.SetActive(false);
            else if (cvPaperButton != null) cvPaperButton.gameObject.SetActive(false);
        }

        public void ShowCVModal(Sprite cvSprite, Action onStartInterview, Action onClose = null)
        {
            if (cvModalPanel == null)
            {
                // Fallback jika panel belum terpasang di Inspector
                onStartInterview?.Invoke();
                return;
            }

            cvModalPanel.SetActive(true);

            if (cvDisplayImage != null)
            {
                if (cvSprite != null)
                {
                    cvDisplayImage.sprite = cvSprite;
                    cvDisplayImage.color = Color.white;
                    cvDisplayImage.gameObject.SetActive(true);
                }
                else
                {
                    cvDisplayImage.gameObject.SetActive(false);
                }
            }

            if (startInterviewButton != null)
            {
                startInterviewButton.onClick.RemoveAllListeners();
                startInterviewButton.onClick.AddListener(() =>
                {
                    CloseCVModal();
                    onStartInterview?.Invoke();
                });
            }

            if (closeCVButton != null)
            {
                closeCVButton.onClick.RemoveAllListeners();
                closeCVButton.onClick.AddListener(() =>
                {
                    CloseCVModal();
                    onClose?.Invoke();
                });
            }
        }

        public void CloseCVModal()
        {
            if (cvModalPanel != null)
            {
                cvModalPanel.SetActive(false);
            }
        }

        #endregion

        #region Typewriter Dialogue

        public void DisplayDialogue(string speaker, string text, Sprite expressionSprite = null, Action onTypewriterComplete = null)
        {
            // Pastikan dialogue box aktif saat menampilkan dialog
            SetDialogueBoxVisible(true);

            // Update speaker name
            if (speakerNameText != null)
            {
                if (string.IsNullOrWhiteSpace(speaker))
                {
                    speakerNameText.gameObject.SetActive(false);
                }
                else
                {
                    speakerNameText.gameObject.SetActive(true);
                    speakerNameText.text = speaker;
                }
            }

            // Update expression jika disediakan
            if (expressionSprite != null)
            {
                UpdatePortraitSprite(expressionSprite, triggerBounce: true);
            }

            // Sembunyikan panah & choice saat mulai dialog baru
            if (nextArrowIndicator != null) nextArrowIndicator.SetActive(false);
            if (choicesPanel != null) choicesPanel.SetActive(false);

            currentFullText = text ?? "";

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            typewriterCoroutine = StartCoroutine(TypewriterRoutine(currentFullText, onTypewriterComplete));
        }

        private IEnumerator TypewriterRoutine(string fullText, Action onComplete)
        {
            if (dialogueContentText == null) yield break;

            dialogueContentText.text = "";
            WaitForSeconds wait = new WaitForSeconds(typewriterSpeed);

            for (int i = 0; i < fullText.Length; i++)
            {
                dialogueContentText.text += fullText[i];
                yield return wait;
            }

            CompleteTypewriter();
            onComplete?.Invoke();
        }

        public void CompleteTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }

            if (dialogueContentText != null)
            {
                dialogueContentText.text = currentFullText;
            }

            if (nextArrowIndicator != null)
            {
                nextArrowIndicator.SetActive(true);
            }
        }

        #endregion

        #region Choices Panel

        public void ShowChoices(List<string> choiceTexts, Action<int> onChoiceSelected)
        {
            if (choicesPanel == null) return;

            if (nextArrowIndicator != null) nextArrowIndicator.SetActive(false);
            choicesPanel.SetActive(true);

            for (int i = 0; i < choiceButtons.Count; i++)
            {
                int index = i;
                Button btn = choiceButtons[i];

                if (btn == null) continue;

                if (i < choiceTexts.Count)
                {
                    btn.gameObject.SetActive(true);
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        choicesPanel.SetActive(false);
                        onChoiceSelected?.Invoke(index);
                    });

                    if (i < choiceButtonTexts.Count && choiceButtonTexts[i] != null)
                    {
                        choiceButtonTexts[i].text = choiceTexts[i];
                    }
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        public void HideChoices()
        {
            if (choicesPanel != null) choicesPanel.SetActive(false);
        }

        #endregion

        #region Summary Modal

        public void ShowSummaryModal(int totalScore, int totalQuestions, Action onContinueClicked)
        {
            if (summaryModalPanel == null) return;

            summaryModalPanel.SetActive(true);

            if (summaryTotalScoreText != null)
            {
                summaryTotalScoreText.text = $"Total Modal Keberanian:\n<size=120%><color=#FFD700>+{totalScore} Poin</color></size>";
            }

            if (summaryFeedbackText != null)
            {
                summaryFeedbackText.text = $"Selamat! Kamu telah menyelesaikan {totalQuestions} pertanyaan wawancara untuk 6 kandidat.\n" +
                                           $"Poin ini akan otomatis dikonversi menjadi modal awal koin untuk mendirikan kantormu di mode Tycoon!";
            }

            if (summaryContinueButton != null)
            {
                summaryContinueButton.onClick.RemoveAllListeners();
                summaryContinueButton.onClick.AddListener(() => onContinueClicked?.Invoke());
            }
        }

        #endregion
    }
}
