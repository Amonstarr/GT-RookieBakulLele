using System.Collections;
using System.Collections.Generic;
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

    public enum CharacterAnimState
    {
        Idle,
        WalkHorizontal,
        WalkUp,
        WalkDown,
        Sitting
    }

    /// <summary>
    /// Autonomous 2D movement AI for office workers.
    /// Operates independently with individual randomized speed, decision timing, and route preferences.
    /// Supports multi-directional frame-by-frame spritesheet animations (Idle, Walk H-Flip, Walk Up, Walk Down).
    /// </summary>
    public class WorkerCharacterAI : MonoBehaviour
    {
        [Header("Character Data Reference")]
        [SerializeField] private CharacterSO characterData;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Runtime State Readout (Debug)")]
        [SerializeField] private WorkerAIState currentState = WorkerAIState.Idle;
        [SerializeField] private CharacterAnimState currentAnimState = CharacterAnimState.Idle;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float currentMoveSpeed = 2f;

        // Animation internal state
        private float animTimer = 0f;
        private int animFrameIndex = 0;
        private CharacterAnimState lastAnimState = CharacterAnimState.Idle;

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
        public CharacterAnimState CurrentAnimState => currentAnimState;

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

        private void Update()
        {
            UpdateSpriteAnimation();
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
        /// Frame-by-frame animation tick according to current anim direction state.
        /// </summary>
        private void UpdateSpriteAnimation()
        {
            if (characterData == null || spriteRenderer == null) return;

            // Reset frame index on state switch
            if (currentAnimState != lastAnimState)
            {
                lastAnimState = currentAnimState;
                animFrameIndex = 0;
                animTimer = 0f;
            }

            // 1. Sitting State Check
            if (currentState == WorkerAIState.SittingAtSeat)
            {
                if (characterData.sittingSprite != null)
                {
                    spriteRenderer.sprite = characterData.sittingSprite;
                }
                return;
            }

            // 2. Select target frame list by direction
            List<Sprite> targetFrameList = null;

            switch (currentAnimState)
            {
                case CharacterAnimState.WalkUp:
                    targetFrameList = (characterData.walkUpSprites != null && characterData.walkUpSprites.Count > 0)
                        ? characterData.walkUpSprites
                        : (characterData.walkHorizontalSprites != null && characterData.walkHorizontalSprites.Count > 0
                            ? characterData.walkHorizontalSprites
                            : characterData.walkingSprites);
                    break;

                case CharacterAnimState.WalkDown:
                    targetFrameList = (characterData.walkDownSprites != null && characterData.walkDownSprites.Count > 0)
                        ? characterData.walkDownSprites
                        : (characterData.walkHorizontalSprites != null && characterData.walkHorizontalSprites.Count > 0
                            ? characterData.walkHorizontalSprites
                            : characterData.walkingSprites);
                    break;

                case CharacterAnimState.WalkHorizontal:
                    targetFrameList = (characterData.walkHorizontalSprites != null && characterData.walkHorizontalSprites.Count > 0)
                        ? characterData.walkHorizontalSprites
                        : characterData.walkingSprites;
                    break;

                case CharacterAnimState.Idle:
                default:
                    targetFrameList = (characterData.idleSprites != null && characterData.idleSprites.Count > 0)
                        ? characterData.idleSprites
                        : null;
                    break;
            }

            // Fallback to static standingSprite if list is empty
            if (targetFrameList == null || targetFrameList.Count == 0)
            {
                if (characterData.standingSprite != null)
                {
                    spriteRenderer.sprite = characterData.standingSprite;
                }
                return;
            }

            // Frame animation ticker
            float fps = characterData.animationFrameRate > 0 ? characterData.animationFrameRate : 10f;
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / fps)
            {
                animTimer -= 1f / fps;
                animFrameIndex = (animFrameIndex + 1) % targetFrameList.Count;
            }

            if (animFrameIndex < targetFrameList.Count && targetFrameList[animFrameIndex] != null)
            {
                spriteRenderer.sprite = targetFrameList[animFrameIndex];
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

            Transform destination = OfficeWaypointGroup.Instance != null 
                ? OfficeWaypointGroup.Instance.GetRandomPacingWaypoint() 
                : null;

            if (destination != null)
            {
                yield return StartCoroutine(MoveToPosition(destination.position));
            }

            // Idle pacing pause at destination
            currentState = WorkerAIState.Idle;
            currentAnimState = CharacterAnimState.Idle;

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

                // Bergerak ke waypoint lorong terdekat dahulu agar tidak berjalan menyamping menabrak meja/objek
                if (OfficeWaypointGroup.Instance != null)
                {
                    Transform nearestWp = OfficeWaypointGroup.Instance.GetNearestPacingWaypoint(transform.position);
                    if (nearestWp != null && Vector3.Distance(transform.position, nearestWp.position) > 0.5f)
                    {
                        yield return StartCoroutine(MoveToPosition(nearestWp.position));
                    }
                }

                yield return StartCoroutine(MoveToPosition(assignedChairTarget.position));

                // Sit down and work
                currentState = WorkerAIState.SittingAtSeat;
                currentAnimState = CharacterAnimState.Sitting;

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
            else
            {
                // Fallback ke jalan-jalan biasa jika tidak ada kursi yang terbuka/dibeli
                yield return StartCoroutine(RoutinePacingWander());
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

                // Bergerak ke waypoint lorong terdekat dahulu sebelum menuju mesin kopi
                if (OfficeWaypointGroup.Instance != null)
                {
                    Transform nearestWp = OfficeWaypointGroup.Instance.GetNearestPacingWaypoint(transform.position);
                    if (nearestWp != null && Vector3.Distance(transform.position, nearestWp.position) > 0.5f)
                    {
                        yield return StartCoroutine(MoveToPosition(nearestWp.position));
                    }
                }

                yield return StartCoroutine(MoveToPosition(coffeeSpot.position));

                // Drinking coffee / break
                currentState = WorkerAIState.DrinkingCoffee;
                currentAnimState = CharacterAnimState.Idle;

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
            else
            {
                // Fallback ke jalan-jalan biasa jika mesin kopi belum dibeli
                yield return StartCoroutine(RoutinePacingWander());
            }
        }

        private IEnumerator MoveToPosition(Vector3 targetPos)
        {
            targetPos.z = transform.position.z; // Keep 2D depth

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                Vector3 moveDelta = targetPos - transform.position;
                float absX = Mathf.Abs(moveDelta.x);
                float absY = Mathf.Abs(moveDelta.y);

                // Determine directional animation state
                if (absY > absX && absY > 0.05f)
                {
                    currentAnimState = moveDelta.y > 0 ? CharacterAnimState.WalkUp : CharacterAnimState.WalkDown;
                }
                else if (absX >= absY && absX > 0.05f)
                {
                    currentAnimState = CharacterAnimState.WalkHorizontal;
                    // Face horizontal walking direction (flip scale X)
                    if (moveDelta.x > 0.02f)
                    {
                        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    }
                    else if (moveDelta.x < -0.02f)
                    {
                        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    }
                }

                transform.position = Vector3.MoveTowards(transform.position, targetPos, currentMoveSpeed * Time.deltaTime);
                DepleteStamina(Time.deltaTime * 0.3f);
                yield return null;
            }

            transform.position = targetPos;
            currentAnimState = CharacterAnimState.Idle;
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
