using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tycoon.Core;
using Tycoon.Data;
using Tycoon.Visual;

namespace Tycoon.UI
{
    /// <summary>
    /// UI Window showing all recruited employees hired from the Interview process.
    /// Displays character info, job titles, and current autonomous status/stamina.
    /// </summary>
    public class TycoonCharacterUI : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private Transform characterCardContainer;
        [SerializeField] private GameObject characterCardPrefab;
        [SerializeField] private ScrollRect characterScrollRect;

        [Header("Arrow Navigation Buttons (Optional)")]
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [Range(0.1f, 0.8f)]
        [SerializeField] private float scrollStep = 0.35f;

        [Header("Options")]
        [SerializeField] private bool openOnStart = false;

        private Coroutine scrollCoroutine;

        private void Start()
        {
            if (characterPanel != null)
            {
                characterPanel.SetActive(openOnStart);
            }

            if (characterScrollRect == null && characterCardContainer != null)
            {
                characterScrollRect = characterCardContainer.GetComponentInParent<ScrollRect>();
            }

            SetupArrowButtons();
            RefreshEmployeeList();
        }

        private void SetupArrowButtons()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.onClick.RemoveAllListeners();
                leftArrowButton.onClick.AddListener(ScrollLeft);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.onClick.RemoveAllListeners();
                rightArrowButton.onClick.AddListener(ScrollRight);
            }
        }

        public void ScrollLeft()
        {
            EnsureScrollRect();
            if (characterScrollRect == null) return;
            float targetPos = Mathf.Clamp01(characterScrollRect.horizontalNormalizedPosition - scrollStep);
            StartSmoothScroll(targetPos);
        }

        public void ScrollRight()
        {
            EnsureScrollRect();
            if (characterScrollRect == null) return;
            float targetPos = Mathf.Clamp01(characterScrollRect.horizontalNormalizedPosition + scrollStep);
            StartSmoothScroll(targetPos);
        }

        private void EnsureScrollRect()
        {
            if (characterScrollRect == null && characterCardContainer != null)
            {
                characterScrollRect = characterCardContainer.GetComponentInParent<ScrollRect>();
            }
            if (characterScrollRect != null)
            {
                characterScrollRect.movementType = ScrollRect.MovementType.Clamped;
                characterScrollRect.inertia = true;
            }
        }

        private void StartSmoothScroll(float targetNormalizedPos)
        {
            if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
            scrollCoroutine = StartCoroutine(SmoothScrollRoutine(targetNormalizedPos));
        }

        private System.Collections.IEnumerator SmoothScrollRoutine(float targetPos)
        {
            if (characterScrollRect == null) yield break;

            float startPos = characterScrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                characterScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            characterScrollRect.horizontalNormalizedPosition = targetPos;
        }

        public void OpenPanel()
        {
            if (characterPanel != null) characterPanel.SetActive(true);
            RefreshEmployeeList();
        }

        public void ClosePanel()
        {
            if (characterPanel != null) characterPanel.SetActive(false);
        }

        public void TogglePanel()
        {
            if (characterPanel != null)
            {
                bool nextState = !characterPanel.activeSelf;
                characterPanel.SetActive(nextState);
                if (nextState) RefreshEmployeeList();
            }
        }

        public void RefreshEmployeeList()
        {
            if (characterCardContainer == null) return;

            EnsureHorizontalLayout();

            foreach (Transform child in characterCardContainer)
            {
                Destroy(child.gameObject);
            }

            if (CharacterManager.Instance == null) return;

            var hiredWorkers = CharacterManager.Instance.HiredCharacters;
            foreach (var charData in hiredWorkers)
            {
                if (charData == null) continue;

                if (characterCardPrefab != null)
                {
                    GameObject card = Instantiate(characterCardPrefab, characterCardContainer);
                    card.transform.localScale = Vector3.one;

                    // Ensure card has LayoutElement for ContentSizeFitter width calculation
                    LayoutElement le = card.GetComponent<LayoutElement>();
                    if (le == null) le = card.AddComponent<LayoutElement>();
                    RectTransform cardRt = card.GetComponent<RectTransform>();
                    float cardW = (cardRt != null && cardRt.rect.width > 30f) ? cardRt.rect.width : 160f;
                    float cardH = (cardRt != null && cardRt.rect.height > 30f) ? cardRt.rect.height : 160f;
                    le.preferredWidth = cardW;
                    le.preferredHeight = cardH;

                    SetupCardView(card, charData);
                }
            }

            // Force immediate UI layout recalculation so container expands properly
            Canvas.ForceUpdateCanvases();
            if (characterCardContainer is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void SetupCardView(GameObject cardObj, CharacterSO data)
        {
            // Find Mascot Image or Button on the card
            Image mascotImage = cardObj.transform.Find("MascotImage")?.GetComponent<Image>();
            if (mascotImage == null) mascotImage = cardObj.transform.Find("AvatarImage")?.GetComponent<Image>();
            if (mascotImage == null) mascotImage = cardObj.GetComponent<Image>();

            Button mascotButton = cardObj.GetComponent<Button>();
            if (mascotButton == null) mascotButton = cardObj.transform.Find("MascotImage")?.GetComponent<Button>();
            if (mascotButton == null) mascotButton = cardObj.transform.Find("ActionButton")?.GetComponent<Button>();

            // Set Mascot Sprite (use avatarIcon or standingSprite)
            Sprite displaySprite = data.avatarIcon != null ? data.avatarIcon : data.standingSprite;
            if (mascotImage != null && displaySprite != null)
            {
                mascotImage.sprite = displaySprite;
            }

            bool isActive = CharacterManager.Instance != null && CharacterManager.Instance.IsCharacterActive(data);

            // Update visual active state (full color when active, dimmed when inactive)
            if (mascotImage != null)
            {
                mascotImage.color = isActive ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.5f);
            }

            // Selection indicator overlay (optional)
            GameObject checkmarkObj = cardObj.transform.Find("ActiveCheckmark")?.gameObject;
            if (checkmarkObj != null)
            {
                checkmarkObj.SetActive(isActive);
            }

            // Click Mascot to Toggle Active Deployment!
            if (mascotButton != null)
            {
                mascotButton.onClick.RemoveAllListeners();
                mascotButton.onClick.AddListener(() =>
                {
                    if (CharacterManager.Instance != null)
                    {
                        CharacterManager.Instance.ToggleCharacterActive(data);
                        RefreshEmployeeList();
                    }
                });
            }
            
            // Text fields are entirely optional now
            TMP_Text nameTxt = cardObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameTxt != null) nameTxt.gameObject.SetActive(false);

            TMP_Text titleTxt = cardObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
            if (titleTxt != null) titleTxt.gameObject.SetActive(false);

            TMP_Text statusTxt = cardObj.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusTxt != null) statusTxt.gameObject.SetActive(false);
        }

        private void EnsureHorizontalLayout()
        {
            if (characterCardContainer == null) return;

            // Ensure container Anchors & Pivot are set to Left-Center (0, 0.5) so ContentSizeFitter can expand width
            if (characterCardContainer is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
            }

            // Auto-add Horizontal Layout Group if missing
            UnityEngine.UI.HorizontalLayoutGroup hlg = characterCardContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = characterCardContainer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            }
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Auto-add Content Size Fitter if missing
            UnityEngine.UI.ContentSizeFitter csf = characterCardContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null)
            {
                csf = characterCardContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}
