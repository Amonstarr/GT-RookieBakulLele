using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tycoon.UI
{
    /// <summary>
    /// Smooth hover scale & animation effect for character cards/buttons on UI.
    /// Add this component to any Character Button/Card in Unity.
    /// </summary>
    public class CharacterCardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Hover Settings")]
        [Tooltip("Skala pembesaran saat kursor berada di atas card/button (misal: 1.08 = 8% lebih besar)")]
        [SerializeField] private float hoverScale = 1.08f;

        [Tooltip("Skala penekanan saat card/button diklik (misal: 0.95 = 5% lebih kecil)")]
        [SerializeField] private float pressScale = 0.95f;

        [Tooltip("Kecepatan transisi animasi pembesaran/pengecilan")]
        [SerializeField] private float transitionSpeed = 12f;

        private Vector3 defaultScale;
        private Vector3 targetScale;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            defaultScale = transform.localScale;
            targetScale = defaultScale;
        }

        private void OnEnable()
        {
            transform.localScale = defaultScale;
            targetScale = defaultScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartScaleAnimation(defaultScale * hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartScaleAnimation(defaultScale);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            StartScaleAnimation(defaultScale * pressScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StartScaleAnimation(defaultScale * hoverScale);
        }

        private void StartScaleAnimation(Vector3 newTargetScale)
        {
            targetScale = newTargetScale;
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateScaleRoutine());
        }

        private IEnumerator AnimateScaleRoutine()
        {
            while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
                yield return null;
            }
            transform.localScale = targetScale;
        }
    }
}
