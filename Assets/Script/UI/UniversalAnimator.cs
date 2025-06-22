using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

namespace UI.Effects
{
    /// <summary>
    /// Universal animation controller for UI and world space objects with support for various entrance effects.
    /// Supports SpriteRenderer, UI Graphics, and CanvasGroup components.
    /// </summary>
    public class UniversalEffectAnimator : MonoBehaviour
    {
        [System.Serializable]
        public enum EffectType
        {
            Random,
            FadeInFromTop,
            FadeInFromBottom,
            FadeInFromLeft,
            FadeInFromRight,
            PopUp,
            MergeFromRandom,
            FadeInPlace,
            SlideInFromTop,
            SlideInFromBottom,
            Bounce,
            Elastic
        }

        [Header("Animation Settings")]
        [SerializeField] private EffectType selectedEffect = EffectType.PopUp;

        [SerializeField, Range(0.1f, 3f)] private float duration = 0.5f;
        [SerializeField] private float offsetDistance = 100f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool resetOnDisable = true;

        [Header("Advanced Settings")]
        [SerializeField] private Ease easeType = Ease.OutCubic;

        [SerializeField] private float delay = 0f;
        [SerializeField] private bool useUnscaledTime = false;

        // Events
        public event Action OnAnimationStart;

        public event Action OnAnimationComplete;

        // Cached components
        private SpriteRenderer spriteRenderer;

        private Graphic uiGraphic;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        // Original states
        private Vector3 originalPosition;

        private Vector3 originalScale;
        private float originalAlpha;

        // Animation management
        private Sequence currentSequence;

        private bool isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            CacheOriginalStates();
        }

        private void Start()
        {
            if (playOnStart)
                PlayEffect();
        }

        private void OnDisable()
        {
            if (resetOnDisable)
                ResetToOriginalState();

            KillCurrentAnimation();
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }

        #endregion Unity Lifecycle

        #region Initialization

        private void InitializeComponents()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            uiGraphic = GetComponent<Graphic>();
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            // Auto-add CanvasGroup if we have UI components but no CanvasGroup
            if (canvasGroup == null && (uiGraphic != null || rectTransform != null))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            isInitialized = true;
        }

        private void CacheOriginalStates()
        {
            originalPosition = transform.localPosition;
            originalScale = transform.localScale;
            originalAlpha = GetCurrentAlpha();
        }

        #endregion Initialization

        #region Public Methods

        /// <summary>
        /// Play the selected effect animation
        /// </summary>
        public void PlayEffect()
        {
            if (!isInitialized) return;

            PlayEffect(selectedEffect);
        }

        /// <summary>
        /// Play a specific effect animation
        /// </summary>
        /// <param name="effectType">The effect type to play</param>
        public void PlayEffect(EffectType effectType)
        {
            if (!isInitialized || !gameObject.activeInHierarchy) return;

            KillCurrentAnimation();

            EffectType effectToPlay = effectType == EffectType.Random ? GetRandomEffect() : effectType;

            OnAnimationStart?.Invoke();

            currentSequence = CreateEffectSequence(effectToPlay);

            if (currentSequence != null)
            {
                currentSequence.SetUpdate(useUnscaledTime)
                              .SetDelay(delay)
                              .OnComplete(() => OnAnimationComplete?.Invoke());
            }
        }

        /// <summary>
        /// Stop current animation and reset to original state
        /// </summary>
        public void StopAnimation()
        {
            KillCurrentAnimation();
            ResetToOriginalState();
        }

        /// <summary>
        /// Reset object to its original state
        /// </summary>
        public void ResetToOriginalState()
        {
            if (!isInitialized) return;

            transform.localPosition = originalPosition;
            transform.localScale = originalScale;
            SetAlpha(originalAlpha);
        }

        #endregion Public Methods

        #region Animation Creation

        private Sequence CreateEffectSequence(EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.FadeInFromTop:
                    return CreateDirectionalFade(Vector2.up);

                case EffectType.FadeInFromBottom:
                    return CreateDirectionalFade(Vector2.down);

                case EffectType.FadeInFromLeft:
                    return CreateDirectionalFade(Vector2.left);

                case EffectType.FadeInFromRight:
                    return CreateDirectionalFade(Vector2.right);

                case EffectType.PopUp:
                    return CreatePopUpAnimation();

                case EffectType.MergeFromRandom:
                    return CreateDirectionalFade(UnityEngine.Random.insideUnitCircle.normalized);

                case EffectType.FadeInPlace:
                    return CreateFadeAnimation();

                case EffectType.SlideInFromTop:
                    return CreateSlideAnimation(Vector2.up);

                case EffectType.SlideInFromBottom:
                    return CreateSlideAnimation(Vector2.down);

                case EffectType.Bounce:
                    return CreateBounceAnimation();

                case EffectType.Elastic:
                    return CreateElasticAnimation();

                default:
                    return CreateFadeAnimation();
            }
        }

        private Sequence CreateDirectionalFade(Vector2 direction)
        {
            SetAlpha(0f);
            Vector3 startPos = originalPosition + (Vector3)(direction * offsetDistance);
            transform.localPosition = startPos;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOLocalMove(originalPosition, duration).SetEase(easeType));
            sequence.Join(CreateFadeTween(1f, duration));

            return sequence;
        }

        private Sequence CreateSlideAnimation(Vector2 direction)
        {
            Vector3 startPos = originalPosition + (Vector3)(direction * offsetDistance);
            transform.localPosition = startPos;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOLocalMove(originalPosition, duration).SetEase(easeType));

            return sequence;
        }

        private Sequence CreatePopUpAnimation()
        {
            SetAlpha(0f);
            transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBack));
            sequence.Join(CreateFadeTween(1f, duration));

            return sequence;
        }

        private Sequence CreateBounceAnimation()
        {
            SetAlpha(0f);
            transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBounce));
            sequence.Join(CreateFadeTween(1f, duration));

            return sequence;
        }

        private Sequence CreateElasticAnimation()
        {
            SetAlpha(0f);
            transform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutElastic));
            sequence.Join(CreateFadeTween(1f, duration));

            return sequence;
        }

        private Sequence CreateFadeAnimation()
        {
            SetAlpha(0f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(CreateFadeTween(1f, duration).SetEase(Ease.Linear));

            return sequence;
        }

        #endregion Animation Creation

        #region Helper Methods

        private EffectType GetRandomEffect()
        {
            Array values = Enum.GetValues(typeof(EffectType));
            EffectType randomEffect;

            do
            {
                randomEffect = (EffectType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            }
            while (randomEffect == EffectType.Random);

            return randomEffect;
        }

        private void KillCurrentAnimation()
        {
            currentSequence?.Kill();
            currentSequence = null;
        }

        private float GetCurrentAlpha()
        {
            if (spriteRenderer != null)
                return spriteRenderer.color.a;
            else if (uiGraphic != null)
                return uiGraphic.color.a;
            else if (canvasGroup != null)
                return canvasGroup.alpha;

            return 1f;
        }

        private void SetAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            if (uiGraphic != null)
            {
                Color color = uiGraphic.color;
                color.a = alpha;
                uiGraphic.color = color;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        private Tween CreateFadeTween(float targetAlpha, float animationDuration)
        {
            if (spriteRenderer != null)
                return spriteRenderer.DOFade(targetAlpha, animationDuration);
            else if (uiGraphic != null)
                return uiGraphic.DOFade(targetAlpha, animationDuration);
            else if (canvasGroup != null)
                return canvasGroup.DOFade(targetAlpha, animationDuration);

            return null;
        }

        #endregion Helper Methods

        #region Editor Helpers

#if UNITY_EDITOR

        [ContextMenu("Preview Effect")]
        private void PreviewEffect()
        {
            if (Application.isPlaying)
            {
                PlayEffect();
            }
            else
            {
                Debug.Log($"Preview: {selectedEffect} effect would play with duration {duration}s");
            }
        }

        private void OnValidate()
        {
            if (duration <= 0f)
                duration = 0.1f;

            if (offsetDistance < 0f)
                offsetDistance = 0f;
        }

#endif

        #endregion Editor Helpers
    }
}