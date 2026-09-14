using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tycoon.Core;
using Tycoon.Data;

namespace Tycoon.UI
{
    /// <summary>
    /// Manages the Office Upgrade Shop UI window.
    /// Spawns and populates item cards for all registered OfficeItemSO assets.
    /// </summary>
    public class TycoonShopUI : MonoBehaviour
    {
        [Header("Shop Container References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform itemCardContainer;
        [SerializeField] private GameObject itemCardPrefab;
        [SerializeField] private ScrollRect shopScrollRect;

        [Header("Arrow Navigation Buttons (Optional)")]
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [Range(0.1f, 0.8f)]
        [SerializeField] private float scrollStep = 0.35f;

        [Header("Options")]
        [SerializeField] private bool openOnStart = false;

        private List<TycoonItemCardUI> spawnedCards = new List<TycoonItemCardUI>();
        private Coroutine scrollCoroutine;

        private void Start()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(openOnStart);
            }

            if (shopScrollRect == null && itemCardContainer != null)
            {
                shopScrollRect = itemCardContainer.GetComponentInParent<ScrollRect>();
            }

            SetupArrowButtons();
            PopulateShop();
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
            if (shopScrollRect == null) return;
            float targetPos = Mathf.Clamp01(shopScrollRect.horizontalNormalizedPosition - scrollStep);
            StartSmoothScroll(targetPos);
        }

        public void ScrollRight()
        {
            EnsureScrollRect();
            if (shopScrollRect == null) return;
            float targetPos = Mathf.Clamp01(shopScrollRect.horizontalNormalizedPosition + scrollStep);
            StartSmoothScroll(targetPos);
        }

        private void EnsureScrollRect()
        {
            if (shopScrollRect == null && itemCardContainer != null)
            {
                shopScrollRect = itemCardContainer.GetComponentInParent<ScrollRect>();
            }
            if (shopScrollRect != null)
            {
                shopScrollRect.movementType = ScrollRect.MovementType.Clamped;
                shopScrollRect.inertia = true;
            }
        }

        private void StartSmoothScroll(float targetNormalizedPos)
        {
            if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
            scrollCoroutine = StartCoroutine(SmoothScrollRoutine(targetNormalizedPos));
        }

        private System.Collections.IEnumerator SmoothScrollRoutine(float targetPos)
        {
            if (shopScrollRect == null) yield break;

            float startPos = shopScrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                shopScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }

            shopScrollRect.horizontalNormalizedPosition = targetPos;
        }

        /// <summary>
        /// Instantiates cards for all available items registered in TycoonManager.
        /// </summary>
        public void PopulateShop()
        {
            if (itemCardContainer == null) return;

            // Ensure container has HorizontalLayoutGroup & ContentSizeFitter to prevent overlapping
            EnsureHorizontalLayout();

            // Clear existing cards
            foreach (Transform child in itemCardContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedCards.Clear();

            if (TycoonManager.Instance == null)
            {
                Debug.LogWarning("[TycoonShopUI] TycoonManager.Instance is null! Make sure TycoonManager GameObject exists in scene.");
                return;
            }

            if (itemCardPrefab == null)
            {
                Debug.LogWarning("[TycoonShopUI] itemCardPrefab is not assigned in Inspector!");
                return;
            }

            if (TycoonManager.Instance.AvailableItems == null || TycoonManager.Instance.AvailableItems.Count == 0)
            {
                Debug.LogWarning("[TycoonShopUI] TycoonManager.AvailableItems is empty! Make sure OfficeItemSO assets are registered in TycoonManager availableItems list.");
                return;
            }

            foreach (var item in TycoonManager.Instance.AvailableItems)
            {
                if (item == null) continue;

                GameObject cardObj = Instantiate(itemCardPrefab, itemCardContainer);
                cardObj.transform.localScale = Vector3.one;

                // Ensure card has LayoutElement so HorizontalLayoutGroup & ContentSizeFitter calculate total width properly
                LayoutElement le = cardObj.GetComponent<LayoutElement>();
                if (le == null) le = cardObj.AddComponent<LayoutElement>();
                RectTransform cardRt = cardObj.GetComponent<RectTransform>();
                float cardW = (cardRt != null && cardRt.rect.width > 50f) ? cardRt.rect.width : 240f;
                float cardH = (cardRt != null && cardRt.rect.height > 50f) ? cardRt.rect.height : 340f;
                le.preferredWidth = cardW;
                le.preferredHeight = cardH;

                TycoonItemCardUI cardUI = cardObj.GetComponent<TycoonItemCardUI>();
                if (cardUI != null)
                {
                    cardUI.Setup(item);
                    spawnedCards.Add(cardUI);
                }
            }

            // Force immediate UI layout recalculation so ContentSizeFitter updates container width from 0 to full size
            Canvas.ForceUpdateCanvases();
            if (itemCardContainer is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        public void OpenShop()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            if (spawnedCards.Count == 0) PopulateShop();
            else RefreshAllCards();
        }

        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
        }

        public void ToggleShop()
        {
            if (shopPanel != null)
            {
                bool nextState = !shopPanel.activeSelf;
                shopPanel.SetActive(nextState);
                if (nextState)
                {
                    if (spawnedCards.Count == 0) PopulateShop();
                    else RefreshAllCards();
                }
            }
        }

        public void RefreshAllCards()
        {
            if (spawnedCards.Count == 0)
            {
                PopulateShop();
                return;
            }

            foreach (var card in spawnedCards)
            {
                if (card != null) card.RefreshCard();
            }
        }

        private void EnsureHorizontalLayout()
        {
            if (itemCardContainer == null) return;

            // Ensure container Anchors & Pivot are set to Left-Center (0, 0.5) so ContentSizeFitter can expand width
            if (itemCardContainer is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
            }

            // Auto-add Horizontal Layout Group if missing
            UnityEngine.UI.HorizontalLayoutGroup hlg = itemCardContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = itemCardContainer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            }
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Auto-add Content Size Fitter if missing
            UnityEngine.UI.ContentSizeFitter csf = itemCardContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null)
            {
                csf = itemCardContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}
