using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tycoon.Data
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Tycoon/Character SO")]
    public class CharacterSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string characterId;
        public string characterName;
        public string jobTitle = "Staff";
        [TextArea(2, 4)]
        public string bio;
        public Sprite avatarIcon;

        [Header("2D Visual Sprites")]
        public Sprite standingSprite;
        public Sprite sittingSprite;
        public List<Sprite> walkingSprites = new List<Sprite>();

        [Header("Movement & Stamina Stats (Non-Income)")]
        [Range(0.5f, 5.0f)]
        public float baseWalkSpeed = 2.0f;
        [Range(20f, 200f)]
        public float maxStamina = 100f;
        [Range(0.5f, 10f)]
        public float staminaDepletionRate = 2f; // loss per sec
        [Range(2f, 30f)]
        public float staminaRecoveryRate = 10f; // recovery per sec

        [Header("AI Behavioral Tendencies")]
        [Range(1f, 10f)]
        public float pacingDurationMin = 2f;
        [Range(5f, 20f)]
        public float pacingDurationMax = 8f;
        [Range(3f, 20f)]
        public float sittingDurationMin = 5f;
        [Range(10f, 40f)]
        public float sittingDurationMax = 15f;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = name.ToLower().Replace(" ", "_");
            }
        }
    }
}
