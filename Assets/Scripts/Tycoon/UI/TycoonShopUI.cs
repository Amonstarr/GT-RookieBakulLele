using System.Collections.Generic;
using UnityEngine;
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

        [Header("Options")]
        [SerializeField] private bool openOnStart = false;

        private List<TycoonItemCardUI> spawnedCards = new List<TycoonItemCardUI>();

        private void Start()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(openOnStart);
            }

            PopulateShop();
        }

        /// <summary>
        /// Instantiates cards for all available items registered in TycoonManager.
        /// </summary>
        public void PopulateShop()
        {
            // Clear existing cards
            foreach (Transform child in itemCardContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedCards.Clear();

            if (TycoonManager.Instance == null || itemCardPrefab == null || itemCardContainer == null)
            {
                return;
            }

            foreach (var item in TycoonManager.Instance.AvailableItems)
            {
                if (item == null) continue;

                GameObject cardObj = Instantiate(itemCardPrefab, itemCardContainer);
                TycoonItemCardUI cardUI = cardObj.GetComponent<TycoonItemCardUI>();
                if (cardUI != null)
                {
                    cardUI.Setup(item);
                    spawnedCards.Add(cardUI);
                }
            }
        }

        public void OpenShop()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            RefreshAllCards();
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
                if (nextState) RefreshAllCards();
            }
        }

        public void RefreshAllCards()
        {
            foreach (var card in spawnedCards)
            {
                if (card != null) card.RefreshCard();
            }
        }
    }
}
