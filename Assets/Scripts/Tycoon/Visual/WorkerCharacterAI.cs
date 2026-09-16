using System.Collections;
using UnityEngine;
using Tycoon.Data;

namespace Tycoon.Visual
{
    public enum WorkerAIState
    {
        Idle,
        Wandering,
        MovingToSeat,
        SittingAtSeat,
        MovingToCoffee,
        DrinkingCoffee
    }

    /// <summary>
    /// Autonomous 2D movement AI for office workers.
    /// Operates independently with individual randomized speed, decision timing, and route preferences.
    /// Handles pacing around waypoints, sitting at chairs, and taking coffee breaks based on stamina.
    /// NOTE: Stamina and stats are purely behavioral/visual and do NOT alter currency income.
    /// </summary>
    public class WorkerCharacterAI : MonoBehaviour
    {
        [Header("Character Data Reference")]
        [SerializeField] private CharacterSO characterData;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Runtime State Readout (Debug)")]
        [SerializeField] private WorkerAIState currentState = WorkerAIState.Idle;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float currentMoveSpeed = 2f;

        // Individual variation factors
        private float speedMultiplier = 1f;
        private float decisionTimerOffset = 0f;
        private Transform assignedChairTarget;
        private Transform currentTargetTransform;
        private Vector3 currentTargetPosition;

        public CharacterSO CharacterData => characterData;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => characterData != null ? characterData.maxStamina : 100f;
        public WorkerAIState CurrentState => currentState;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // Generate individual variation so characters move distinctly
            speedMultiplier = Random.Range(0.8f, 1.25f);
            decisionTimerOffset = Random.Range(-1.5f, 1.5f);
        }

        private void Start()
        {
            if (characterData != null)
            {
                currentStamina = characterData.maxStamina;
                currentMoveSpeed = characterData.baseWalkSpeed * speedMultiplier;
                if (spriteRenderer != null && characterData.standingSprite != null)
                {
                    spriteRenderer.sprite = characterData.standingSprite;
                }
            }

            StartCoroutine(AIBehaviorRoutine());
        }

        public void SetupCharacter(CharacterSO data)
        {
            characterData = data;
            if (characterData != null)
            {
                currentStamina = characterData.maxStamina;
                currentMoveSpeed = characterData.baseWalkSpeed * speedMultiplier;
                if (spriteRenderer != null && characterData.standingSprite != null)
                {
                    spriteRenderer.sprite = characterData.standingSprite;
                }
            }
        }

        /// <summary>
        /// Main autonomous FSM loop for worker decision making.
        /// </summary>
        private IEnumerator AIBehaviorRoutine()
        {
            // Initial random delay to prevent synchronized starting movement
            yield return new WaitForSeconds(Random.Range(0.2f, 2.0f));

            while (true)
            {
                // 1. Low Stamina Check -> Go get coffee!
                if (currentStamina <= 20f && currentState != WorkerAIState.MovingToCoffee && currentState != WorkerAIState.DrinkingCoffee)
                {
                    yield return StartCoroutine(RoutineGoToCoffee());
                }
                else
                {
                    // 2. Decide between Wandering (pacing) or Sitting at a chair
                    int roll = Random.Range(0, 100);
                    if (roll < 55)
                    {
                        yield return StartCoroutine(RoutineSitAtChair());
                    }
                    else
                    {
                        yield return StartCoroutine(RoutinePacingWander());
                    }
                }

                yield return null;
            }
        }

        private IEnumerator RoutinePacingWander()
        {
            currentState = WorkerAIState.Wandering;
            SetSprite(characterData != null ? characterData.standingSprite : null);

            Transform destination = OfficeWaypointGroup.Instance != null 
                ? OfficeWaypointGroup.Instance.GetRandomPacingWaypoint() 
                : null;

            if (destination != null)
            {
                yield return StartCoroutine(MoveToPosition(destination.position));
            }

            // Idle pacing pause at destination
            currentState = WorkerAIState.Idle;
            float idleTime = Random.Range(1.5f, 4.5f) + decisionTimerOffset;
            float elapsed = 0f;
            while (elapsed < idleTime)
            {
                elapsed += Time.deltaTime;
                DepleteStamina(Time.deltaTime * 0.5f);
                yield return null;
            }
        }

        private IEnumerator RoutineSitAtChair()
        {
            if (OfficeWaypointGroup.Instance != null)
            {
                assignedChairTarget = OfficeWaypointGroup.Instance.ClaimAvailableChair(assignedChairTarget);
            }

            if (assignedChairTarget != null)
            {
                currentState = WorkerAIState.MovingToSeat;
                SetSprite(characterData != null ? characterData.standingSprite : null);
                yield return StartCoroutine(MoveToPosition(assignedChairTarget.position));

                // Sit down and work
                currentState = WorkerAIState.SittingAtSeat;
                SetSprite(characterData != null && characterData.sittingSprite != null ? characterData.sittingSprite : characterData.standingSprite);

                float sitDuration = (characterData != null 
                    ? Random.Range(characterData.sittingDurationMin, characterData.sittingDurationMax) 
                    : Random.Range(5f, 12f)) + decisionTimerOffset;

                float elapsed = 0f;
                while (elapsed < sitDuration && currentStamina > 15f)
                {
                    elapsed += Time.deltaTime;
                    DepleteStamina(Time.deltaTime * (characterData != null ? characterData.staminaDepletionRate : 2f));
                    yield return null;
                }

                if (OfficeWaypointGroup.Instance != null && assignedChairTarget != null)
                {
                    OfficeWaypointGroup.Instance.ReleaseChair(assignedChairTarget);
                    assignedChairTarget = null;
                }
            }
        }

        private IEnumerator RoutineGoToCoffee()
        {
            if (OfficeWaypointGroup.Instance != null && assignedChairTarget != null)
            {
                OfficeWaypointGroup.Instance.ReleaseChair(assignedChairTarget);
                assignedChairTarget = null;
            }

            Transform coffeeSpot = OfficeWaypointGroup.Instance != null 
                ? OfficeWaypointGroup.Instance.GetCoffeeTarget() 
                : null;

            if (coffeeSpot != null)
            {
                currentState = WorkerAIState.MovingToCoffee;
                SetSprite(characterData != null ? characterData.standingSprite : null);
                yield return StartCoroutine(MoveToPosition(coffeeSpot.position));

                // Drinking coffee / break
                currentState = WorkerAIState.DrinkingCoffee;
                float recoveryRate = characterData != null ? characterData.staminaRecoveryRate : 15f;

                while (currentStamina < MaxStamina)
                {
                    currentStamina += Time.deltaTime * recoveryRate;
                    yield return null;
                }
                currentStamina = MaxStamina;

                // Brief satisfaction pause after drinking coffee
                yield return new WaitForSeconds(Random.Range(1.0f, 2.5f));
            }
        }

        private IEnumerator MoveToPosition(Vector3 targetPos)
        {
            targetPos.z = transform.position.z; // Keep 2D depth

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                // Face walking direction
                if (targetPos.x > transform.position.x + 0.02f)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
                else if (targetPos.x < transform.position.x - 0.02f)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, currentMoveSpeed * Time.deltaTime);
                DepleteStamina(Time.deltaTime * 0.3f);
                yield return null;
            }

            transform.position = targetPos;
        }

        private void DepleteStamina(float amount)
        {
            currentStamina = Mathf.Max(0f, currentStamina - amount);
        }

        private void SetSprite(Sprite sprite)
        {
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        private void OnMouseDown()
        {
            // Fun little click feedback (bounce effect) when clicking mascot in scene directly
            StopCoroutine(nameof(ClickBounceRoutine));
            StartCoroutine(nameof(ClickBounceRoutine));
        }

        private IEnumerator ClickBounceRoutine()
        {
            Vector3 originalScale = transform.localScale;
            float targetX = Mathf.Sign(originalScale.x) * Mathf.Abs(originalScale.x) * 1.25f;
            float targetY = originalScale.y * 1.25f;
            transform.localScale = new Vector3(targetX, targetY, originalScale.z);

            yield return new WaitForSeconds(0.15f);

            transform.localScale = originalScale;
        }

        private void OnDestroy()
        {
            if (OfficeWaypointGroup.Instance != null && assignedChairTarget != null)
            {
                OfficeWaypointGroup.Instance.ReleaseChair(assignedChairTarget);
            }
        }
    }
}
