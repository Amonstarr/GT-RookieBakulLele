using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tycoon.Core;
using Tycoon.Data;
using Tycoon.Economy;

namespace Tycoon.UI
{
    /// <summary>
    /// Manages the Office Upgrade Shop UI window.
    /// Spawns and populates item cards for all registered OfficeItemSO assets.
    /// Supports left/right sidebar positioning and real-time top coin header display.
    /// </summary>
    public class TycoonShopUI : MonoBehaviour
    {
        [Header("Shop Container References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform itemCardContainer;
        [SerializeField] private GameObject itemCardPrefab;
        [SerializeField] private ScrollRect shopScrollRect;

        [Header("Header / Currency Display")]
        [SerializeField] private TMP_Text shopCoinsText;
        [SerializeField] private Text uiShopCoinsText;
        [SerializeField] private string coinsFormatString = "Koin: {0:N0}";

        [Header("Control Buttons")]
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button closeButton;

        [Header("Arrow Navigation Buttons (Optional)")]
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [Range(0.1f, 0.8f)]
        [SerializeField] private float scrollStep = 0.35f;

        [Header("Options")]
        [SerializeField] private bool openOnStart = false;
        [SerializeField] private bool useVerticalSidebar = true;
        [SerializeField] private bool placeOnLeftSide = true;

        private List<TycoonItemCardUI> spawnedCards = new List<TycoonItemCardUI>();
        private Coroutine scrollCoroutine;

        private void Start()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(openOnStart);
            }

            if (openShopButton != null && shopPanel != null)
            {
                openShopButton.gameObject.SetActive(!shopPanel.activeSelf);
            }

            if (shopScrollRect == null && itemCardContainer != null)
            {
                shopScrollRect = itemCardContainer.GetComponentInParent<ScrollRect>();
            }

            SetupControlButtons();
            SetupArrowButtons();
            SetupCurrencyDisplay();
            PopulateShop();
        }

        private void OnDestroy()
        {
            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged -= UpdateShopCoinsText;
            }
        }

        private void SetupCurrencyDisplay()
        {
            if (shopCoinsText == null && uiShopCoinsText == null && shopPanel != null)
            {
                Transform foundText = shopPanel.transform.Find("Header/CoinsText");
                if (foundText == null) foundText = shopPanel.transform.Find("CoinsText");
                if (foundText == null) foundText = shopPanel.transform.Find("Header/TextCoins");
                if (foundText == null) foundText = shopPanel.transform.Find("TextCoins");

                if (foundText != null)
                {
                    shopCoinsText = foundText.GetComponent<TMP_Text>();
                    if (shopCoinsText == null) uiShopCoinsText = foundText.GetComponent<Text>();
                }
            }

            if (TycoonCurrencyManager.Instance != null)
            {
                TycoonCurrencyManager.Instance.OnCoinsChanged -= UpdateShopCoinsText;
                TycoonCurrencyManager.Instance.OnCoinsChanged += UpdateShopCoinsText;
                UpdateShopCoinsText(TycoonCurrencyManager.Instance.CurrentCoins);
            }
        }

        private void UpdateShopCoinsText(int coins)
        {
            string formattedText = string.Format(coinsFormatString, coins);

            if (shopCoinsText != null)
            {
                shopCoinsText.text = formattedText;
            }

            if (uiShopCoinsText != null)
            {
                uiShopCoinsText.text = formattedText;
            }
        }

        public void RefreshCurrencyDisplay()
        {
            if (TycoonCurrencyManager.Instance != null)
            {
                UpdateShopCoinsText(TycoonCurrencyManager.Instance.CurrentCoins);
            }
        }

        private void SetupControlButtons()
        {
            if (openShopButton == null && shopPanel != null)
            {
                // Try finding open shop button in HUD / Canvas if unassigned in inspector
                Transform parentCanvas = shopPanel.transform.parent;
                if (parentCanvas != null)
                {
                    Transform foundOpen = parentCanvas.Find("OpenShopButton");
                    if (foundOpen == null) foundOpen = parentCanvas.Find("ShopButton");
                    if (foundOpen == null) foundOpen = parentCanvas.Find("ButtonShop");
                    if (foundOpen != null) openShopButton = foundOpen.GetComponent<Button>();
                }
            }

            if (openShopButton != null)
            {
                openShopButton.onClick.RemoveAllListeners();
                openShopButton.onClick.AddListener(OpenShop);
            }

            if (closeButton == null && shopPanel != null)
            {
                // Try auto-finding a child button named CloseButton or ButtonClose if unassigned in inspector
                Transform foundBtn = shopPanel.transform.Find("CloseButton");
                if (foundBtn == null) foundBtn = shopPanel.transform.Find("Header/CloseButton");
                if (foundBtn == null) foundBtn = shopPanel.transform.Find("ButtonClose");
                if (foundBtn != null) closeButton = foundBtn.GetComponent<Button>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }
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
                if (useVerticalSidebar)
                {
                    shopScrollRect.vertical = true;
                    shopScrollRect.horizontal = false;
                }
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

            // Ensure container layout matches sidebar configuration
            if (useVerticalSidebar)
            {
                EnsureVerticalLayout();
            }
            else
            {
                EnsureHorizontalLayout();
            }

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

                // Ensure card has LayoutElement so layout groups calculate sizes properly
                LayoutElement le = cardObj.GetComponent<LayoutElement>();
                if (le == null) le = cardObj.AddComponent<LayoutElement>();
                RectTransform cardRt = cardObj.GetComponent<RectTransform>();

                if (useVerticalSidebar)
                {
                    float cardH = (cardRt != null && cardRt.rect.height > 40f) ? cardRt.rect.height : 80f;
                    le.preferredHeight = cardH;
                }
                else
                {
                    float cardW = (cardRt != null && cardRt.rect.width > 50f) ? cardRt.rect.width : 240f;
                    float cardH = (cardRt != null && cardRt.rect.height > 50f) ? cardRt.rect.height : 340f;
                    le.preferredWidth = cardW;
                    le.preferredHeight = cardH;
                }

                TycoonItemCardUI cardUI = cardObj.GetComponent<TycoonItemCardUI>();
                if (cardUI != null)
                {
                    cardUI.Setup(item);
                    spawnedCards.Add(cardUI);
                }
            }

            // Force immediate UI layout recalculation
            Canvas.ForceUpdateCanvases();
            if (itemCardContainer is RectTransform rectTransform)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        public void OpenShop()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            if (openShopButton != null) openShopButton.gameObject.SetActive(false);
            RefreshCurrencyDisplay();
            if (spawnedCards.Count == 0) PopulateShop();
            else RefreshAllCards();
        }

        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (openShopButton != null) openShopButton.gameObject.SetActive(true);
        }

        public void ToggleShop()
        {
            if (shopPanel != null)
            {
                bool nextState = !shopPanel.activeSelf;
                shopPanel.SetActive(nextState);
                if (openShopButton != null) openShopButton.gameObject.SetActive(!nextState);
                if (nextState)
                {
                    RefreshCurrencyDisplay();
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

        private void EnsureVerticalLayout()
        {
            if (itemCardContainer == null) return;

            // Anchor & Pivot for Vertical List Content (Top-Stretch)
            if (itemCardContainer is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }

            // Remove HorizontalLayoutGroup if it exists
            UnityEngine.UI.HorizontalLayoutGroup hlg = itemCardContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg != null) Destroy(hlg);

            // Auto-add Vertical Layout Group if missing
            UnityEngine.UI.VerticalLayoutGroup vlg = itemCardContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = itemCardContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            }
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Auto-add Content Size Fitter if missing
            UnityEngine.UI.ContentSizeFitter csf = itemCardContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null)
            {
                csf = itemCardContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            EnsureScrollRect();
            EnsureSidebarPosition();
        }

        private void EnsureSidebarPosition()
        {
            if (shopPanel == null) return;

            RectTransform panelRt = shopPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                if (placeOnLeftSide)
                {
                    // Anchor to Left-Stretch (Tepi kiri layar, tinggi penuh)
                    panelRt.anchorMin = new Vector2(0f, 0f);
                    panelRt.anchorMax = new Vector2(0f, 1f);
                    panelRt.pivot = new Vector2(0f, 0.5f);
                    panelRt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Anchor to Right-Stretch (Tepi kanan layar, tinggi penuh)
                    panelRt.anchorMin = new Vector2(1f, 0f);
                    panelRt.anchorMax = new Vector2(1f, 1f);
                    panelRt.pivot = new Vector2(1f, 0.5f);
                    panelRt.anchoredPosition = Vector2.zero;
                }

                if (panelRt.rect.width < 50f)
                {
                    panelRt.sizeDelta = new Vector2(240f, 0f);
                }
            }
        }

        private void EnsureHorizontalLayout()
        {
            if (itemCardContainer == null) return;

            // Remove VerticalLayoutGroup if it exists
            UnityEngine.UI.VerticalLayoutGroup vlg = itemCardContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null) Destroy(vlg);

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

