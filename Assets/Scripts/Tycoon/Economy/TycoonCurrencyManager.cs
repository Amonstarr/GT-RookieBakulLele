using System;
using UnityEngine;

namespace Tycoon.Economy
{
    /// <summary>
    /// Manages the currency (coins) for the Tycoon minigame.
    /// Supports Inspector debugging and triggers events when coin balances change.
    /// </summary>
    public class TycoonCurrencyManager : MonoBehaviour
    {
        public static TycoonCurrencyManager Instance { get; private set; }

        [Header("Starting Balance / Debug Settings")]
        [SerializeField] private int currentCoins = 1000;
        
        [Header("Simulator HR Multiplier")]
        [SerializeField] private float hrScoreMultiplier = 1.0f;

        /// <summary>
        /// Action invoked whenever the coin amount changes. Passes current total coins.
        /// </summary>
        public event Action<int> OnCoinsChanged;

        public int CurrentCoins => currentCoins;
        public float HRScoreMultiplier => hrScoreMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Trigger initial UI update
            OnCoinsChanged?.Invoke(currentCoins);
        }

        /// <summary>
        /// Checks if player has at least the specified amount of coins.
        /// </summary>
        public bool HasEnoughCoins(int amount)
        {
            return currentCoins >= amount;
        }

        /// <summary>
        /// Add coins directly (e.g. from debug or reward).
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            currentCoins += amount;
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"[TycoonCurrencyManager] Added {amount} coins. Total: {currentCoins}");
        }

        /// <summary>
        /// Add coins calculated from HR Simulator score.
        /// </summary>
        public void AddCoinsFromHRScore(int rawScore)
        {
            int calculatedCoins = Mathf.RoundToInt(rawScore * hrScoreMultiplier);
            AddCoins(calculatedCoins);
        }

        /// <summary>
        /// Deducts coins if affordable. Returns true if successful.
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (!HasEnoughCoins(amount)) return false;

            currentCoins -= amount;
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"[TycoonCurrencyManager] Spent {amount} coins. Remaining: {currentCoins}");
            return true;
        }

        /// <summary>
        /// Force set coin amount (useful for Inspector debug or save loading).
        /// </summary>
        public void SetCoins(int amount)
        {
            currentCoins = Mathf.Max(0, amount);
            OnCoinsChanged?.Invoke(currentCoins);
        }

        #region Inspector Debug Context Buttons

        [ContextMenu("Debug: Add +1,000 Coins")]
        public void DebugAdd1000Coins()
        {
            AddCoins(1000);
        }

        [ContextMenu("Debug: Add +10,000 Coins")]
        public void DebugAdd10000Coins()
        {
            AddCoins(10000);
        }

        [ContextMenu("Debug: Reset Coins to 0")]
        public void DebugResetCoins()
        {
            SetCoins(0);
        }

        #endregion
    }
}
